using k8s;
using k8s.Models;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// V1 ConfigMap → 配置展示模型的映射(列表与详情)。
/// </summary>
public static class ConfigMapMappingExtensions
{
    /// <summary>将 <see cref="V1ConfigMap"/> 映射为 ConfigMap 列表展示数据。</summary>
    public static ConfigMapListViewModel ToConfigMapListViewModel(this V1ConfigMap cm)
    {
        var keys = cm.Data?.Keys.ToList() ?? [];
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

    /// <summary>将 <see cref="V1ConfigMap"/> 映射为 ConfigMap 详情展示数据(含 YAML)。</summary>
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
