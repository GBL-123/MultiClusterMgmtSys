using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Components.Clusters.ViewModels;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using System.Text;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 节点维度的查询服务：从 k8s 实时拉取节点列表与节点详情。
/// 与 <see cref="ClusterService"/> 解耦，后者负责集群 CRUD 与连通性探测。
/// 节点 IP 备注（<see cref="NodeIpRemark"/>）在读取时合并进地址数据。
/// </summary>
public class ClusterNodeService(ClusterRepository repo, AuditService auditService, ILogger<ClusterNodeService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private readonly ClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<ClusterNodeService> logger = logger;

    private static readonly string[] IpAddressTypes = ["InternalIP", "ExternalIP"];

    public async Task<List<ClusterNodeViewModel>> GetClusterNodesAsync(int id)
    {
        logger.LogInformation("GetClusterNodes clusterId={ClusterId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", id);
            throw new NotFoundException($"集群 {id} 不存在");
        }

        var remarks = BuildRemarkLookup(entity);

        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        IList<V1Node> nodeItems;
        try
        {
            var nodeList = await client.CoreV1.ListNodeAsync();
            nodeItems = nodeList.Items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListNodes failed clusterId={ClusterId}", id);
            throw K8sExceptionMapper.Translate(ex, "加载节点列表");
        }
        var result = nodeItems.Select(n => MapNode(n, remarks)).ToList();
        logger.LogInformation("GetClusterNodes done clusterId={ClusterId} count={Count}", id, result.Count);
        return result;
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

        var remarks = BuildRemarkLookup(entity);

        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        V1Node node;
        try
        {
            node = await client.CoreV1.ReadNodeAsync(nodeName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadNode failed clusterId={ClusterId} node={NodeName}", clusterId, nodeName);
            throw K8sExceptionMapper.Translate(ex, "加载节点详情");
        }
        var vm = MapNodeDetail(node, entity, remarks);
        vm.ClusterId = clusterId;
        vm.ClusterName = entity.Name;
        vm.IsReachable = true;
        return vm;
    }

    public async Task UpdateNodeIpNotesAsync(int clusterId, string nodeName, List<NodeIpNoteEditItem> items)
    {
        logger.LogInformation("UpdateNodeIpNotes clusterId={ClusterId} node={NodeName} count={Count}", clusterId, nodeName, items.Count);
        var entity = await repo.GetByIdAsync(clusterId);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", clusterId);
            throw new NotFoundException($"集群 {clusterId} 不存在");
        }

        var incoming = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Address))
            .ToDictionary(i => i.Address, i => i.Note);

        var existing = entity.NodeIpRemarks
            .Where(r => r.NodeName == nodeName)
            .ToDictionary(r => r.Address);

        foreach (var (address, note) in incoming)
        {
            if (note is not null && note.Length > 64)
            {
                logger.LogWarning("NodeIpRemark note too long node={NodeName} address={Address}", nodeName, address);
                throw new ValidationException("备注长度不能超过 64 个字符");
            }

            if (existing.TryGetValue(address, out var remark))
            {
                remark.Note = note;
            }
            else if (note is not null)
            {
                entity.NodeIpRemarks.Add(new NodeIpRemark
                {
                    ClusterId = clusterId,
                    NodeName = nodeName,
                    Address = address,
                    Note = note
                });
            }
        }

        foreach (var (address, remark) in existing)
        {
            if (!incoming.ContainsKey(address))
            {
                entity.NodeIpRemarks.Remove(remark);
            }
        }

        await repo.UpdateAsync(entity);
        logger.LogInformation("UpdateNodeIpNotes persisted clusterId={ClusterId} node={NodeName}", clusterId, nodeName);
        await auditService.LogAsync(AuditCategory.Node, AuditAction.Update, $"节点: {nodeName} @ 集群 {entity.Name}");
    }

    // ---- Private k8s helpers ----

    private static KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)
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

    private static Dictionary<(string NodeName, string Address), string?> BuildRemarkLookup(ClusterInfo cluster)
    {
        return cluster.NodeIpRemarks.ToDictionary(
            r => (r.NodeName, r.Address),
            r => (string?)r.Note);
    }

    private static bool IsIpClassAddress(string type)
        => IpAddressTypes.Contains(type);

    private static ClusterNodeViewModel MapNode(V1Node node, Dictionary<(string, string), string?> remarks)
    {
        var nodeName = node.Metadata?.Name ?? "";
        return new ClusterNodeViewModel
        {
            Name = nodeName,
            Status = ComputeNodeStatus(node),
            Roles = ComputeRoles(node),
            KubeletVersion = node.Status?.NodeInfo?.KubeletVersion ?? "",
            OsImage = node.Status?.NodeInfo?.OsImage ?? "",
            Unschedulable = node.Spec?.Unschedulable ?? false,
            IpAddresses = node.Status?.Addresses
                ?.Where(a => IsIpClassAddress(a.Type ?? ""))
                .Select(a => new NodeIpViewModel
                {
                    Address = a.Address ?? "",
                    Note = remarks.GetValueOrDefault((nodeName, a.Address ?? ""))
                })
                .ToList() ?? new()
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

    private static ClusterNodeDetailViewModel MapNodeDetail(V1Node node, ClusterInfo cluster, Dictionary<(string, string), string?> remarks)
    {
        var nodeName = node.Metadata?.Name ?? "";
        var vm = new ClusterNodeDetailViewModel
        {
            // 概要
            Name = nodeName,
            Status = ComputeNodeStatus(node),
            Roles = ComputeRoles(node),
            KubeletVersion = node.Status?.NodeInfo?.KubeletVersion ?? "",
            OsImage = node.Status?.NodeInfo?.OsImage ?? "",

            // 元数据
            CreatedAt = node.Metadata?.CreationTimestamp,
            Unschedulable = node.Spec?.Unschedulable ?? false,
            PodCIDR = node.Spec?.PodCIDR ?? "",
            Phase = node.Status?.Phase ?? "",

            // 地址
            Addresses = node.Status?.Addresses?.Select(a => new NodeAddressViewModel
            {
                Type = a.Type ?? "",
                Address = a.Address ?? "",
                Note = IsIpClassAddress(a.Type ?? "")
                    ? remarks.GetValueOrDefault((nodeName, a.Address ?? ""))
                    : null
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
