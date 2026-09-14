using k8s.Models;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// core/v1 Event → 事件展示模型的映射:类型/原因/关联对象、最近发生时间四级回退链,
/// 以及关联对象详情路由的静态白名单(无详情页的 kind 返回 null 由页面按纯文本展示)。
/// </summary>
public static class EventMappingExtensions
{
    /// <summary>将 <see cref="Corev1Event"/> 映射为事件列表展示数据。</summary>
    /// <param name="ev">Kubernetes core/v1 事件对象。</param>
    /// <param name="clusterId">当前集群 Id,用于生成关联对象详情路由。</param>
    public static EventListViewModel ToEventListViewModel(this Corev1Event ev, int clusterId)
    {
        return new EventListViewModel
        {
            Type = ev.Type ?? "",
            Reason = ev.Reason ?? "",
            Namespace = ev.Metadata?.NamespaceProperty ?? "",
            InvolvedKind = ev.InvolvedObject?.Kind ?? "",
            InvolvedNamespace = ev.InvolvedObject?.NamespaceProperty ?? "",
            InvolvedName = ev.InvolvedObject?.Name ?? "",
            InvolvedFieldPath = ev.InvolvedObject?.FieldPath ?? "",
            DetailRoute = BuildDetailRoute(clusterId, ev.InvolvedObject),
            Message = ev.Message ?? "",
            Count = ev.Count ?? 1,
            OccurredAt = ResolveOccurredAt(ev),
            FirstOccurredAt = ResolveFirstOccurredAt(ev),
            SourceComponent = ev.Source?.Component ?? "",
            SourceHost = ev.Source?.Host ?? ""
        };
    }

    private static DateTime? ResolveOccurredAt(Corev1Event ev)
        => ev.LastTimestamp ?? ev.Series?.LastObservedTime ?? ev.EventTime ?? ev.Metadata?.CreationTimestamp;

    private static DateTime? ResolveFirstOccurredAt(Corev1Event ev)
        => ev.FirstTimestamp ?? ev.EventTime ?? ev.Metadata?.CreationTimestamp;

    private static string? BuildDetailRoute(int clusterId, V1ObjectReference? involved)
    {
        var name = involved?.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var ns = involved?.NamespaceProperty ?? "";
        return involved?.Kind switch
        {
            "Node" => $"/nodes/{clusterId}/{name}",
            "Namespace" => $"/namespaces/{clusterId}/{name}",
            "ConfigMap" when ns.Length > 0 => $"/configmaps/{clusterId}/{ns}/{name}",
            "Service" when ns.Length > 0 => $"/services/{clusterId}/{ns}/{name}",
            "Deployment" when ns.Length > 0 => $"/workloads/deployments/{clusterId}/{ns}/{name}",
            "StatefulSet" when ns.Length > 0 => $"/workloads/statefulsets/{clusterId}/{ns}/{name}",
            "DaemonSet" when ns.Length > 0 => $"/workloads/daemonsets/{clusterId}/{ns}/{name}",
            "ReplicaSet" when ns.Length > 0 => $"/workloads/replicasets/{clusterId}/{ns}/{name}",
            _ => null
        };
    }
}
