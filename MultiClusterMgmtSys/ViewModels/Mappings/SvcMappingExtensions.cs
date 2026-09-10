using k8s;
using k8s.Models;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// V1 Service/EndpointSlice/Endpoints → 服务展示模型的映射(列表/详情/端口/端点)。
/// </summary>
public static class SvcMappingExtensions
{
    /// <summary>EndpointSlice 上标记所属 Service 的标签键。</summary>
    public const string EndpointSliceServiceLabel = "kubernetes.io/service-name";

    /// <summary>将 <see cref="V1Service"/> 映射为服务列表展示数据。</summary>
    public static SvcListViewModel ToSvcListViewModel(this V1Service svc)
    {
        return new SvcListViewModel
        {
            Name = svc.Metadata?.Name ?? "",
            Namespace = svc.Metadata?.NamespaceProperty ?? "",
            Type = svc.Spec?.Type ?? "ClusterIP",
            ClusterIP = svc.Spec?.ClusterIP ?? "",
            Headless = svc.Spec?.ClusterIP == "None",
            Ports = ToSvcPortViewModels(svc.Spec?.Ports),
            ExternalEntry = BuildExternalEntry(svc),
            CreatedAt = svc.Metadata?.CreationTimestamp
        };
    }

    /// <summary>将 <see cref="V1Service"/> 映射为服务详情展示数据(含 YAML)。</summary>
    public static SvcDetailViewModel ToSvcDetailViewModel(this V1Service svc)
    {
        return new SvcDetailViewModel
        {
            Name = svc.Metadata?.Name ?? "",
            Namespace = svc.Metadata?.NamespaceProperty ?? "",
            Uid = svc.Metadata?.Uid ?? "",
            CreatedAt = svc.Metadata?.CreationTimestamp,
            Type = svc.Spec?.Type ?? "ClusterIP",
            ClusterIP = svc.Spec?.ClusterIP ?? "",
            Headless = svc.Spec?.ClusterIP == "None",
            ExternalEntry = BuildExternalEntry(svc),
            ExternalName = svc.Spec?.Type == "ExternalName" ? svc.Spec?.ExternalName : null,
            Selector = svc.Spec?.Selector?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new(),
            Ports = ToSvcPortViewModels(svc.Spec?.Ports),
            Yaml = KubernetesYaml.Serialize(svc)
        };
    }

    /// <summary>端口定义列表 → 端口展示数据列表映射;null 输入返回空列表。</summary>
    public static List<SvcPortViewModel> ToSvcPortViewModels(IList<V1ServicePort>? ports)
        => ports?.Select(p => new SvcPortViewModel
        {
            Name = p.Name,
            Port = p.Port,
            TargetPort = p.TargetPort?.Value,
            Protocol = p.Protocol ?? "TCP",
            NodePort = p.NodePort
        }).ToList() ?? new();

    /// <summary>EndpointSlice 列表 → 端点展示数据列表映射(地址带端口,Ready 取条件)。</summary>
    public static List<SvcEndpointViewModel> ToSvcEndpointViewModels(this V1EndpointSliceList list)
        => list.Items?
            .SelectMany(slice => (slice.Endpoints ?? new List<V1Endpoint>())
                .SelectMany(ep => (ep.Addresses ?? new List<string>())
                    .SelectMany(address => SlicePortStrings(slice)
                        .Select(port => new SvcEndpointViewModel
                        {
                            Address = port is null ? address : $"{address}:{port}",
                            Ready = ep.Conditions?.Ready ?? true
                        }))))
            .ToList() ?? new();

    /// <summary>旧版 Endpoints → 端点展示数据列表映射(ready 与 notReady 分开标记)。</summary>
    public static List<SvcEndpointViewModel> ToSvcEndpointViewModels(this V1Endpoints endpoints)
        => (endpoints.Subsets ?? new List<V1EndpointSubset>())
            .SelectMany(subset => (subset.Addresses ?? new List<V1EndpointAddress>())
                .Select(address => new SvcEndpointViewModel { Address = address.Ip ?? "", Ready = true })
                .Concat((subset.NotReadyAddresses ?? new List<V1EndpointAddress>())
                    .Select(address => new SvcEndpointViewModel { Address = address.Ip ?? "", Ready = false })))
            .ToList();

    private static IEnumerable<string?> SlicePortStrings(V1EndpointSlice slice)
    {
        var ports = slice.Ports ?? new List<Discoveryv1EndpointPort>();
        return ports.Count > 0 ? ports.Select(p => p.Port?.ToString()) : [null];
    }

    private static string BuildExternalEntry(V1Service svc)
    {
        var type = svc.Spec?.Type;
        if (type == "LoadBalancer")
        {
            var ingress = svc.Status?.LoadBalancer?.Ingress ?? new List<V1LoadBalancerIngress>();
            var entries = ingress
                .Select(i => !string.IsNullOrEmpty(i.Ip) ? i.Ip : i.Hostname)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
            return entries.Count > 0 ? string.Join(", ", entries) : "待分配";
        }
        if (type == "NodePort")
        {
            var nodePorts = svc.Spec?.Ports?
                .Where(p => p.NodePort.HasValue)
                .Select(p => $"*:{p.NodePort.Value}")
                .ToList() ?? new();
            return nodePorts.Count > 0 ? string.Join(", ", nodePorts) : "—";
        }
        if (type == "ExternalName")
        {
            return svc.Spec?.ExternalName ?? "";
        }
        return "—";
    }
}
