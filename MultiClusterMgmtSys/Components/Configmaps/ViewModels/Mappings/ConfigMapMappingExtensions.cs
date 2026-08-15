using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Features.Configmaps.ViewModels;

namespace MultiClusterMgmtSys.Features.Configmaps.ViewModels.Mappings;

public static class ConfigMapMappingExtensions
{
    public static ConfigMapListViewModel ToConfigMapListViewModel(this V1ConfigMap cm)
    {
        var keys = cm.Data?.Keys.ToList() ?? new List<string>();
        var preview = keys.Count <= 3
            ? string.Join(", ", keys)
            : string.Join(", ", keys.Take(3)) + "...";
        return new ConfigMapListViewModel
        {
            Name = cm.Metadata?.Name ?? "",
            Namespace = cm.Metadata?.NamespaceProperty ?? "",
            DataKeyCount = cm.Data?.Count ?? 0,
            DataKeyPreview = preview,
            CreatedAt = cm.Metadata?.CreationTimestamp
        };
    }

    public static ConfigMapDetailViewModel ToConfigMapDetailViewModel(this V1ConfigMap cm)
    {
        return new ConfigMapDetailViewModel
        {
            Name = cm.Metadata?.Name ?? "",
            Namespace = cm.Metadata?.NamespaceProperty ?? "",
            Uid = cm.Metadata?.Uid ?? "",
            CreatedAt = cm.Metadata?.CreationTimestamp,
            Data = cm.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new(),
            Yaml = KubernetesYaml.Serialize(cm)
        };
    }
}
