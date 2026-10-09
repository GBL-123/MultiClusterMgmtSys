using k8s;
using k8s.Models;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Application.Requests;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 节点维度的查询服务：从 k8s 实时拉取节点列表与节点详情。
/// 与 <see cref="ClusterService"/> 解耦，后者负责集群 CRUD 与连通性探测。
/// 节点 IP 备注（<see cref="NodeIpRemark"/>）在读取时合并进地址数据。
/// </summary>
public class ClusterNodeService(IClusterRepository repo, AuditService auditService, ILogger<ClusterNodeService> logger, IClusterClientCache clientCache, IHttpContextAccessor httpContextAccessor)
{
    private readonly IClusterRepository _repo = repo;

    private readonly AuditService _auditService = auditService;

    private readonly ILogger<ClusterNodeService> _logger = logger;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private static readonly string[] _IpAddressTypes = ["InternalIP", "ExternalIP"];

    /// <summary>实时拉取指定集群的节点列表(状态/角色/Kubelet 版本/IP 地址含管理员备注)。集群不存在抛 <see cref="NotFoundException"/>,K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task<List<ClusterNodeViewModel>> GetClusterNodesAsync(int id)
    {
        _logger.LogInformation("GetClusterNodes clusterId={ClusterId}", id);
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", id);
            throw new NotFoundException($"集群 {id} 不存在");
        }

        var remarks = BuildRemarkLookup(entity);

