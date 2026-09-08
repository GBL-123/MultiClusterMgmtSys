using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Workloads;

public class WorkloadListTableTests
{
    private static WorkloadListViewModel Item(
        string name, WorkloadRolloutState state, string ns = "app", int desired = 3, int ready = 3)
        => new()
        {
            Name = name,
            Namespace = ns,
            Kind = WorkloadKind.Deployment,
            DesiredCount = desired,
            ReadyCount = ready,
            RolloutState = state
        };

    [Fact]
    public async Task Rows_render_rollout_badges_for_three_states()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListTable>(
            parameters => parameters
                .Add(p => p.Kind, WorkloadKind.Deployment)
                .Add(p => p.Items, new[]
                {
                    Item("web", WorkloadRolloutState.Ready),
                    Item("api", WorkloadRolloutState.Rolling),
                    Item("old", WorkloadRolloutState.NotReady)
                })
                .Add(p => p.OnNavigateDetail, _ => Task.CompletedTask)
                .Add(p => p.OnScale, _ => Task.CompletedTask)
                .Add(p => p.OnRestart, _ => Task.CompletedTask)
                .Add(p => p.OnDelete, _ => Task.CompletedTask));

        Assert.Contains("就绪", cut.Markup);
        Assert.Contains("滚动中", cut.Markup);
        Assert.Contains("未就绪", cut.Markup);
        Assert.Contains("web", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_uses_kind_display_name()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListTable>(
            parameters => parameters
                .Add(p => p.Kind, WorkloadKind.StatefulSet)
                .Add(p => p.Items, Array.Empty<WorkloadListViewModel>()));

        Assert.Contains("暂无有状态应用", cut.Markup);
    }

    [Fact]
    public async Task Scale_button_presence_follows_kind_capabilities()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var deployment = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListTable>(
            parameters => parameters
                .Add(p => p.Kind, WorkloadKind.Deployment)
                .Add(p => p.Items, [Item("web", WorkloadRolloutState.Ready)]));

        var daemonSet = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListTable>(
            parameters => parameters
                .Add(p => p.Kind, WorkloadKind.DaemonSet)
                .Add(p => p.Items, [Item("ds-1", WorkloadRolloutState.Ready)]));

        var deploymentIconTooltips = deployment.FindComponents<MudTooltip>()
            .Count(t => t.Instance.Text is "扩缩容" or "重启");
        var daemonSetIconTooltips = daemonSet.FindComponents<MudTooltip>()
            .Count(t => t.Instance.Text is "扩缩容" or "重启");

        Assert.Equal(2, deploymentIconTooltips);
        Assert.Equal(1, daemonSetIconTooltips);
    }

    [Fact]
    public async Task Row_click_navigates_with_namespace_and_name()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        (string ns, string name)? navigated = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListTable>(
            parameters => parameters
                .Add(p => p.Kind, WorkloadKind.Deployment)
                .Add(p => p.Items, [Item("web", WorkloadRolloutState.Ready)])
                .Add(p => p.OnNavigateDetail, args => { navigated = args; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("web")).Click();

        Assert.Equal(("app", "web"), navigated!.Value);
    }
}

public class WorkloadStatusCardTests
{
    [Fact]
    public async Task Shows_replica_fields_and_conditions()
    {
        await using var ctx = new BunitHost();

        var detail = new WorkloadDetailViewModel
        {
            Name = "web",
            Namespace = "app",
            Uid = "uid-1",
            DesiredCount = 3,
            ReadyCount = 2,
            UpdatedCount = 3,
            Selector = "app=web",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Conditions =
            [
                new WorkloadConditionViewModel { Type = "Available", Status = "True", Reason = "ok", Message = "m" }
            ]
        };

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadStatusCard>(
            parameters => parameters.Add(p => p.Detail, detail));

        Assert.Contains("3", cut.Markup);
        Assert.Contains("app=web", cut.Markup);
        Assert.Contains("uid-1", cut.Markup);
        Assert.Contains("Available", cut.Markup);
    }
}

public class WorkloadListFilterBarTests
{
    [Fact]
    public async Task Buttons_fire_query_and_reset()
    {
        await using var ctx = new BunitHost();
        var fired = new List<string>();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListFilterBar>(
            parameters => parameters
                .Add(p => p.Namespaces, new List<string> { "app", "default" })
                .Add(p => p.OnQuery, () => { fired.Add("query"); return Task.CompletedTask; })
                .Add(p => p.OnReset, () => { fired.Add("reset"); return Task.CompletedTask; }));

        foreach (var label in new[] { "查询", "重置" })
        {
            var button = cut.FindComponents<MudButton>().First(b => b.Markup.Contains(label));
            await cut.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());
        }

        Assert.Equal(["query", "reset"], fired);
    }

    [Fact]
    public async Task Namespace_change_propagates_to_parent()
    {
        await using var ctx = new BunitHost();

        string? selected = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListFilterBar>(
            parameters => parameters
                .Add(p => p.Namespaces, new List<string> { "app" })
                .Add(p => p.SelectedNamespaceChanged, ns => { selected = ns; return Task.CompletedTask; }));

        var nsSelect = cut.FindComponents<MudSelect<string?>>()
            .First(s => s.Instance.Label == "命名空间");
        await cut.InvokeAsync(() => nsSelect.Instance.ValueChanged!.InvokeAsync("app"));

        Assert.Equal("app", selected);
    }

    [Fact]
    public async Task Search_name_change_propagates()
    {
        await using var ctx = new BunitHost();

        string? typed = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Shared.WorkloadListFilterBar>(
            parameters => parameters
                .Add(p => p.SearchNameChanged, v => { typed = v; return Task.CompletedTask; }));

        var field = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "名称");
        await cut.InvokeAsync(() => field.Instance.ValueChanged!.InvokeAsync("web"));

        Assert.Equal("web", typed);
    }
}
