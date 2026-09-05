using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;
using System.Text.Json;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// apps/v1 工作负载管理服务。方法按类型显式展开:
/// 扩缩容仅提供 Deployment/StatefulSet/ReplicaSet 版本,滚动重启仅提供 Deployment/StatefulSet/DaemonSet 版本(design D3/D4)。
/// </summary>
public class WorkloadService(ClusterRepository repo, AuditService auditService, ILogger<WorkloadService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private const string RestartedAtAnnotation = "kubectl.kubernetes.io/restartedAt";

    private readonly ClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<WorkloadService> logger = logger;

    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var nsList = await client.CoreV1.ListNamespaceAsync();
            return nsList.Items.Select(n => n.Metadata?.Name ?? "").OrderBy(n => n).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListNamespaces failed clusterId={ClusterId}", clusterId);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间");
        }
    }

    public async Task<List<WorkloadListViewModel>> ListDeploymentsAsync(WorkloadQueryRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.AppsV1.ListDeploymentForAllNamespacesAsync()
                : await client.AppsV1.ListNamespacedDeploymentAsync(request.Namespace);
            return list.Items.Select(d => d.ToWorkloadListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListDeployments failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载部署列表");
        }
    }

    public async Task<List<WorkloadListViewModel>> ListStatefulSetsAsync(WorkloadQueryRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.AppsV1.ListStatefulSetForAllNamespacesAsync()
                : await client.AppsV1.ListNamespacedStatefulSetAsync(request.Namespace);
            return list.Items.Select(s => s.ToWorkloadListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListStatefulSets failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载有状态应用列表");
        }
    }

    public async Task<List<WorkloadListViewModel>> ListDaemonSetsAsync(WorkloadQueryRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.AppsV1.ListDaemonSetForAllNamespacesAsync()
                : await client.AppsV1.ListNamespacedDaemonSetAsync(request.Namespace);
            return list.Items.Select(d => d.ToWorkloadListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListDaemonSets failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载守护进程列表");
        }
    }

    public async Task<List<WorkloadListViewModel>> ListReplicaSetsAsync(WorkloadQueryRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.AppsV1.ListReplicaSetForAllNamespacesAsync()
                : await client.AppsV1.ListNamespacedReplicaSetAsync(request.Namespace);
            return list.Items.Select(r => r.ToWorkloadListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListReplicaSets failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载副本集列表");
        }
    }

    public async Task<WorkloadDetailViewModel?> GetDeploymentAsync(WorkloadKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var dep = await client.AppsV1.ReadNamespacedDeploymentAsync(request.Name, request.Namespace);
            return dep.ToWorkloadDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadDeployment failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载部署详情");
        }
    }

    public async Task<WorkloadDetailViewModel?> GetStatefulSetAsync(WorkloadKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var sts = await client.AppsV1.ReadNamespacedStatefulSetAsync(request.Name, request.Namespace);
            return sts.ToWorkloadDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadStatefulSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载有状态应用详情");
        }
    }

    public async Task<WorkloadDetailViewModel?> GetDaemonSetAsync(WorkloadKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var ds = await client.AppsV1.ReadNamespacedDaemonSetAsync(request.Name, request.Namespace);
            return ds.ToWorkloadDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadDaemonSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载守护进程详情");
        }
    }

    public async Task<WorkloadDetailViewModel?> GetReplicaSetAsync(WorkloadKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var rs = await client.AppsV1.ReadNamespacedReplicaSetAsync(request.Name, request.Namespace);
            return rs.ToWorkloadDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadReplicaSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载副本集详情");
        }
    }

    public async Task CreateDeploymentFromYamlAsync(WorkloadCreateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var body = DeserializeOrThrow<V1Deployment>(request.Yaml, "创建部署", request.ClusterId);
        var ns = RequireNamespace(body.Metadata?.NamespaceProperty);
        try
        {
            await client.AppsV1.CreateNamespacedDeploymentAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateDeployment failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建部署");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Create, AuditTarget(WorkloadKind.Deployment, ns, body.Metadata?.Name, entity.Name));
    }

    public async Task CreateStatefulSetFromYamlAsync(WorkloadCreateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var body = DeserializeOrThrow<V1StatefulSet>(request.Yaml, "创建有状态应用", request.ClusterId);
        var ns = RequireNamespace(body.Metadata?.NamespaceProperty);
        try
        {
            await client.AppsV1.CreateNamespacedStatefulSetAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateStatefulSet failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建有状态应用");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Create, AuditTarget(WorkloadKind.StatefulSet, ns, body.Metadata?.Name, entity.Name));
    }

    public async Task CreateDaemonSetFromYamlAsync(WorkloadCreateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var body = DeserializeOrThrow<V1DaemonSet>(request.Yaml, "创建守护进程", request.ClusterId);
        var ns = RequireNamespace(body.Metadata?.NamespaceProperty);
        try
        {
            await client.AppsV1.CreateNamespacedDaemonSetAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateDaemonSet failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建守护进程");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Create, AuditTarget(WorkloadKind.DaemonSet, ns, body.Metadata?.Name, entity.Name));
    }

    public async Task CreateReplicaSetFromYamlAsync(WorkloadCreateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var body = DeserializeOrThrow<V1ReplicaSet>(request.Yaml, "创建副本集", request.ClusterId);
        var ns = RequireNamespace(body.Metadata?.NamespaceProperty);
        try
        {
            await client.AppsV1.CreateNamespacedReplicaSetAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateReplicaSet failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建副本集");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Create, AuditTarget(WorkloadKind.ReplicaSet, ns, body.Metadata?.Name, entity.Name));
    }

    public async Task UpdateDeploymentFromYamlAsync(WorkloadUpdateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var deserialized = DeserializeOrThrow<V1Deployment>(request.Yaml, "保存部署", request.ClusterId);
        try
        {
            // 方案 A:读最新对象,仅覆盖 spec(metadata/status 以服务器为准),携带最新 resourceVersion 替换(design D5)
            var existing = await client.AppsV1.ReadNamespacedDeploymentAsync(request.Name, request.Namespace);
            existing.Spec = deserialized.Spec;
            await client.AppsV1.ReplaceNamespacedDeploymentAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceDeployment failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存部署");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Update, AuditTarget(WorkloadKind.Deployment, request.Namespace, request.Name, entity.Name));
    }

    public async Task UpdateStatefulSetFromYamlAsync(WorkloadUpdateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var deserialized = DeserializeOrThrow<V1StatefulSet>(request.Yaml, "保存有状态应用", request.ClusterId);
        try
        {
            var existing = await client.AppsV1.ReadNamespacedStatefulSetAsync(request.Name, request.Namespace);
            existing.Spec = deserialized.Spec;
            await client.AppsV1.ReplaceNamespacedStatefulSetAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceStatefulSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存有状态应用");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Update, AuditTarget(WorkloadKind.StatefulSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task UpdateDaemonSetFromYamlAsync(WorkloadUpdateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var deserialized = DeserializeOrThrow<V1DaemonSet>(request.Yaml, "保存守护进程", request.ClusterId);
        try
        {
            var existing = await client.AppsV1.ReadNamespacedDaemonSetAsync(request.Name, request.Namespace);
            existing.Spec = deserialized.Spec;
            await client.AppsV1.ReplaceNamespacedDaemonSetAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceDaemonSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存守护进程");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Update, AuditTarget(WorkloadKind.DaemonSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task UpdateReplicaSetFromYamlAsync(WorkloadUpdateRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var deserialized = DeserializeOrThrow<V1ReplicaSet>(request.Yaml, "保存副本集", request.ClusterId);
        try
        {
            var existing = await client.AppsV1.ReadNamespacedReplicaSetAsync(request.Name, request.Namespace);
            existing.Spec = deserialized.Spec;
            await client.AppsV1.ReplaceNamespacedReplicaSetAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceReplicaSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存副本集");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Update, AuditTarget(WorkloadKind.ReplicaSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task DeleteDeploymentAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.AppsV1.DeleteNamespacedDeploymentAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteDeployment failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除部署");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Delete, AuditTarget(WorkloadKind.Deployment, request.Namespace, request.Name, entity.Name));
    }

    public async Task DeleteStatefulSetAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.AppsV1.DeleteNamespacedStatefulSetAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteStatefulSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除有状态应用");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Delete, AuditTarget(WorkloadKind.StatefulSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task DeleteDaemonSetAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.AppsV1.DeleteNamespacedDaemonSetAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteDaemonSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除守护进程");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Delete, AuditTarget(WorkloadKind.DaemonSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task DeleteReplicaSetAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.AppsV1.DeleteNamespacedReplicaSetAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteReplicaSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除副本集");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Delete, AuditTarget(WorkloadKind.ReplicaSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task ScaleDeploymentAsync(WorkloadScaleRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var scale = await client.AppsV1.ReadNamespacedDeploymentScaleAsync(request.Name, request.Namespace);
            scale.Spec = scale.Spec ?? new V1ScaleSpec();
            scale.Spec.Replicas = request.Replicas;
            await client.AppsV1.ReplaceNamespacedDeploymentScaleAsync(scale, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ScaleDeployment failed clusterId={ClusterId} ns={Namespace} name={Name} replicas={Replicas}",
                request.ClusterId, request.Namespace, request.Name, request.Replicas);
            throw K8sExceptionMapper.Translate(ex, "扩缩容部署");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Scale, ScaleTarget(WorkloadKind.Deployment, request, entity.Name));
    }

    public async Task ScaleStatefulSetAsync(WorkloadScaleRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var scale = await client.AppsV1.ReadNamespacedStatefulSetScaleAsync(request.Name, request.Namespace);
            scale.Spec = scale.Spec ?? new V1ScaleSpec();
            scale.Spec.Replicas = request.Replicas;
            await client.AppsV1.ReplaceNamespacedStatefulSetScaleAsync(scale, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ScaleStatefulSet failed clusterId={ClusterId} ns={Namespace} name={Name} replicas={Replicas}",
                request.ClusterId, request.Namespace, request.Name, request.Replicas);
            throw K8sExceptionMapper.Translate(ex, "扩缩容有状态应用");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Scale, ScaleTarget(WorkloadKind.StatefulSet, request, entity.Name));
    }

    public async Task ScaleReplicaSetAsync(WorkloadScaleRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var scale = await client.AppsV1.ReadNamespacedReplicaSetScaleAsync(request.Name, request.Namespace);
            scale.Spec = scale.Spec ?? new V1ScaleSpec();
            scale.Spec.Replicas = request.Replicas;
            await client.AppsV1.ReplaceNamespacedReplicaSetScaleAsync(scale, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ScaleReplicaSet failed clusterId={ClusterId} ns={Namespace} name={Name} replicas={Replicas}",
                request.ClusterId, request.Namespace, request.Name, request.Replicas);
            throw K8sExceptionMapper.Translate(ex, "扩缩容副本集");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Scale, ScaleTarget(WorkloadKind.ReplicaSet, request, entity.Name));
    }

    public async Task RestartDeploymentAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var patch = new V1Patch(BuildRestartPatchJson(), V1Patch.PatchType.StrategicMergePatch);
        try
        {
            await client.AppsV1.PatchNamespacedDeploymentAsync(patch, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RestartDeployment failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "重启部署");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Restart, AuditTarget(WorkloadKind.Deployment, request.Namespace, request.Name, entity.Name));
    }

    public async Task RestartStatefulSetAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var patch = new V1Patch(BuildRestartPatchJson(), V1Patch.PatchType.StrategicMergePatch);
        try
        {
            await client.AppsV1.PatchNamespacedStatefulSetAsync(patch, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RestartStatefulSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "重启有状态应用");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Restart, AuditTarget(WorkloadKind.StatefulSet, request.Namespace, request.Name, entity.Name));
    }

    public async Task RestartDaemonSetAsync(WorkloadKeyRequest request)
    {
        var entity = await RequireClusterAsync(request.ClusterId);
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var patch = new V1Patch(BuildRestartPatchJson(), V1Patch.PatchType.StrategicMergePatch);
        try
        {
            await client.AppsV1.PatchNamespacedDaemonSetAsync(patch, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RestartDaemonSet failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "重启守护进程");
        }
        await auditService.LogAsync(AuditCategory.Workload, AuditAction.Restart, AuditTarget(WorkloadKind.DaemonSet, request.Namespace, request.Name, entity.Name));
    }

    private async Task<ClusterInfo> RequireClusterAsync(int clusterId)
        => await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");

    private T DeserializeOrThrow<T>(string yaml, string operation, int clusterId) where T : class
    {
        try
        {
            return KubernetesYaml.Deserialize<T>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for {Operation} clusterId={ClusterId}", operation, clusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
    }

    private static string RequireNamespace(string? ns)
    {
        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new ValidationException("YAML 未指定 metadata.namespace");
        }

        return ns;
    }

    private static string AuditTarget(WorkloadKind kind, string? ns, string? name, string clusterName)
        => $"{kind.ToDisplayText()}: {ns}/{name} @ 集群 {clusterName}";

    private static string ScaleTarget(WorkloadKind kind, WorkloadScaleRequest request, string clusterName)
        => $"扩缩容 {kind.ToDisplayText()} {request.Namespace}/{request.Name} → {request.Replicas} @ 集群 {clusterName}";

    private static string BuildRestartPatchJson()
    {
        var annotation = new Dictionary<string, string>
        {
            [RestartedAtAnnotation] = DateTime.UtcNow.ToString("o")
        };
        return JsonSerializer.Serialize(new
        {
            spec = new
            {
                template = new
                {
                    metadata = new
                    {
                        annotations = annotation
                    }
                }
            }
        });
    }

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
}
