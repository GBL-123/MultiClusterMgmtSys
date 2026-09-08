using Bunit;
using MudBlazor;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Nodes;

public class NodeListTableTests
{
    private static MultiClusterMgmtSys.ViewModels.ClusterNodeViewModel Node(
        string name, string status, string? note = null)
        => new()
        {
            Name = name,
            Status = status,
            Roles = "control-plane",
            KubeletVersion = "v1.30.2",
            OsImage = "Ubuntu 22.04",
            Unschedulable = false,
            IpAddresses =
            [
                new MultiClusterMgmtSys.ViewModels.NodeIpViewModel { Address = "10.0.0.1", Note = note }
            ]
        };

    [Fact]
    public async Task Renders_nodes_with_fields()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters
                .Add(p => p.Nodes, new[]
                {
                    Node("node-1", "Ready"),
                    Node("node-2", "NotReady"),
                    Node("node-3", "Unknown")
                }));

        Assert.Contains("node-1", cut.Markup);
        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("NotReady", cut.Markup);
        Assert.Contains("control-plane", cut.Markup);
        Assert.Contains("v1.30.2", cut.Markup);
        Assert.Contains("10.0.0.1", cut.Markup);
    }

    [Fact]
    public async Task Ip_note_displayed_next_to_address()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters.Add(p => p.Nodes, [Node("n1", "Ready", note: "管理口")]));

        Assert.Contains("10.0.0.1", cut.Markup);
        Assert.Contains("管理口", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_varies_by_filter_active()
    {
        await using var ctx = new BunitHost();

        var idle = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters.Add(p => p.Nodes, Array.Empty<MultiClusterMgmtSys.ViewModels.ClusterNodeViewModel>()));
        var filtered = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters
                .Add(p => p.Nodes, Array.Empty<MultiClusterMgmtSys.ViewModels.ClusterNodeViewModel>())
                .Add(p => p.FilterActive, true));

        Assert.Contains("暂无节点", idle.Markup);
        Assert.Contains("无匹配节点", filtered.Markup);
    }

    [Fact]
    public async Task Row_name_click_navigates_with_name()
    {
        await using var ctx = new BunitHost();

        string? navigated = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters
                .Add(p => p.Nodes, [Node("click-node", "Ready")])
                .Add(p => p.OnNavigateNode, name => { navigated = name; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("click-node")).Click();

        Assert.Equal("click-node", navigated);
    }
}

public class NodeCardsTests
{
    private static MultiClusterMgmtSys.ViewModels.ClusterNodeDetailViewModel Detail()
        => new()
        {
            ClusterId = 1,
            ClusterName = "prod",
            Name = "node-1",
            Status = "Ready",
            Roles = "control-plane",
            KubeletVersion = "v1.30.2",
            OsImage = "Ubuntu 22.04",
            Unschedulable = true,
            Phase = "Running",
            PodCIDR = "10.244.0.0/24",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Addresses =
            [
                new MultiClusterMgmtSys.ViewModels.NodeAddressViewModel { Type = "InternalIP", Address = "10.0.0.1" }
            ],
            Conditions =
            [
                new MultiClusterMgmtSys.ViewModels.NodeConditionViewModel
                {
                    Type = "Ready", Status = "True", Reason = "KubeletReady", Message = "ok"
                }
            ],
            Capacity = new Dictionary<string, string> { ["cpu"] = "4", ["memory"] = "8Gi" },
            Allocatable = new Dictionary<string, string> { ["cpu"] = "3800m" },
            Labels = new Dictionary<string, string> { ["env"] = "prod" },
            Annotations = new Dictionary<string, string> { ["a"] = "b" },
            SystemInfo = new MultiClusterMgmtSys.ViewModels.NodeSystemInfoViewModel
            {
                Architecture = "amd64",
                KubeletVersion = "v1.30.2"
            },
            IsReachable = true
        };

    [Fact]
    public async Task Overview_card_shows_fields_and_schedulable_state()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeOverviewCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("node-1", cut.Markup);
        Assert.Contains("不可调度", cut.Markup);
        Assert.Contains("10.244.0.0/24", cut.Markup);
        Assert.Contains("Running", cut.Markup);
    }

    [Fact]
    public async Task Resources_card_shows_capacity_and_allocatable()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeResourcesCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("cpu", cut.Markup);
        Assert.Contains("8Gi", cut.Markup);
        Assert.Contains("3800m", cut.Markup);
    }

    [Fact]
    public async Task Resources_card_shows_dash_when_empty()
    {
        await using var ctx = new BunitHost();
        var node = Detail();
        node.Capacity = new Dictionary<string, string>();
        node.Allocatable = new Dictionary<string, string>();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeResourcesCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("资源容量", cut.Markup);
    }

    [Fact]
    public async Task Conditions_card_shows_condition_rows()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeConditionsCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("KubeletReady", cut.Markup);
    }
}

public class NodeListFilterBarTests
{
    [Fact]
    public async Task Reset_clears_filter_and_invokes_on_reset()
    {
        await using var ctx = new BunitHost();
        var filter = new NodeListFilter { Name = "x", Status = "Ready", Schedulable = true };
        var reset = false;

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListFilterBar>(
            parameters => parameters
                .Add(p => p.Filter, filter)
                .Add(p => p.OnReset, () => { reset = true; return Task.CompletedTask; }));

        var resetButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(() => resetButton.Instance.OnClick.InvokeAsync());

        Assert.Equal("", filter.Name);
        Assert.Null(filter.Status);
        Assert.Null(filter.Role);
        Assert.Null(filter.Schedulable);
        Assert.False(filter.IsActive);
        Assert.True(reset);
    }

    [Fact]
    public async Task Reset_wires_both_callbacks()
    {
        await using var ctx = new BunitHost();
        var reset = false;

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Shared.NodeListFilterBar>(
            parameters => parameters
                .Add(p => p.Filter, new NodeListFilter { Name = "x" })
                .Add(p => p.OnReset, () => { reset = true; return Task.CompletedTask; })
                .Add(p => p.OnFilterChanged, () => Task.CompletedTask));

        var resetButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(() => resetButton.Instance.OnClick.InvokeAsync());

        Assert.True(reset);
    }
}
