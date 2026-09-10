using k8s.Models;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.ViewModels;

public class SvcMappingTests
{
    private static V1Service NewService(
        string name = "web",
        string ns = "default",
        string type = "ClusterIP",
        string? clusterIP = "10.96.0.10",
        IList<V1ServicePort>? ports = null,
        string? externalName = null,
        V1LoadBalancerStatus? loadBalancer = null)
        => new()
        {
            ApiVersion = "v1",
            Kind = "Service",
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Uid = "uid-1",
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Spec = new V1ServiceSpec
            {
                Type = type,
                ClusterIP = clusterIP,
                ExternalName = externalName,
                Ports = ports ?? [Port(80, "8080")],
                Selector = new Dictionary<string, string> { ["app"] = "web" }
            },
            Status = new V1ServiceStatus { LoadBalancer = loadBalancer }
        };

    private static V1ServicePort Port(int port, string? targetPort = null, string protocol = "TCP", int? nodePort = null, string? name = null)
    {
        var p = new V1ServicePort { Port = port, Protocol = protocol, NodePort = nodePort, Name = name };
        if (targetPort is not null) p.TargetPort = (IntOrString)targetPort;
        return p;
    }

    [Fact]
    public void ClusterIP_service_maps_basics_and_dash_entry()
    {
        var vm = NewService().ToSvcListViewModel();

        Assert.Equal("web", vm.Name);
        Assert.Equal("default", vm.Namespace);
        Assert.Equal("ClusterIP", vm.Type);
        Assert.Equal("10.96.0.10", vm.ClusterIP);
        Assert.False(vm.Headless);
        Assert.Equal("—", vm.ExternalEntry);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), vm.CreatedAt);
    }

    [Fact]
    public void NodePort_service_lists_node_ports_as_entry()
    {
        var svc = NewService(type: "NodePort", ports: [Port(80, "8080", nodePort: 30080), Port(443, "8443", nodePort: 30443)]);

        var vm = svc.ToSvcListViewModel();

        Assert.Equal("*:30080, *:30443", vm.ExternalEntry);
        Assert.Equal(2, vm.Ports.Count);
        Assert.Equal(30080, vm.Ports[0].NodePort);
        Assert.Equal("8080", vm.Ports[0].TargetPort);
    }

    [Fact]
    public void TargetPort_supports_named_and_numeric_forms()
    {
        var svc = NewService(ports: [Port(80, "http"), Port(53, "53", protocol: "UDP")]);

        var vm = svc.ToSvcListViewModel();

        Assert.Equal("http", vm.Ports[0].TargetPort);
        Assert.Equal("53", vm.Ports[1].TargetPort);
        Assert.Equal("UDP", vm.Ports[1].Protocol);
        Assert.Equal("TCP", vm.Ports[0].Protocol);
    }

    [Fact]
    public void Headless_service_marks_headless()
    {
        var vm = NewService(clusterIP: "None").ToSvcListViewModel();

        Assert.True(vm.Headless);
        Assert.Equal("None", vm.ClusterIP);
    }

    [Fact]
    public void ExternalName_service_exposes_dns_as_entry()
    {
        var vm = NewService(type: "ExternalName", clusterIP: "", externalName: "ext.db.io").ToSvcListViewModel();

        Assert.Equal("ExternalName", vm.Type);
        Assert.Equal("ext.db.io", vm.ExternalEntry);
    }

    [Fact]
    public void LoadBalancer_service_maps_ingress_ip_and_pending()
    {
        var assigned = NewService(type: "LoadBalancer", loadBalancer: new V1LoadBalancerStatus
        {
            Ingress = [new V1LoadBalancerIngress { Ip = "203.0.113.7" }]
        }).ToSvcListViewModel();
        Assert.Equal("203.0.113.7", assigned.ExternalEntry);

        var pending = NewService(type: "LoadBalancer").ToSvcListViewModel();
        Assert.Equal("待分配", pending.ExternalEntry);

        var byHostname = NewService(type: "LoadBalancer", loadBalancer: new V1LoadBalancerStatus
        {
            Ingress = [new V1LoadBalancerIngress { Hostname = "lb.example.com" }]
        }).ToSvcListViewModel();
        Assert.Equal("lb.example.com", byHostname.ExternalEntry);
    }

    [Fact]
    public void Detail_view_maps_selector_and_yaml()
    {
        var detail = NewService().ToSvcDetailViewModel();

        Assert.Equal("uid-1", detail.Uid);
        Assert.Equal("web", detail.Selector["app"]);
        Assert.Contains("kind: Service", detail.Yaml);
        Assert.Single(detail.Ports);
    }

    [Fact]
    public void Detail_view_external_name_only_for_externalname_type()
    {
        var external = NewService(type: "ExternalName", clusterIP: "", externalName: "ext.db.io").ToSvcDetailViewModel();
        Assert.Equal("ext.db.io", detailName(external));

        var clusterIp = NewService().ToSvcDetailViewModel();
        Assert.Null(detailName(clusterIp));

        static string? detailName(SvcDetailViewModel d) => d.ExternalName;
    }

    [Fact]
    public void Endpoint_slice_list_flattens_with_ready_flag()
    {
        var list = new V1EndpointSliceList
        {
            Items =
            [
                new V1EndpointSlice
                {
                    Endpoints =
                    [
                        new V1Endpoint
                        {
                            Addresses = ["10.244.1.5"],
                            Conditions = new V1EndpointConditions { Ready = true }
                        },
                        new V1Endpoint
                        {
                            Addresses = ["10.244.2.3"],
                            Conditions = new V1EndpointConditions { Ready = false }
                        }
                    ],
                    Ports = [new Discoveryv1EndpointPort { Port = 8080 }]
                }
            ]
        };

        var rows = list.ToSvcEndpointViewModels();

        Assert.Equal(2, rows.Count);
        Assert.Equal("10.244.1.5:8080", rows[0].Address);
        Assert.True(rows[0].Ready);
        Assert.Equal("10.244.2.3:8080", rows[1].Address);
        Assert.False(rows[1].Ready);
    }

    [Fact]
    public void Endpoint_slice_without_ports_maps_bare_address()
    {
        var list = new V1EndpointSliceList
        {
            Items =
            [
                new V1EndpointSlice
                {
                    Endpoints = [new V1Endpoint { Addresses = ["10.244.1.5"] }],
                    Ports = []
                }
            ]
        };

        var rows = list.ToSvcEndpointViewModels();

        Assert.Single(rows);
        Assert.Equal("10.244.1.5", rows[0].Address);
        Assert.True(rows[0].Ready);
    }

    [Fact]
    public void Legacy_endpoints_maps_ready_and_not_ready_addresses()
    {
        var endpoints = new V1Endpoints
        {
            Subsets =
            [
                new V1EndpointSubset
                {
                    Addresses = [new V1EndpointAddress { Ip = "10.244.1.5" }],
                    NotReadyAddresses = [new V1EndpointAddress { Ip = "10.244.2.3" }]
                }
            ]
        };

        var rows = endpoints.ToSvcEndpointViewModels();

        Assert.Equal(2, rows.Count);
        Assert.Equal("10.244.1.5", rows[0].Address);
        Assert.True(rows[0].Ready);
        Assert.Equal("10.244.2.3", rows[1].Address);
        Assert.False(rows[1].Ready);
    }
}
