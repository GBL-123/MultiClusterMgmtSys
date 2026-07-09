using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;
using System.Text;

namespace MultiClusterMgmtSys.Services;

public class ConfigMapService(ClusterRepository repo)
{
    private readonly ClusterRepository repo = repo;

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

    public async Task CreateConfigMapAsync(int clusterId, ConfigMapCreateViewModel vm)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var body = new V1ConfigMap
        {
            Metadata = new V1ObjectMeta
            {
                Name = vm.Name,
                NamespaceProperty = vm.Namespace
            },
            Data = vm.DataEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                .ToDictionary(e => e.Key, e => e.Value)
        };
        await client.CoreV1.CreateNamespacedConfigMapAsync(body, vm.Namespace);
    }

    public async Task UpdateConfigMapAsync(int clusterId, ConfigMapUpdateViewModel vm)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        var existing = await client.CoreV1.ReadNamespacedConfigMapAsync(vm.Name, vm.Namespace);
        existing.Data = vm.DataEntries
            .Where(e => !string.IsNullOrWhiteSpace(e.Key))
            .ToDictionary(e => e.Key, e => e.Value);
        await client.CoreV1.ReplaceNamespacedConfigMapAsync(existing, vm.Name, vm.Namespace);
    }

    public async Task DeleteConfigMapAsync(int clusterId, string name, string ns)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new InvalidOperationException($"Cluster {clusterId} not found");
        var config = BuildConfig(entity);
        using var client = new Kubernetes(config);
        await client.CoreV1.DeleteNamespacedConfigMapAsync(name, ns);
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
