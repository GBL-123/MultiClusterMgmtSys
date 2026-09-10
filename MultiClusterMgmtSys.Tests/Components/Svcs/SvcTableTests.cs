using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Svcs;

public class SvcListTableTests
{
    private static SvcListViewModel Item(
        string name,
        string ns = "app",
        string type = "ClusterIP",
        string clusterIP = "10.96.0.10",
        List<SvcPortViewModel>? ports = null,
        string externalEntry = "—")
        => new()
        {
            Name = name,
            Namespace = ns,
            Type = type,
            ClusterIP = clusterIP,
            Ports = ports ?? [],
            ExternalEntry = externalEntry,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private static SvcPortViewModel Port(int port, string? targetPort = null, string protocol = "TCP", int? nodePort = null)
        => new() { Port = port, TargetPort = targetPort, Protocol = protocol, NodePort = nodePort };

    [Fact]
    public async Task Renders_kubectl_style_port_rows()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("ingress-gw", type: "NodePort", ports: [Port(80, "8080", nodePort: 30080)], externalEntry: "*:30080")]));

        Assert.Contains("ingress-gw", cut.Markup);
        Assert.Contains("80:30080/TCP", cut.Markup);
        Assert.Contains("→ 8080", cut.Markup);
        Assert.Contains("NodePort", cut.Markup);
        Assert.Contains("*:30080", cut.Markup);
    }

    [Fact]
    public async Task Ports_over_three_collapse_with_overflow_hint()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var ports = new List<SvcPortViewModel>
        {
            Port(80), Port(443), Port(8080), Port(9090), Port(9100)
        };
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters.Add(p => p.Items, [Item("many", ports: ports)]));

        Assert.Contains("+2", cut.Markup);
        Assert.DoesNotContain("9100/TCP", cut.Markup);
    }

    [Fact]
    public async Task ExternalName_shows_no_ports_and_dns_entry()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("ext-dns", type: "ExternalName", clusterIP: "", externalEntry: "ext.db.io")]));

        Assert.Contains("[ 暂无 ]", cut.Markup);
        Assert.Contains("ext.db.io", cut.Markup);
    }

    [Fact]
    public async Task Headless_service_shows_none_cluster_ip()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("etcd-headless", clusterIP: "None")]));

        Assert.Contains("None", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_shown_when_no_items()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters.Add(p => p.Items, Array.Empty<SvcListViewModel>()));

        Assert.Contains("暂无 Service", cut.Markup);
    }

    [Fact]
    public async Task Admin_buttons_disappear_when_role_downgraded()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("web")]));

        var adminIcons = cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除");

        auth.SetRoles("Member");
        cut.Render();
        var memberIcons = cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除");

        Assert.Equal(2, adminIcons);
        Assert.Equal(0, memberIcons);
    }

    [Fact]
    public async Task Name_click_navigates_to_detail()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        (string ns, string name)? navigated = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Shared.SvcListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("web")])
                .Add(p => p.OnNavigateDetail, args => { navigated = args; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("web")).Click();

        Assert.Equal(("app", "web"), navigated!.Value);
    }
}
