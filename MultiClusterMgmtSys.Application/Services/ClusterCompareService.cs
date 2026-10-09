using Microsoft.Extensions.Logging;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Common.Yaml;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 跨集群同名资源对照:读取两侧 YAML、判定差异并在满足条件时把源资源克隆到目标集群(契约见 cross-cluster-compare)。
/// 读取与克隆复用既有 WorkloadService / ConfigMapService 能力,不新增 K8s 调用形态。
/// </summary>
public class ClusterCompareService
{
    private readonly IClusterRepository _repo;

    private readonly WorkloadService _workloadService;

    private readonly ConfigMapService _configMapService;

    private readonly ILogger<ClusterCompareService> _logger;

    /// <summary>创建对照服务;依赖集群仓库与既有的工作负载 / ConfigMap 服务。</summary>
    public ClusterCompareService(
        IClusterRepository repo,
        WorkloadService workloadService,
        ConfigMapService configMapService,
        ILogger<ClusterCompareService> logger)
    {
        _repo = repo;
        _workloadService = workloadService;
        _configMapService = configMapService;
        _logger = logger;
    }

    /// <summary>列举源集群指定族与命名空间下的资源名,供对照页选择对象。</summary>
    public async Task<List<string>> GetSourceResourceNamesAsync(int clusterId, CompareKind kind, string namespaceName)
    {
        return (kind switch
        {
            CompareKind.ConfigMap => (await _configMapService.ListConfigMapsAsync(new ConfigMapQueryRequest(clusterId, namespaceName)))
                .Select(x => x.Name),
            CompareKind.Deployment => (await _workloadService.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, namespaceName)))
                .Select(x => x.Name),
            CompareKind.StatefulSet => (await _workloadService.ListStatefulSetsAsync(new WorkloadQueryRequest(clusterId, namespaceName)))
                .Select(x => x.Name),
            CompareKind.DaemonSet => (await _workloadService.ListDaemonSetsAsync(new WorkloadQueryRequest(clusterId, namespaceName)))
                .Select(x => x.Name),
            CompareKind.ReplicaSet => (await _workloadService.ListReplicaSetsAsync(new WorkloadQueryRequest(clusterId, namespaceName)))
                .Select(x => x.Name),
            _ => System.Linq.Enumerable.Empty<string>()
        }
        )
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToList();
    }

    /// <summary>对照两侧同名资源:并行读取 YAML 并标注存在性、差异与可克隆状态;任一侧资源不存在标注为「不存在」而非报错。</summary>
    public async Task<ComparePairViewModel> GetPairAsync(ComparePairQueryRequest request)
    {
        if (request.SourceClusterId == request.TargetClusterId)
        {
            throw new ValidationException("请选择不同的集群进行对照");
        }

        var (sourceName, targetName) = await ResolveClusterNamesAsync(request.SourceClusterId, request.TargetClusterId);

        var sourceTask = ReadSideAsync(request.Kind, request.SourceClusterId, request.Namespace, request.Name);
        var targetTask = ReadSideAsync(request.Kind, request.TargetClusterId, request.Namespace, request.Name);
        await Task.WhenAll(sourceTask, targetTask);
        var source = sourceTask.Result;
        var target = targetTask.Result;

        var hasDifference = source.Exists && target.Exists
            && !string.Equals(source.Yaml, target.Yaml, StringComparison.Ordinal);

        return new ComparePairViewModel
        {
            Namespace = request.Namespace,
            Name = request.Name,
            SourceClusterName = sourceName,
            TargetClusterName = targetName,
            SourceExists = source.Exists,
            SourceYaml = source.Yaml,
            TargetExists = target.Exists,
            TargetYaml = target.Yaml,
            HasDifference = hasDifference,
            CanClone = source.Exists && source.CanOperate && (!target.Exists || hasDifference)
        };
    }

    /// <summary>克隆预览:读取源资源并返回剥离服务器侧元数据后的 YAML(确认对话框展示用,不写目标集群)。</summary>
    public async Task<string> GetClonePreviewAsync(CompareCloneRequest request)
    {
        var (_, sourceName) = await ResolveClusterNamesAsync(request.TargetClusterId, request.SourceClusterId);
        var source = await ReadSideAsync(request.Kind, request.SourceClusterId, request.Namespace, request.Name);
        if (!source.Exists)
        {
            throw new NotFoundException($"源资源不存在或已被删除:{request.Name}");
        }

        return StripServerMetadata(source.Yaml, request.Kind, sourceName, request.Name);
    }

    /// <summary>一键克隆:读取源资源、剥离服务器侧元数据后按既有创建服务路径写入目标集群;目标已存在会由创建路径以冲突语义拒绝。</summary>
    public async Task CloneAsync(CompareCloneRequest request)
    {
        var (sourceName, _) = await ResolveClusterNamesAsync(request.SourceClusterId, request.TargetClusterId);
        var source = await ReadSideAsync(request.Kind, request.SourceClusterId, request.Namespace, request.Name);
        if (!source.Exists)
        {
            throw new NotFoundException($"源资源不存在或已被删除:{request.Name}");
        }

        var yaml = StripServerMetadata(source.Yaml, request.Kind, sourceName, request.Name);
        switch (request.Kind)
        {
            case CompareKind.ConfigMap:
                await _configMapService.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(request.TargetClusterId, yaml));
                break;
            case CompareKind.Deployment:
                await _workloadService.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(request.TargetClusterId, yaml));
                break;
            case CompareKind.StatefulSet:
                await _workloadService.CreateStatefulSetFromYamlAsync(new WorkloadCreateRequest(request.TargetClusterId, yaml));
                break;
            case CompareKind.DaemonSet:
                await _workloadService.CreateDaemonSetFromYamlAsync(new WorkloadCreateRequest(request.TargetClusterId, yaml));
                break;
            case CompareKind.ReplicaSet:
                await _workloadService.CreateReplicaSetFromYamlAsync(new WorkloadCreateRequest(request.TargetClusterId, yaml));
                break;
            default:
                throw new ValidationException("暂不支持该资源族");
        }
    }

    /// <summary>解析两侧集群名(对照页可展示中文集群名);不存在抛业务异常。</summary>
    private async Task<(string SourceName, string TargetName)> ResolveClusterNamesAsync(int sourceClusterId, int targetClusterId)
    {
        var source = await _repo.GetByIdAsync(sourceClusterId)
            ?? throw new NotFoundException($"集群 {sourceClusterId} 不存在");
        var target = await _repo.GetByIdAsync(targetClusterId)
            ?? throw new NotFoundException($"集群 {targetClusterId} 不存在");
        return (source.Name, target.Name);
    }

    /// <summary>按资源族读取单侧资源 YAML;资源缺失(不存在或 404)返回不存在的判定,其余异常原样上抛。</summary>
    /// <param name="kind">资源族。</param>
    /// <param name="clusterId">目标集群 Id。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="name">资源名称。</param>
    /// <returns>(是否存在, 原始 YAML, 当前用户可否操作源)。</returns>
    private async Task<(bool Exists, string Yaml, bool CanOperate)> ReadSideAsync(CompareKind kind, int clusterId, string namespaceName, string name)
    {
        try
        {
            (string? Yaml, bool CanOperate)? detail = kind switch
            {
                CompareKind.ConfigMap =>
                    (await _configMapService.GetConfigMapAsync(new ConfigMapKeyRequest(clusterId, name, namespaceName))) is { } cm
                        ? (cm.Yaml, cm.CanOperate)
                        : null,
                CompareKind.Deployment =>
                    (await _workloadService.GetDeploymentAsync(new WorkloadKeyRequest(clusterId, name, namespaceName))) is { } dep
                        ? (dep.Yaml, dep.CanOperate)
                        : null,
                CompareKind.StatefulSet =>
                    (await _workloadService.GetStatefulSetAsync(new WorkloadKeyRequest(clusterId, name, namespaceName))) is { } sts
                        ? (sts.Yaml, sts.CanOperate)
                        : null,
                CompareKind.DaemonSet =>
                    (await _workloadService.GetDaemonSetAsync(new WorkloadKeyRequest(clusterId, name, namespaceName))) is { } ds
                        ? (ds.Yaml, ds.CanOperate)
                        : null,
                CompareKind.ReplicaSet =>
                    (await _workloadService.GetReplicaSetAsync(new WorkloadKeyRequest(clusterId, name, namespaceName))) is { } rs
                        ? (rs.Yaml, rs.CanOperate)
                        : null,
                _ => null
            };
            return detail is null ? (false, "", false) : (true, detail.Value.Yaml, detail.Value.CanOperate);
        }
        catch (NotFoundException)
        {
            return (false, "", false);
        }
    }

    /// <summary>剥离服务器侧元数据(uid/resourceVersion/creationTimestamp/managedFields/status/归属盖章),使 YAML 可在目标集群作为新对象创建;剥离逻辑统一走 <see cref="ServerYamlSanitizer"/>。</summary>
    /// <param name="sourceYaml">源集群原始 YAML。</param>
    /// <param name="kind">资源族。</param>
    /// <param name="clusterName">源集群显示名(日志用)。</param>
    /// <param name="name">资源名称(日志用)。</param>
    /// <returns>清理后的 YAML 文本;剥离失败时保留原文交由创建路径校验。</returns>
    private string StripServerMetadata(string sourceYaml, CompareKind kind, string clusterName, string name)
    {
        try
        {
            return ServerYamlSanitizer.Sanitize(sourceYaml, kind);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "StripServerMetadata failed cluster={Cluster} name={Name}", clusterName, name);
            return sourceYaml;
        }
    }
}
