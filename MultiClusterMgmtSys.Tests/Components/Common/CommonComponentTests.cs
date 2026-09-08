using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Common;

public class ConfirmDialogTests
{
    [Fact]
    public async Task Confirm_returns_ok_with_true()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Common.ConfirmDialog>(
                "确认",
                new DialogParameters
                {
                    { "Message", "确认删除集群 prod？" },
                    { "ConfirmText", "删除" }
                });

        provider.WaitForState(() => provider.Markup.Contains("删除"));

        var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        await provider.InvokeAsync(async () => await confirm.Instance.OnClick.InvokeAsync());

        var dialogResult = await dialogReference.Result;
        Assert.False(dialogResult.Canceled);
        Assert.True((bool)dialogResult.Data!);
    }

    [Fact]
    public async Task Cancel_marks_result_canceled()
    {
        await using var ctx = new BunitHost();
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Common.ConfirmDialog>(
                "确认",
                new DialogParameters { { "Message", "确认？" } });

        provider.WaitForState(() => provider.Markup.Contains("取消"));

        var cancel = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("取消"));
        await provider.InvokeAsync(async () => await cancel.Instance.OnClick.InvokeAsync());

        var dialogResult = await dialogReference.Result;
        Assert.True(dialogResult.Canceled);
    }
}

public class ClusterSelectionStateTests
{
    [Fact]
    public void Set_and_clear_update_state()
    {
        var state = new MultiClusterMgmtSys.Components.Common.ClusterSelectionState();

        state.Set(42);
        Assert.Equal(42, state.SelectedClusterId);

        state.Clear();
        Assert.Null(state.SelectedClusterId);
    }
}

public class ClusterSelectSidebarTests
{
    [Fact]
    public async Task Groups_clusters_and_expands_default()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Common.ClusterSelectSidebar>(
            parameters => parameters
                .Add(p => p.Clusters, new[]
                {
                    new ClusterViewModel { Id = 1, Name = "b", GroupName = "prod", Status = ClusterStatus.Online, StatusText = "在线" },
                    new ClusterViewModel { Id = 2, Name = "a", GroupName = "prod", Status = ClusterStatus.Offline, StatusText = "离线" },
                    new ClusterViewModel { Id = 3, Name = "loose", Status = ClusterStatus.Unknown, StatusText = "未知" }
                }));

        Assert.Contains("prod", cut.Markup);
        Assert.Contains("loose", cut.Markup);
        Assert.Contains("未分组", cut.Markup);
        Assert.Contains("b", cut.Markup);
    }

    [Fact]
    public async Task Cluster_click_invokes_selection()
    {
        await using var ctx = new BunitHost();

        int? selected = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Common.ClusterSelectSidebar>(
            parameters => parameters
                .Add(p => p.Clusters, new[]
                {
                    new ClusterViewModel { Id = 9, Name = "target", Status = ClusterStatus.Online, StatusText = "在线" }
                })
                .Add(p => p.OnClusterSelected, id => { selected = id; return Task.CompletedTask; }));

        var leaf = cut.FindAll(".mud-list-item")
            .First(e => e.TextContent.Contains("target"));
        leaf.Click();

        Assert.Equal(9, selected!.Value);
    }

    [Fact]
    public async Task Empty_clusters_shows_hint()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Common.ClusterSelectSidebar>(
            parameters => parameters.Add(p => p.Clusters, Array.Empty<ClusterViewModel>()));

        Assert.Contains("暂无集群", cut.Markup);
    }
}

