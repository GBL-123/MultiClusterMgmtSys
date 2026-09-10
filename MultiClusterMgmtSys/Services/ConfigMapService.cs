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

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// ConfigMap(配置)管理服务:基于所选集群的凭据实时读写 k8s ConfigMap。
/// 支持按命名空间查询、YAML 创建/更新(仅覆盖 data/binaryData)、删除,成功后写审计。
/// </summary>
public class ConfigMapService(ClusterRepository repo, AuditService auditService, ILogger<ConfigMapService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private readonly ClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<ConfigMapService> logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供配置页命名空间下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
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

    /// <summary>查询配置列表;Namespace 为 null 时查全部命名空间。集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(ConfigMapQueryRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var list = request.Namespace is null
                ? await client.CoreV1.ListConfigMapForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedConfigMapAsync(request.Namespace);
            return list.Items.Select(cm => cm.ToConfigMapListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListConfigMaps failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载配置列表");
        }
    }

    /// <summary>读取单个配置详情(含 data/binaryData);集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<ConfigMapDetailViewModel?> GetConfigMapAsync(ConfigMapKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            var cm = await client.CoreV1.ReadNamespacedConfigMapAsync(request.Name, request.Namespace);
            return cm.ToConfigMapDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载配置详情");
        }
    }

    /// <summary>删除指定配置,成功后写删除审计;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task DeleteConfigMapAsync(ConfigMapKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        try
        {
            await client.CoreV1.DeleteNamespacedConfigMapAsync(request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除配置");
        }
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Delete, $"配置: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>以 YAML 更新既有配置:反序列化后仅覆盖 data/binaryData 字段,携带服务器最新对象替换提交;YAML 非法抛 <see cref="ValidationException"/>,成功后写更新审计。</summary>
    public async Task UpdateConfigMapFromYamlAsync(ConfigMapUpdateRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        V1ConfigMap deserialized;
        try
        {
            deserialized = KubernetesYaml.Deserialize<V1ConfigMap>(request.Yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        try
        {
            var existing = await client.CoreV1.ReadNamespacedConfigMapAsync(request.Name, request.Namespace);
            existing.Data = deserialized.Data;
            existing.BinaryData = deserialized.BinaryData;
            await client.CoreV1.ReplaceNamespacedConfigMapAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存配置");
        }
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Update, $"配置: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>以 YAML 创建配置,命名空间取自 YAML 的 metadata.namespace;YAML 非法或未指定命名空间抛 <see cref="ValidationException"/>,成功后写创建审计。</summary>
    public async Task CreateConfigMapFromYamlAsync(ConfigMapCreateRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = clientFactory(config);
        V1ConfigMap body;
        try
        {
            body = KubernetesYaml.Deserialize<V1ConfigMap>(request.Yaml);
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
            await client.CoreV1.CreateNamespacedConfigMapAsync(body, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateConfigMap failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建配置");
        }
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Create, $"配置: {ns}/{body.Metadata?.Name ?? "未知"} @ 集群 {entity.Name}");
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
