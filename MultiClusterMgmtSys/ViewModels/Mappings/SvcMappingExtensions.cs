using k8s;
using k8s.Models;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

public static class SvcMappingExtensions
{
    public const string EndpointSliceServiceLabel = "kubernetes.io/service-name";

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

    public static List<SvcPortViewModel> ToSvcPortViewModels(IList<V1ServicePort>? ports)
        => ports?.Select(p => new SvcPortViewModel
        {
            Name = p.Name,
            Port = p.Port,
            TargetPort = p.TargetPort?.Value,
            Protocol = p.Protocol ?? "TCP",
            NodePort = p.NodePort
        }).ToList() ?? new();

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
