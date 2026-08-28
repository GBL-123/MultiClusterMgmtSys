using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;

namespace MultiClusterMgmtSys.Services;

public class ConfigMapService(ClusterRepository repo, AuditService auditService)
{
    private readonly ClusterRepository repo = repo;
    private readonly AuditService auditService = auditService;

    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var nsList = await client.CoreV1.ListNamespaceAsync();
        return nsList.Items.Select(n => n.Metadata?.Name ?? "").OrderBy(n => n).ToList();
    }

    public async Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(int clusterId, string? ns)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var list = ns is null
            ? await client.CoreV1.ListConfigMapForAllNamespacesAsync()
            : await client.CoreV1.ListNamespacedConfigMapAsync(ns);
        return list.Items.Select(cm => cm.ToConfigMapListViewModel()).ToList();
    }

    public async Task<ConfigMapDetailViewModel?> GetConfigMapAsync(int clusterId, string name, string ns)
    {
        var entity = await repo.GetByIdAsync(clusterId);
        if (entity is null) return null;
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var cm = await client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);
        return cm.ToConfigMapDetailViewModel();
    }

    public async Task DeleteConfigMapAsync(int clusterId, string name, string ns)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        await client.CoreV1.DeleteNamespacedConfigMapAsync(name, ns);
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Delete, $"配置: {ns}/{name} @ 集群 {entity.Name}");
    }

    public async Task UpdateConfigMapFromYamlAsync(int clusterId, string name, string ns, string yaml)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var deserialized = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
        var existing = await client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);
        existing.Data = deserialized.Data;
        existing.BinaryData = deserialized.BinaryData;
        await client.CoreV1.ReplaceNamespacedConfigMapAsync(existing, name, ns);
        await auditService.LogAsync(AuditCategory.Configmap, AuditAction.Update, $"配置: {ns}/{name} @ 集群 {entity.Name}");
    }

    public async Task CreateConfigMapFromYamlAsync(int clusterId, string yaml)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var body = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
        var ns = body.Metadata?.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(ns))
            throw new InvalidOperationException("YAML metadata.namespace 未指定");
        await client.CoreV1.CreateNamespacedConfigMapAsync(body, ns);
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
