using k8s;
using k8s.Models;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// V1 Namespace → 命名空间展示模型的映射(列表与详情),含状态归一。
/// </summary>
public static class NamespaceMappingExtensions
{
    /// <summary>将 <see cref="V1Namespace"/> 映射为命名空间列表展示数据。</summary>
    public static NamespaceListViewModel ToNamespaceListViewModel(this V1Namespace ns)
    {
        return new NamespaceListViewModel
        {
            Name = ns.Metadata?.Name ?? "",
            Phase = ns.Status?.Phase ?? "",
            LabelCount = ns.Metadata?.Labels?.Count ?? 0,
            CreatedAt = ns.Metadata?.CreationTimestamp
        };
    }

    /// <summary>将 <see cref="V1Namespace"/> 映射为命名空间详情展示数据(含标签、注解与 YAML)。</summary>
    public static NamespaceDetailViewModel ToNamespaceDetailViewModel(this V1Namespace ns)
    {
        return new NamespaceDetailViewModel
        {
            Name = ns.Metadata?.Name ?? "",
            Phase = ns.Status?.Phase ?? "",
            CreatedAt = ns.Metadata?.CreationTimestamp,
            Labels = ns.Metadata?.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new(),
            Annotations = ns.Metadata?.Annotations?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new(),
            Yaml = KubernetesYaml.Serialize(ns)
        };
    }
}
