using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Features.Clusters.Services;
using MultiClusterMgmtSys.Features.Clusters.ViewModels;
using MultiClusterMgmtSys.Features.Nodes.ViewModels;
using MultiClusterMgmtSys.ViewModels;
using System.Text;

namespace MultiClusterMgmtSys.Features.Nodes.Services;

/// <summary>
/// 节点维度的查询服务：从 k8s 实时拉取节点列表与节点详情。
/// 与 <see cref="ClusterService"/> 解耦，后者负责集群 CRUD 与连通性探测。
/// </summary>
public class ClusterNodeService(ClusterRepository repo)
{
    private readonly ClusterRepository repo = repo;

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