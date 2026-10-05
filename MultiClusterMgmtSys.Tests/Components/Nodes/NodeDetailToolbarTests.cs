using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Nodes;

public class NodeDetailToolbarTests
{
    private static MultiClusterMgmtSys.Application.ViewModels.ClusterNodeDetailViewModel Detail(bool unschedulable = false)
        => new()
        {
            ClusterId = 1,
            ClusterName = "prod",
            Name = "detail-node",
            Status = "Ready",
            Roles = "control-plane",
            KubeletVersion = "v1.30.2",
            OsImage = "Ubuntu 22.04",
            Unschedulable = unschedulable
        };

    private static List<IRenderedComponent<MudIconButton>> MaintenanceButtons(
        IRenderedComponent<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar> cut)
        => cut.FindComponents<MudIconButton>()
            .Where(b => b.Instance.UserAttributes.TryGetValue("aria-label", out var v) &&
                        v?.ToString() is "封锁" or "解封" or "排空")
            .ToList();

    [Fact]
    public async Task Admin_sees_cordon_and_drain_entries()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.Contains("返回列表", cut.Markup);
        var ar = MaintenanceButtons(cut).Select(b => b.Instance.UserAttributes["aria-label"].ToString()).ToHashSet();
        Assert.Contains("封锁", ar);
        Assert.Contains("排空", ar);
        Assert.DoesNotContain("解封", ar);
    }

    [Fact]
    public async Task Unschedulable_detail_shows_badge_and_uncordon_entry()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar>(
            parameters => parameters.Add(p => p.Node, Detail(unschedulable: true)));

        Assert.Contains("已封锁", cut.Markup);
        Assert.Contains("Unschedulable", cut.Markup);
        var ar = MaintenanceButtons(cut).Select(b => b.Instance.UserAttributes["aria-label"].ToString()).ToHashSet();
        Assert.Contains("解封", ar);
        Assert.Contains("排空", ar);
        Assert.DoesNotContain("封锁", ar);
    }

    [Fact]
    public async Task Member_sees_no_maintenance_entries()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("member");
        auth.SetRoles("Member");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar>(
            parameters => parameters.Add(p => p.Node, Detail()));

        Assert.DoesNotContain("排空", cut.Markup);
        Assert.Empty(MaintenanceButtons(cut));
    }

    [Fact]
    public async Task Maintenance_disabled_disables_all_entries()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar>(
            parameters => parameters
                .Add(p => p.Node, Detail())
                .Add(p => p.MaintenanceDisabled, true));

        Assert.All(MaintenanceButtons(cut), b => Assert.True(b.Instance.Disabled));
    }

    [Fact]
    public async Task Entry_buttons_invoke_callbacks()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cordoned = false;
        var drained = false;
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDetailToolbar>(
            parameters => parameters
                .Add(p => p.Node, Detail())
                .Add(p => p.OnCordonNode, EventCallback.Factory.Create(this, () => cordoned = true))
                .Add(p => p.OnDrainNode, EventCallback.Factory.Create(this, () => drained = true)));

        var cordon = MaintenanceButtons(cut).First(b => b.Instance.UserAttributes["aria-label"].ToString() == "封锁");
        await cut.InvokeAsync(() => cordon.Instance.OnClick.InvokeAsync());
        Assert.True(cordoned);

        var drain = MaintenanceButtons(cut).First(b => b.Instance.UserAttributes["aria-label"].ToString() == "排空");
        await cut.InvokeAsync(() => drain.Instance.OnClick.InvokeAsync());
        Assert.True(drained);
    }
}
