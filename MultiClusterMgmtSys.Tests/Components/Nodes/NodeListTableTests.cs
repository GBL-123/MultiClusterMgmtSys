using Bunit;
using MudBlazor;
using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Nodes;

public class NodeListTableTests
{
    private static MultiClusterMgmtSys.Application.ViewModels.ClusterNodeViewModel Node(
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
                new MultiClusterMgmtSys.Application.ViewModels.NodeIpViewModel { Address = "10.0.0.1", Note = note }
            ]
        };

    [Fact]
    public async Task Renders_nodes_with_fields()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListTable>(
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
        Assert.Contains("就绪", cut.Markup);
        Assert.Contains("未就绪", cut.Markup);
        Assert.Contains("control-plane", cut.Markup);
        Assert.Contains("控制平面", cut.Markup);
        Assert.Contains("v1.30.2", cut.Markup);
        Assert.Contains("10.0.0.1", cut.Markup);
        Assert.Contains("status-badge-raw", cut.Markup);
    }

    [Fact]
    public async Task Ip_note_displayed_next_to_address()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters.Add(p => p.Nodes, [Node("n1", "Ready", note: "管理口")]));

        Assert.Contains("10.0.0.1", cut.Markup);
        Assert.Contains("管理口", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_varies_by_filter_active()
    {
        await using var ctx = new BunitHost();

        var idle = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters.Add(p => p.Nodes, Array.Empty<MultiClusterMgmtSys.Application.ViewModels.ClusterNodeViewModel>()));
        var filtered = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters
                .Add(p => p.Nodes, Array.Empty<MultiClusterMgmtSys.Application.ViewModels.ClusterNodeViewModel>())
                .Add(p => p.FilterActive, true));

        Assert.Contains("暂无节点", idle.Markup);
        Assert.Contains("无匹配节点", filtered.Markup);
    }

    [Fact]
    public async Task Row_name_click_navigates_with_name()
    {
        await using var ctx = new BunitHost();

        string? navigated = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListTable>(
            parameters => parameters
                .Add(p => p.Nodes, [Node("click-node", "Ready")])
                .Add(p => p.OnNavigateNode, name => { navigated = name; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("click-node")).Click();

        Assert.Equal("click-node", navigated);
    }
}

public class NodeCardsTests
{
    private static MultiClusterMgmtSys.Application.ViewModels.ClusterNodeDetailViewModel Detail()
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
                new MultiClusterMgmtSys.Application.ViewModels.NodeAddressViewModel { Type = "InternalIP", Address = "10.0.0.1" }
            ],
            Conditions =
            [
                new MultiClusterMgmtSys.Application.ViewModels.NodeConditionViewModel
                {
                    Type = "Ready", Status = "True", Reason = "KubeletReady", Message = "ok"
                }
            ],
            Resources =
            [
                new MultiClusterMgmtSys.Application.ViewModels.NodeResourceViewModel
                {
                    Key = "cpu",
                    Label = "CPU",
                    CapacityRaw = "4",
                    CapacityText = "4 核",
                    AllocatableRaw = "3800m",
                    AllocatableText = "3.8 核",
                    AllocatablePercent = 95
                },
                new MultiClusterMgmtSys.Application.ViewModels.NodeResourceViewModel
                {
                    Key = "memory",
                    Label = "内存",
                    CapacityRaw = "16297496Ki",
                    CapacityText = "15.5 GiB",
                    AllocatableRaw = "15942336Ki",
                    AllocatableText = "15.2 GiB",
                    AllocatablePercent = 97.8
                }
            ],
            Labels = new Dictionary<string, string> { ["env"] = "prod" },
            Annotations = new Dictionary<string, string> { ["a"] = "b" },
            SystemInfo = new MultiClusterMgmtSys.Application.ViewModels.NodeSystemInfoViewModel
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

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeOverviewCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("node-1", cut.Markup);
        Assert.Contains("不可调度", cut.Markup);
        Assert.Contains("10.244.0.0/24", cut.Markup);
        Assert.Contains("Running", cut.Markup);
        Assert.Contains("阶段 (Phase)", cut.Markup);
        Assert.Contains("运行中", cut.Markup);
        Assert.Contains("控制平面", cut.Markup);
    }

    [Fact]
    public async Task Addresses_card_lists_rows_with_notes()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var node = Detail();
        node.Addresses =
        [
            new MultiClusterMgmtSys.Application.ViewModels.NodeAddressViewModel { Type = "InternalIP", Address = "10.0.0.5", Note = "管理口" },
            new MultiClusterMgmtSys.Application.ViewModels.NodeAddressViewModel { Type = "InternalIP", Address = "172.16.8.2" },
            new MultiClusterMgmtSys.Application.ViewModels.NodeAddressViewModel { Type = "ExternalIP", Address = "203.0.113.10" }
        ];

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeAddressesCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("内网 IP", cut.Markup);
        Assert.Contains("外网 IP", cut.Markup);
        Assert.Contains("10.0.0.5", cut.Markup);
        Assert.Contains("管理口", cut.Markup);
        Assert.Contains("172.16.8.2", cut.Markup);
        Assert.Contains("203.0.113.10", cut.Markup);
    }

    [Fact]
    public async Task Addresses_card_shows_empty_state()
    {
        await using var ctx = new BunitHost();
        var node = Detail();
        node.Addresses = new();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeAddressesCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("[ 暂无地址 ]", cut.Markup);
    }

    [Fact]
    public async Task Taints_card_lists_rows()
    {
        await using var ctx = new BunitHost();
        var node = Detail();
        node.Taints =
        [
            new MultiClusterMgmtSys.Application.ViewModels.NodeTaintViewModel { Key = "dedicated", Value = "gpu", Effect = "NoSchedule" }
        ];

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeTaintsCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("dedicated", cut.Markup);
        Assert.Contains("gpu", cut.Markup);
        Assert.Contains("禁止调度", cut.Markup);
        Assert.Contains("NoSchedule", cut.Markup);
    }

    [Fact]
    public async Task Taints_card_shows_empty_state()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeTaintsCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("[ 暂无污点 ]", cut.Markup);
    }

    [Fact]
    public async Task Resources_card_shows_human_readable_values_and_ratio()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeResourcesCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("CPU", cut.Markup);
        Assert.Contains("4 核", cut.Markup);
        Assert.Contains("3.8 核", cut.Markup);
        Assert.Contains("15.5 GiB", cut.Markup);
        Assert.Contains("95%", cut.Markup);
        var tooltips = cut.FindComponents<MultiClusterMgmtSys.Web.Components.Common.TextTooltip>();
        Assert.Contains(tooltips, t => t.Instance.Text == "3800m" && t.Instance.Mono);
        Assert.Contains(tooltips, t => t.Instance.Text == "16297496Ki");
    }

    [Fact]
    public async Task Resources_card_shows_dash_when_empty()
    {
        await using var ctx = new BunitHost();
        var node = Detail();
        node.Resources = new();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeResourcesCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("资源容量", cut.Markup);
        Assert.Contains("—", cut.Markup);
    }

    [Fact]
    public async Task Resources_card_shows_dash_for_allocatable_only_resource()
    {
        await using var ctx = new BunitHost();
        var node = Detail();
        node.Resources =
        [
            new MultiClusterMgmtSys.Application.ViewModels.NodeResourceViewModel
            {
                Key = "nvidia.com/gpu",
                Label = "nvidia.com/gpu",
                CapacityText = "—",
                AllocatableRaw = "1",
                AllocatableText = "1"
            }
        ];

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeResourcesCard>(
            parameters => parameters.Add(p => p.Node, node));

        Assert.Contains("nvidia.com/gpu", cut.Markup);
        Assert.Contains("—", cut.Markup);
        Assert.Contains("1", cut.Markup);
    }

    [Fact]
    public async Task Conditions_card_shows_condition_rows()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeConditionsCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("KubeletReady", cut.Markup);
        Assert.Contains("就绪", cut.Markup);
        Assert.Contains("成立", cut.Markup);
    }

    [Fact]
    public async Task System_info_card_labels_are_bilingual()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeSystemInfoCard>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("架构 (Architecture)", cut.Markup);
        Assert.Contains("操作系统镜像 (OsImage)", cut.Markup);
        Assert.Contains("amd64", cut.Markup);
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

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListFilterBar>(
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
    public async Task Query_button_invokes_on_query()
    {
        await using var ctx = new BunitHost();
        var queried = false;

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeListFilterBar>(
            parameters => parameters
                .Add(p => p.Filter, new NodeListFilter())
                .Add(p => p.OnQuery, () => { queried = true; return Task.CompletedTask; }));

        var queryButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(() => queryButton.Instance.OnClick.InvokeAsync());

        Assert.True(queried);
    }
}
