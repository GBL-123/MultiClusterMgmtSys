using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Tests.ViewModels;

public class SvcListFilterTests
{
    private static SvcListViewModel Item(string name, string type = "ClusterIP", params SvcPortViewModel[] ports)
        => new() { Name = name, Namespace = "app", Type = type, Ports = ports.ToList() };

    private static SvcPortViewModel Port(int port, string? targetPort = null, int? nodePort = null)
        => new() { Port = port, TargetPort = targetPort, Protocol = "TCP", NodePort = nodePort };

    [Fact]
    public void Port_query_matches_service_node_and_target_ports()
    {
        var items = new List<SvcListViewModel>
        {
            Item("svc-a", ports: [Port(80, targetPort: "8080")]),
            Item("svc-b", ports: [Port(80, nodePort: 30080)]),
            Item("svc-c", ports: [Port(443, targetPort: "6443")]),
            Item("svc-d", ports: [Port(53, targetPort: "http")])
        };

        var all = items.Apply(null, null, null);
        Assert.Equal(4, all.Count());

        var byTarget = items.Apply(null, "6443", null);
        Assert.Equal(["svc-c"], byTarget.Select(s => s.Name));

        var byNodePort = items.Apply(null, "30080", null);
        Assert.Equal(["svc-b"], byNodePort.Select(s => s.Name));

        var byServicePort = items.Apply(null, "80", null);
        Assert.Equal(["svc-a", "svc-b"], byServicePort.Select(s => s.Name));
    }

    [Fact]
    public void Type_filter_matches_exactly()
    {
        var items = new List<SvcListViewModel>
        {
            Item("svc-a", type: "ClusterIP"),
            Item("svc-b", type: "NodePort"),
            Item("svc-c", type: "ExternalName")
        };

        var nodePortOnly = items.Apply(null, null, "NodePort");
        Assert.Equal(["svc-b"], nodePortOnly.Select(s => s.Name));

        var all = items.Apply(null, null, null);
        Assert.Equal(3, all.Count());
    }

    [Fact]
    public void Name_filter_is_case_insensitive_and_combinable()
    {
        var items = new List<SvcListViewModel>
        {
            Item("nginx-svc", type: "NodePort", Port(80)),
            Item("web-svc", type: "NodePort", Port(80)),
            Item("other", type: "ClusterIP", Port(80))
        };

        var combined = items.Apply("WEB", "80", "NodePort");

        Assert.Equal(["web-svc"], combined.Select(s => s.Name));
    }
}