        var client = clientCache.GetOrCreate(entity);
        IList<V1Node> nodeItems;
        try
        {
            var nodeList = await client.CoreV1.ListNodeAsync();
            nodeItems = nodeList.Items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListNodes failed clusterId={ClusterId}", id);
            throw K8sExceptionMapper.Translate(ex, "加载节点列表");
        }
        var result = nodeItems.Select(n => MapNode(n, remarks)).ToList();
        _logger.LogInformation("GetClusterNodes done clusterId={ClusterId} count={Count}", id, result.Count);
        return result;
    }

    /// <summary>拉取节点详情(地址/条件/污点/容量/标签/系统信息)。集群状态为 Offline 时直接返回 IsReachable=false 的占位视图;集群不存在返回 null,K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task<ClusterNodeDetailViewModel?> GetNodeDetailAsync(NodeDetailQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;

        if (entity.Status == ClusterStatus.Offline)
        {
            return new ClusterNodeDetailViewModel
            {
                ClusterId = request.ClusterId,
                ClusterName = entity.Name,
                IsReachable = false
            };
        }

        var remarks = BuildRemarkLookup(entity);

        var client = clientCache.GetOrCreate(entity);
        V1Node node;
        try
        {
            node = await client.CoreV1.ReadNodeAsync(request.NodeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReadNode failed clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
            throw K8sExceptionMapper.Translate(ex, "加载节点详情");
        }
        var vm = MapNodeDetail(node, entity, remarks);
        vm.ClusterId = request.ClusterId;
        vm.ClusterName = entity.Name;
        vm.IsReachable = true;
        return vm;
    }

    /// <summary>整体维护某节点各地址的 IP 备注:提交的条目做新增/更新,提交中未出现的既有备注删除;备注超过 64 字符抛 <see cref="ValidationException"/>,成功后写审计。</summary>
    public async Task UpdateNodeIpNotesAsync(NodeIpNotesUpdateRequest request)
    {
        _logger.LogInformation("UpdateNodeIpNotes clusterId={ClusterId} node={NodeName} count={Count}",
            request.ClusterId, request.NodeName, request.Items.Count);
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        var incoming = request.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Address))
            .ToDictionary(i => i.Address, i => i.Note);

        var existing = entity.NodeIpRemarks
            .Where(r => r.NodeName == request.NodeName)
            .ToDictionary(r => r.Address);

        foreach (var (address, note) in incoming)
        {
            if (note is not null && note.Length > 64)
            {
                _logger.LogWarning("NodeIpRemark note too long node={NodeName} address={Address}", request.NodeName, address);
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
                    ClusterId = request.ClusterId,
                    NodeName = request.NodeName,
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

        await _repo.UpdateAsync(entity);
        _logger.LogInformation("UpdateNodeIpNotes persisted clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
        await _auditService.LogAsync(AuditCategory.Node, AuditAction.Update, $"节点： {request.NodeName} @ 集群 {entity.Name}");
    }

    /// <summary>封锁节点:PATCH <c>spec.unschedulable=true</c>,此后调度器不再向该节点派发新 Pod,存量 Pod 不受影响;仅管理员可用,成功后写审计。K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task CordonAsync(NodeMaintenanceRequest request)
        => await SetNodeSchedulableAsync(request, true, AuditAction.Cordon, "封锁节点");

    /// <summary>解封节点:PATCH <c>spec.unschedulable=false</c>,节点恢复参与调度;仅管理员可用,成功后写审计。K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task UncordonAsync(NodeMaintenanceRequest request)
        => await SetNodeSchedulableAsync(request, false, AuditAction.Uncordon, "解封节点");

    /// <summary>排空预检:列出该节点全部 Pod 并按归属分类(控制器托管可迁移/DaemonSet 自动跳过/无控制器裸 Pod),供强确认界面展示;仅管理员可用。K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task<NodeDrainPreflightViewModel> GetNodeDrainPreflightAsync(NodeDrainRequest request)
    {
        EnsureMaintenanceAllowed();
        _logger.LogInformation("NodeDrainPreflight clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        var client = clientCache.GetOrCreate(entity);
        IList<V1Pod> podItems;
        try
        {
            var podList = await client.CoreV1.ListPodForAllNamespacesAsync(
                fieldSelector: $"spec.nodeName={request.NodeName}");
            podItems = podList.Items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NodeDrainPreflight list pods failed clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
            throw K8sExceptionMapper.Translate(ex, "排空预检");
        }

        var result = ClassifyNodePods(request.NodeName, podItems);
        _logger.LogInformation(
            "NodeDrainPreflight done clusterId={ClusterId} node={NodeName} migratable={Migratable} daemonSet={DaemonSet} bare={Bare}",
            request.ClusterId, request.NodeName, result.MigratablePods.Count, result.DaemonSetPods.Count, result.BarePods.Count);
        return result;
    }

    /// <summary>排空节点:先封锁自身(已封锁则不再重复封锁与审计),再逐个对控制器托管 Pod 发起 policy/v1 Eviction 尽力迁移;DaemonSet Pod 跳过、裸 Pod 默认不驱逐,429(PDB)与其他失败计入阻塞不中断,不等待 Pod 清零即返回汇总(成功/跳过/阻塞计数与阻塞清单)并写排空审计。仅管理员可用,无取消回滚。K8s 调用失败经翻译后抛业务异常。</summary>
    public async Task<NodeDrainReportViewModel> DrainNodeAsync(NodeDrainRequest request, IProgress<NodeDrainProgress>? progress = null)
    {
        EnsureMaintenanceAllowed();
        _logger.LogInformation("DrainNode clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        var client = clientCache.GetOrCreate(entity);
        V1Node node;
        try
        {
            node = await client.CoreV1.ReadNodeAsync(request.NodeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DrainNode read node failed clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
            throw K8sExceptionMapper.Translate(ex, "排空节点");
        }

        if ((node.Spec?.Unschedulable ?? false) == false)
        {
            await PatchNodeSchedulableAsync(client, request.NodeName, unschedulable: true, "封锁节点");
            await _auditService.LogAsync(AuditCategory.Node, AuditAction.Cordon, $"节点： {request.NodeName} @ 集群 {entity.Name}");
        }

        IList<V1Pod> podItems;
        try
        {
            var podList = await client.CoreV1.ListPodForAllNamespacesAsync(
                fieldSelector: $"spec.nodeName={request.NodeName}");
            podItems = podList.Items;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DrainNode list pods failed clusterId={ClusterId} node={NodeName}", request.ClusterId, request.NodeName);
            throw K8sExceptionMapper.Translate(ex, "排空节点");
        }

        var classification = ClassifyNodePods(request.NodeName, podItems);
        var targets = classification.MigratablePods;
        var total = targets.Count;
        var evicted = 0;
        var skipped = classification.DaemonSetPods.Count;
        var blocked = 0;
        var blockedPods = new List<DrainPodViewModel>();
        var completed = 0;
        foreach (var target in targets)
        {
            var eviction = new V1Eviction
            {
                Metadata = new V1ObjectMeta { Name = target.Name, NamespaceProperty = target.Namespace }
            };
            try
            {
                await client.CoreV1.CreateNamespacedPodEvictionWithHttpMessagesAsync(
                    eviction, target.Name, target.Namespace,
                    null, null, null, null, null, CancellationToken.None);
                evicted++;
            }
            catch (KubernetesException kex) when (kex.Status?.Code == 429)
            {
                blocked++;
                blockedPods.Add(target);
                _logger.LogWarning("DrainNode pod blocked by disruption budget clusterId={ClusterId} pod={Ns}/{Name}",
                    request.ClusterId, target.Namespace, target.Name);
            }
            catch (Exception ex)
            {
                blocked++;
                blockedPods.Add(target);
                _logger.LogWarning(ex, "DrainNode pod eviction failed clusterId={ClusterId} pod={Ns}/{Name}",
                    request.ClusterId, target.Namespace, target.Name);
            }

            completed++;
            progress?.Report(new NodeDrainProgress(completed, total));
        }

        var report = new NodeDrainReportViewModel
        {
            Evicted = evicted,
            Skipped = skipped,
            Blocked = blocked,
            BlockedPods = blockedPods
        };
        _logger.LogInformation(
            "DrainNode done clusterId={ClusterId} node={NodeName} evicted={Evicted} skipped={Skipped} blocked={Blocked}",
            request.ClusterId, request.NodeName, evicted, skipped, blocked);
        await _auditService.LogAsync(
            AuditCategory.Node, AuditAction.Drain,
            $"节点： {request.NodeName} @ 集群 {entity.Name}（成功 {evicted}/跳过 {skipped}/阻塞 {blocked}）");
        return report;
    }
    // ---- Private k8s helpers ----

    private static NodeDrainPreflightViewModel ClassifyNodePods(string nodeName, IEnumerable<V1Pod> pods)
    {
        var migratable = new List<DrainPodViewModel>();
        var daemonSet = new List<DrainPodViewModel>();
        var bare = new List<DrainPodViewModel>();
        foreach (var pod in pods)
        {
            var metadata = pod.Metadata;
            if (metadata is null)
            {
                continue;
            }

            var vm = new DrainPodViewModel
            {
                Name = metadata.Name ?? "",
                Namespace = metadata.NamespaceProperty ?? "",
                OwnerKind = metadata.OwnerReferences?
                    .Select(o => o.Kind)
                    .FirstOrDefault(kind => kind is not null)
            };
            var owners = metadata.OwnerReferences ?? Enumerable.Empty<V1OwnerReference>();
            if (owners.Any(o => o.Kind == "DaemonSet"))
            {
                daemonSet.Add(vm);
            }
            else if (owners.Any())
            {
                migratable.Add(vm);
            }
            else
            {
                bare.Add(vm);
            }
        }

        return new NodeDrainPreflightViewModel
        {
            NodeName = nodeName,
            MigratablePods = migratable.OrderBy(p => p.Namespace).ThenBy(p => p.Name, StringComparer.Ordinal).ToList(),
            DaemonSetPods = daemonSet.OrderBy(p => p.Namespace).ThenBy(p => p.Name, StringComparer.Ordinal).ToList(),
            BarePods = bare.OrderBy(p => p.Namespace).ThenBy(p => p.Name, StringComparer.Ordinal).ToList()
        };
    }

    private void EnsureMaintenanceAllowed()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.IsInRole("Admin") == true)
        {
            return;
        }

        _logger.LogWarning("Node maintenance denied user={User}", user?.Identity?.Name ?? "(anonymous)");
        throw new PermissionException("节点维护操作仅管理员可用");
    }

    private async Task SetNodeSchedulableAsync(NodeMaintenanceRequest request, bool unschedulable, AuditAction action, string operation)
    {
        EnsureMaintenanceAllowed();
        _logger.LogInformation("{Operation} clusterId={ClusterId} node={NodeName}", operation, request.ClusterId, request.NodeName);
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        var client = clientCache.GetOrCreate(entity);
        await PatchNodeSchedulableAsync(client, request.NodeName, unschedulable, operation);
        _logger.LogInformation("{Operation} done clusterId={ClusterId} node={NodeName}", operation, request.ClusterId, request.NodeName);
        await _auditService.LogAsync(AuditCategory.Node, action, $"节点： {request.NodeName} @ 集群 {entity.Name}");
    }

    private static async Task PatchNodeSchedulableAsync(IKubernetes client, string nodeName, bool unschedulable, string operation)
    {
        var patch = new V1Patch(
            KubernetesJson.Serialize(new V1Node { Spec = new V1NodeSpec { Unschedulable = unschedulable } }),
            V1Patch.PatchType.StrategicMergePatch);
        try
        {
            await client.CoreV1.PatchNodeAsync(patch, nodeName);
        }
        catch (Exception ex)
        {
            throw K8sExceptionMapper.Translate(ex, operation);
        }
    }

    private static Dictionary<(string NodeName, string Address), string?> BuildRemarkLookup(ClusterInfo cluster)
    {
        return cluster.NodeIpRemarks.ToDictionary(
            r => (r.NodeName, r.Address),
            r => (string?)r.Note);
    }

    private static bool IsIpClassAddress(string type)
        => _IpAddressTypes.Contains(type);

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
            Resources = MapResources(node.Status?.Capacity, node.Status?.Allocatable),

            // 标签 & 注解
            Labels = node.Metadata?.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),
            Annotations = node.Metadata?.Annotations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new(),

            // 系统信息
            SystemInfo = MapSystemInfo(node.Status?.NodeInfo),

            // YAML
            Yaml = KubernetesYaml.Serialize(node)
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

    private static List<NodeResourceViewModel> MapResources(
        IDictionary<string, ResourceQuantity>? capacity,
        IDictionary<string, ResourceQuantity>? allocatable)
    {
        capacity ??= new Dictionary<string, ResourceQuantity>();
        allocatable ??= new Dictionary<string, ResourceQuantity>();
        return capacity.Keys
            .Union(allocatable.Keys, StringComparer.Ordinal)
            .OrderBy(ResourceSortOrder)
            .ThenBy(key => key, StringComparer.Ordinal)
            .Select(key =>
            {
                var capacityValue = capacity.TryGetValue(key, out var cap) ? cap : null;
                var allocatableValue = allocatable.TryGetValue(key, out var alloc) ? alloc : null;
                return new NodeResourceViewModel
                {
                    Key = key,
                    Label = ResourceLabel(key),
                    CapacityRaw = capacityValue?.ToString(),
                    CapacityText = FormatResource(key, capacityValue),
                    AllocatableRaw = allocatableValue?.ToString(),
                    AllocatableText = FormatResource(key, allocatableValue),
                    AllocatablePercent = ComputeAllocatablePercent(capacityValue, allocatableValue)
                };
            })
            .ToList();
    }

    private static string ResourceLabel(string key) => key switch
    {
        "cpu" => "CPU",
        "memory" => "内存",
        "ephemeral-storage" => "临时存储",
        "pods" => "Pod",
        _ when IsHugePages(key) => "大页",
        _ => key
    };

    private static int ResourceSortOrder(string key) => key switch
    {
        "cpu" => 0,
        "memory" => 1,
        "ephemeral-storage" => 2,
        "pods" => 3,
        _ when IsHugePages(key) => 4,
        _ => 5
    };

    private static bool IsHugePages(string key) => key.StartsWith("hugepages-", StringComparison.Ordinal);

    private static string FormatResource(string key, ResourceQuantity? quantity)
    {
        if (quantity is null)
        {
            return "—";
        }

        var raw = quantity.ToString();
        if (!TryToDecimal(quantity, out var value))
        {
            return string.IsNullOrEmpty(raw) ? "—" : raw;
        }

        if (key == "cpu")
        {
            return $"{value.ToString("0.###", CultureInfo.InvariantCulture)} 核";
        }

        if (key == "memory" || key == "ephemeral-storage" || IsHugePages(key))
        {
            return FormatBytes(value);
        }

        if (key == "pods")
        {
            return $"{value.ToString("0", CultureInfo.InvariantCulture)} 个";
        }

        return string.IsNullOrEmpty(raw) ? "—" : raw;
    }

    private static string FormatBytes(decimal bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];
        var unit = 0;
        while (bytes >= 1024m && unit < units.Length - 1)
        {
            bytes /= 1024m;
            unit++;
        }

        return $"{bytes.ToString("0.#", CultureInfo.InvariantCulture)} {units[unit]}";
    }

    private static double? ComputeAllocatablePercent(ResourceQuantity? capacity, ResourceQuantity? allocatable)
    {
        if (capacity is null || allocatable is null)
        {
            return null;
        }

        if (!TryToDecimal(capacity, out var capacityValue) ||
            !TryToDecimal(allocatable, out var allocatableValue) ||
            capacityValue <= 0)
        {
            return null;
        }

        return Math.Clamp(Math.Round((double)(allocatableValue / capacityValue * 100m), 1), 0, 100);
    }

    private static bool TryToDecimal(ResourceQuantity quantity, out decimal value)
    {
        try
        {
            value = quantity.ToDecimal();
            return true;
        }
        catch (OverflowException)
        {
            value = 0;
            return false;
        }
    }
}
