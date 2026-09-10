using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;
namespace MultiClusterMgmtSys.Services;

public class SvcService(ClusterRepository repo, AuditService auditService, ILogger<SvcService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private readonly ClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<SvcService> logger = logger;

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

    public async Task<List<SvcListViewModel>> ListServicesAsync(SvcQueryRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.CoreV1.ListServiceForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedServiceAsync(request.Namespace);
            return list.Items.Select(s => s.ToSvcListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListServices failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载服务列表");
        }
    }

    public async Task<SvcDetailViewModel?> GetServiceAsync(SvcKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var svc = await client.CoreV1.ReadNamespacedServiceAsync(request.Name, request.Namespace);
            return svc.ToSvcDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadService failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载服务详情");
        }
    }

    public async Task<List<SvcEndpointViewModel>> GetServiceEndpointsAsync(SvcKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return new();
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        var labelSelector = $"{SvcMappingExtensions.EndpointSliceServiceLabel}={request.Name}";
        try
        {
            var slices = await client.DiscoveryV1.ListNamespacedEndpointSliceAsync(request.Namespace, labelSelector: labelSelector);
            return slices.ToSvcEndpointViewModels();
        }
        catch (KubernetesException kex) when (kex.Status?.Code == 404)
        {
            logger.LogInformation("EndpointSlice API unavailable, falling back to legacy Endpoints clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            return await ListLegacyEndpointsAsync(client, request.Namespace, request.Name);
        }
        catch (HttpOperationException hopex) when (hopex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("EndpointSlice API unavailable, falling back to legacy Endpoints clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            return await ListLegacyEndpointsAsync(client, request.Namespace, request.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListEndpointSlices failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载服务后端列表");
        }
    }

    private async Task<List<SvcEndpointViewModel>> ListLegacyEndpointsAsync(IKubernetes client, string ns, string name)
    {
        try
        {
            var list = await client.CoreV1.ListNamespacedEndpointsAsync(ns);
            return list.Items
                .Where(e => e.Metadata?.Name == name)
                .SelectMany(e => e.ToSvcEndpointViewModels())
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListLegacyEndpoints failed ns={Namespace} name={Name}", ns, name);
            throw K8sExceptionMapper.Translate(ex, "加载服务后端列表");
        }
    }

    public async Task DeleteServiceAsync(SvcKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.CoreV1.DeleteNamespacedServiceAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteService failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除服务");
        }
        await auditService.LogAsync(AuditCategory.Service, AuditAction.Delete, $"服务: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    public async Task UpdateServiceFromYamlAsync(SvcUpdateRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        V1Service deserialized;
        try
        {
            deserialized = KubernetesYaml.Deserialize<V1Service>(request.Yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        V1Service existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedServiceAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadService failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载服务详情");
        }

        GuardImmutableFields(deserialized.Spec, existing.Spec);

        deserialized.Metadata.Name = request.Name;
        deserialized.Metadata.NamespaceProperty = request.Namespace;
        deserialized.Metadata.ResourceVersion = existing.Metadata?.ResourceVersion;
        deserialized.Metadata.Uid = existing.Metadata?.Uid;
        deserialized.Status = existing.Status;
        try
        {
            await client.CoreV1.ReplaceNamespacedServiceAsync(deserialized, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceService failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存服务");
        }
        await auditService.LogAsync(AuditCategory.Service, AuditAction.Update, $"服务: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    public async Task CreateServiceFromYamlAsync(SvcCreateRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        V1Service body;
        try
        {
            body = KubernetesYaml.Deserialize<V1Service>(request.Yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for create clusterId={ClusterId}", request.ClusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        var ns = body.Metadata?.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(ns))
            throw new ValidationException("YAML 未指定 metadata.namespace");
        try
        {
            await client.CoreV1.CreateNamespacedServiceAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateService failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建服务");
        }
        await auditService.LogAsync(AuditCategory.Service, AuditAction.Create, $"服务: {ns}/{body.Metadata?.Name ?? "未知"} @ 集群 {entity.Name}");
    }

    private static void GuardImmutableFields(V1ServiceSpec? incoming, V1ServiceSpec? existing)
    {
        if (incoming is null || existing is null) return;

        if (!string.IsNullOrWhiteSpace(incoming.ClusterIP) && incoming.ClusterIP != existing.ClusterIP)
            throw new ValidationException("clusterIP 为不可变字段,如需更换请删除服务后重建");

        if (incoming.ClusterIPs is { Count: > 0 } && !SequenceEquals(incoming.ClusterIPs, existing.ClusterIPs))
            throw new ValidationException("clusterIPs 为不可变字段,如需更换请删除服务后重建");

        if (incoming.IpFamilies is { Count: > 0 } && !SequenceEquals(incoming.IpFamilies, existing.IpFamilies))
            throw new ValidationException("ipFamilies 为不可变字段,如需更换请删除服务后重建");

        incoming.ClusterIP ??= existing.ClusterIP;
        if (incoming.ClusterIPs is not { Count: > 0 })
            incoming.ClusterIPs = existing.ClusterIPs;
        if (incoming.IpFamilies is not { Count: > 0 })
            incoming.IpFamilies = existing.IpFamilies;
    }

    private static bool SequenceEquals<T>(IList<T>? left, IList<T>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.SequenceEqual(right);
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
