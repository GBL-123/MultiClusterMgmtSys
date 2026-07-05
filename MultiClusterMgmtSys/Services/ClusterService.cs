using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;

namespace MultiClusterMgmtSys.Services;

public class ClusterService(ClusterRepository repo, ILogger<ClusterService> logger)
{
    private readonly ClusterRepository repo = repo;
    private readonly ILogger<ClusterService> logger = logger;

    public async Task<List<ClusterViewModel>> GetClustersAsync()
    {
        var clusters = await repo.GetAllAsync();
        return [.. clusters.Select(c => c.ToViewModel())];
    }

    public async Task<ClusterDetailViewModel?> GetClusterDetailAsync(int id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null) return null;

        var vm = entity.ToDetailViewModel();
        if (entity.Status != ClusterStatus.Offline)
        {
            try
            {
                vm.Nodes = await GetClusterNodesAsync(id);
                vm.IsReachable = true;
            }
            catch
            {
                vm.IsReachable = false;
            }
        }
        return vm;
    }

    public async Task<ClusterEditViewModel?> GetClusterForEditAsync(int id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null) return null;
        return entity.ToEditViewModel();
    }

    public async Task<ClusterViewModel> AddClusterAsync(ClusterCreateViewModel vm)
    {
        var entity = new ClusterInfo
        {
            Name = vm.Name,
            GroupId = vm.GroupId,
            ApiServer = vm.ApiServer,
            ConnectionType = vm.ConnectionType,
            KubeConfig = vm.ConnectionType == ConnectionType.KubeConfig ? vm.KubeConfig : null,
            Token = vm.ConnectionType == ConnectionType.Token ? vm.Token : null,
            SkipTlsVerify = vm.SkipTlsVerify,
            Status = ClusterStatus.Unknown,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        await ProbeAsync(entity);
        await repo.UpdateAsync(entity);
        return entity.ToViewModel();
    }

    public async Task<ClusterViewModel> UpdateClusterAsync(ClusterUpdateViewModel vm)
    {
        var entity = await repo.GetByIdAsync(vm.Id);
        if (entity is null)
            throw new InvalidOperationException($"Cluster {vm.Id} not found");

        var configChanged = entity.ConnectionType != vm.ConnectionType
            || entity.ApiServer != vm.ApiServer
            || entity.KubeConfig != vm.KubeConfig
            || entity.Token != vm.Token
            || entity.SkipTlsVerify != vm.SkipTlsVerify;

        entity.Name = vm.Name;
        entity.GroupId = vm.GroupId;
        entity.ApiServer = vm.ApiServer;
        entity.ConnectionType = vm.ConnectionType;
        entity.SkipTlsVerify = vm.SkipTlsVerify;
        entity.KubeConfig = vm.ConnectionType == ConnectionType.KubeConfig ? vm.KubeConfig : null;
        entity.Token = vm.ConnectionType == ConnectionType.Token ? vm.Token : null;

        if (configChanged)
        {
            await ProbeAsync(entity);
        }

        await repo.UpdateAsync(entity);
        return entity.ToViewModel();
    }

    public async Task DeleteClusterAsync(int id)
    {
        await repo.DeleteAsync(id);
    }

    public async Task<ClusterViewModel> RefreshClusterStatusAsync(int id)
    {
        var entity = await repo.GetByIdAsync(id) 
            ?? throw new InvalidOperationException($"Cluster {id} not found");
        await ProbeAsync(entity);
        await repo.UpdateAsync(entity);
        return entity.ToViewModel();
    }

    public async Task<List<ClusterNodeViewModel>> GetClusterNodesAsync(int id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
            throw new InvalidOperationException($"Cluster {id} not found");

        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var nodeList = await client.CoreV1.ListNodeAsync();
        return nodeList.Items.Select(MapNode).ToList();
    }

    public async Task<ClusterNodeDetailViewModel?> GetNodeDetailAsync(int clusterId, string nodeName)
    {
        var entity = await repo.GetByIdAsync(clusterId);
        if (entity is null) return null;

        if (entity.Status == ClusterStatus.Offline)
        {
            return new ClusterNodeDetailViewModel
            {
                ClusterId = clusterId,
                ClusterName = entity.Name,
                IsReachable = false
            };
        }

        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var node = await client.CoreV1.ReadNodeAsync(nodeName);
        var vm = MapNodeDetail(node, entity);
        vm.ClusterId = clusterId;
        vm.ClusterName = entity.Name;
        vm.IsReachable = true;
        return vm;
    }

    // ---- Private k8s helpers ----

    private KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)
    {
        if (cluster.ConnectionType == ConnectionType.KubeConfig)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cluster.KubeConfig ?? ""));
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
        }

        return new KubernetesClientConfiguration
        {
            Host = cluster.ApiServer ?? "",
            AccessToken = cluster.Token ?? "",
            SkipTlsVerify = cluster.SkipTlsVerify
        };
    }

    private async Task ProbeAsync(ClusterInfo cluster)
    {
        try
        {
            var config = BuildConfig(cluster);
            using var client = new Kubernetes(config);
            var versionInfo = await client.Version.GetCodeAsync();
            var nodeList = await client.CoreV1.ListNodeAsync();

            cluster.Status = ClusterStatus.Online;
            cluster.Version = versionInfo.GitVersion;
            cluster.NodeCount = nodeList.Items.Count;

            if (string.IsNullOrEmpty(cluster.ApiServer))
                cluster.ApiServer = config.Host;

            cluster.LastCheckedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to cluster {ClusterName} (Id={ClusterId})", cluster.Name, cluster.Id);
            cluster.Status = ClusterStatus.Offline;
            cluster.Version = null;
            cluster.NodeCount = 0;
            cluster.LastCheckedAt = DateTime.UtcNow;
        }
    }

    private static ClusterNodeViewModel MapNode(V1Node node)
    {
        return new ClusterNodeViewModel
        {
            Name = node.Metadata?.Name ?? "",
            Status = ComputeNodeStatus(node),
            Roles = ComputeRoles(node),
            KubeletVersion = node.Status?.NodeInfo?.KubeletVersion ?? "",
            OsImage = node.Status?.NodeInfo?.OsImage ?? "",
            InternalIP = ComputeInternalIP(node)
        };
    }

    private static string ComputeNodeStatus(V1Node node)
    {
        var readyCondition = node.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready");
        return readyCondition is not null
            ? (readyCondition.Status == "True" ? "Ready" : "NotReady")
            : "Unknown";
    }

    private static string ComputeRoles(V1Node node)
    {
        if (node.Metadata?.Labels is null) return "";
        const string rolePrefix = "node-role.kubernetes.io/";
        var roleLabels = node.Metadata.Labels
            .Where(kvp => kvp.Key.StartsWith(rolePrefix))
            .Select(kvp => kvp.Key[rolePrefix.Length..]);
        return string.Join(",", roleLabels);
    }

    private static string ComputeInternalIP(V1Node node)
    {
        return node.Status?.Addresses?.FirstOrDefault(a => a.Type == "InternalIP")?.Address ?? "";
    }

    private static ClusterNodeDetailViewModel MapNodeDetail(V1Node node, ClusterInfo cluster)
    {
        var vm = new ClusterNodeDetailViewModel
        {
            // 概要
            Name = node.Metadata?.Name ?? "",
            Status = ComputeNodeStatus(node),
            Roles = ComputeRoles(node),
            KubeletVersion = node.Status?.NodeInfo?.KubeletVersion ?? "",
            OsImage = node.Status?.NodeInfo?.OsImage ?? "",
            InternalIP = ComputeInternalIP(node),

            // 元数据
            Uid = node.Metadata?.Uid ?? "",
            CreatedAt = node.Metadata?.CreationTimestamp,
            Unschedulable = node.Spec?.Unschedulable ?? false,
            PodCIDR = node.Spec?.PodCIDR ?? "",
            Phase = node.Status?.Phase ?? "",

            // 地址
            Addresses = node.Status?.Addresses?.Select(a => new NodeAddressViewModel
            {
                Type = a.Type ?? "",
                Address = a.Address ?? ""
            }).ToList() ?? new(),

            // 条件
            Conditions = node.Status?.Conditions?.Select(c => new NodeConditionViewModel
            {
                Type = c.Type ?? "",
                Status = c.Status ?? "",
                Reason = c.Reason,
                Message = c.Message,
                LastHeartbeatTime = c.LastHeartbeatTime,
                LastTransitionTime = c.LastTransitionTime
            }).ToList() ?? new(),

            // 污点
            Taints = node.Spec?.Taints?.Select(t => new NodeTaintViewModel
            {
                Key = t.Key ?? "",
                Value = t.Value,
                Effect = t.Effect ?? ""
            }).ToList() ?? new(),

            // 容量 & 可分配
            Capacity = node.Status?.Capacity?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "") ?? new(),
            Allocatable = node.Status?.Allocatable?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "") ?? new(),

            // 标签 & 注解
            Labels = node.Metadata?.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
            Annotations = node.Metadata?.Annotations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),

            // 系统信息
            SystemInfo = MapSystemInfo(node.Status?.NodeInfo)
        };

        // 上下文
        vm.ClusterId = cluster.Id;
        vm.ClusterName = cluster.Name;
        vm.IsReachable = true;

        return vm;
    }

    private static NodeSystemInfoViewModel MapSystemInfo(V1NodeSystemInfo? systemInfo)
    {
        if (systemInfo is null) return new();
        return new NodeSystemInfoViewModel
        {
            Architecture = systemInfo.Architecture ?? "",
            BootID = systemInfo.BootID ?? "",
            ContainerRuntimeVersion = systemInfo.ContainerRuntimeVersion ?? "",
            KernelVersion = systemInfo.KernelVersion ?? "",
            KubeProxyVersion = systemInfo.KubeProxyVersion ?? "",
            KubeletVersion = systemInfo.KubeletVersion ?? "",
            MachineID = systemInfo.MachineID ?? "",
            OperatingSystem = systemInfo.OperatingSystem ?? "",
            OsImage = systemInfo.OsImage ?? "",
            SystemUUID = systemInfo.SystemUUID ?? ""
        };
    }
}
