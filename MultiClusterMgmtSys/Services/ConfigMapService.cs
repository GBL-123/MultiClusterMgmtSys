using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;

namespace MultiClusterMgmtSys.Services;

public class ConfigMapService(ClusterRepository repo, AuditService auditService, ILogger<ConfigMapService> logger)
{
    private readonly ClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<ConfigMapService> logger = logger;

    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
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

    public async Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(int clusterId, string? ns)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        try
        {
            var list = ns is null
                ? await client.CoreV1.ListConfigMapForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedConfigMapAsync(ns);
            return list.Items.Select(cm => cm.ToConfigMapListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListConfigMaps failed clusterId={ClusterId} ns={Namespace}", clusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "加载配置列表");
        }
    }

    public async Task<ConfigMapDetailViewModel?> GetConfigMapAsync(int clusterId, string name, string ns)
    {
        var entity = await repo.GetByIdAsync(clusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        try
        {
            var cm = await client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);
            return cm.ToConfigMapDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}", clusterId, ns, name);
            throw K8sExceptionMapper.Translate(ex, "加载配置详情");
        }
    }

    public async Task DeleteConfigMapAsync(int clusterId, string name, string ns)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        try
        {
            await client.CoreV1.DeleteNamespacedConfigMapAsync(name, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}", clusterId, ns, name);
            throw K8sExceptionMapper.Translate(ex, "删除配置");
        }
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Delete, $"配置: {ns}/{name} @ 集群 {entity.Name}");
    }

    public async Task UpdateConfigMapFromYamlAsync(int clusterId, string name, string ns, string yaml)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        V1ConfigMap deserialized;
        try
        {
            deserialized = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for update clusterId={ClusterId} ns={Namespace} name={Name}", clusterId, ns, name);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        try
        {
            var existing = await client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);
            existing.Data = deserialized.Data;
            existing.BinaryData = deserialized.BinaryData;
            await client.CoreV1.ReplaceNamespacedConfigMapAsync(existing, name, ns);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReplaceConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}", clusterId, ns, name);
            throw K8sExceptionMapper.Translate(ex, "保存配置");
        }
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Update, $"配置: {ns}/{name} @ 集群 {entity.Name}");
    }

    public async Task CreateConfigMapFromYamlAsync(int clusterId, string yaml)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        V1ConfigMap body;
        try
        {
            body = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for create clusterId={ClusterId}", clusterId);
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
            logger.LogWarning(ex, "CreateConfigMap failed clusterId={ClusterId} ns={Namespace}", clusterId, ns);
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
