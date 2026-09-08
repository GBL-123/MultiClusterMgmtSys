using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Clusters;

public class ClusterOverviewCardTests
{
    private static MultiClusterMgmtSys.ViewModels.ClusterDetailViewModel Detail() => new()
    {
        Id = 5,
        Name = "prod-5",
        Status = ClusterStatus.Online,
        StatusText = "在线",
        Version = "1.30.0",
        NodeCount = 3,
        GroupName = "prod",
        ApiServer = "https://api:6443",
        ConnectionType = ConnectionType.Token,
        Nodes = new(),
        IsReachable = true
    };

    [Fact]
    public async Task Renders_cluster_fields_with_fallbacks()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        ctx.AddClusterStack();

        var detail = Detail();
        detail.Version = null;
        detail.GroupName = null;
        detail.ApiServer = null;

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterOverviewCard>(
            parameters => parameters.Add(p => p.Cluster, detail));

        Assert.Contains("prod-5", cut.Markup);
        Assert.Contains("未分组", cut.Markup);
        Assert.Contains("Token", cut.Markup);
    }

    [Fact]
    public async Task Admin_can_reveal_secret_via_service()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var harness = ctx.AddClusterStack();
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("secret-cluster"));
        var detail = Detail();
        detail.Id = added.Id;

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterOverviewCard>(
            parameters => parameters.Add(p => p.Cluster, detail));

        var revealButton = cut.FindComponents<MudButton>()
            .First(b => b.Markup.Contains("显示密文"));
        await cut.InvokeAsync(() => revealButton.Instance.OnClick.InvokeAsync());

        cut.WaitForState(() => cut.Markup.Contains("隐藏密文"));
        Assert.Contains("Token", cut.Markup);
        Assert.Contains("token-secret-cluster", cut.Markup);
    }

    [Fact]
    public async Task Member_has_no_secret_button()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("member");
        auth.SetRoles("Member");
        ctx.AddClusterStack();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterOverviewCard>(
            parameters => parameters.Add(p => p.Cluster, Detail()));

        Assert.DoesNotContain("显示密文", cut.Markup);
    }
}

public class ClusterDetailToolbarTests
{
    [Fact]
    public async Task Shows_name_and_status_badge()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterDetailToolbar>(
            parameters => parameters
                .Add(p => p.Cluster, new MultiClusterMgmtSys.ViewModels.ClusterDetailViewModel
                {
                    Id = 1,
                    Name = "prod-9",
                    Status = ClusterStatus.Offline,
                    StatusText = "离线"
                }));

        Assert.Contains("prod-5".Replace("5", ""), cut.Markup);
        Assert.Contains("prod-", cut.Markup);
        Assert.Contains("离线", cut.Markup);
    }

    [Fact]
    public async Task Processing_disables_action_buttons()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterDetailToolbar>(
            parameters => parameters
                .Add(p => p.Cluster, new MultiClusterMgmtSys.ViewModels.ClusterDetailViewModel
                {
                    Id = 1,
                    Name = "busy",
                    Status = ClusterStatus.Online,
                    StatusText = "在线"
                })
                .Add(p => p.Processing, true));

        var refresh = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("刷新状态"));
        var edit = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("编辑"));
        var delete = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));

        Assert.True(edit.Instance.Disabled);
        Assert.True(delete.Instance.Disabled);
        Assert.True(delete.Instance.Disabled);
    }

    [Fact]
    public async Task Buttons_fire_callbacks()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var fired = new List<string>();
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterDetailToolbar>(
            parameters => parameters
                .Add(p => p.Cluster, new MultiClusterMgmtSys.ViewModels.ClusterDetailViewModel
                {
                    Id = 1,
                    Name = "prod",
                    Status = ClusterStatus.Online,
                    StatusText = "在线"
                })
                .Add(p => p.OnBack, () => { fired.Add("back"); return Task.CompletedTask; })
                .Add(p => p.OnRefresh, () => { fired.Add("refresh"); return Task.CompletedTask; })
                .Add(p => p.OnEdit, () => { fired.Add("edit"); return Task.CompletedTask; })
                .Add(p => p.OnDelete, () => { fired.Add("delete"); return Task.CompletedTask; }));

        foreach (var label in new[] { "返回列表", "刷新状态", "编辑", "删除" })
        {
            var button = cut.FindComponents<MudButton>().First(b => b.Markup.Contains(label));
            await cut.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());
        }

        Assert.Equal(["back", "refresh", "edit", "delete"], fired);
    }
}

public class GroupSidebarTests
{
    [Fact]
    public async Task Lists_all_and_ungrouped_with_group_counts()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.GroupSidebar>(
            parameters => parameters
                .Add(p => p.Groups, new[]
                {
                    new MultiClusterMgmtSys.ViewModels.ClusterGroupViewModel { Id = 1, Name = "prod", ClusterCount = 3 },
                    new MultiClusterMgmtSys.ViewModels.ClusterGroupViewModel { Id = 2, Name = "dev", ClusterCount = 1 }
                })
                .Add(p => p.UngroupedCount, 4));

        Assert.Contains("全部集群", cut.Markup);
        Assert.Contains("未分组", cut.Markup);
        Assert.Contains("prod", cut.Markup);
        Assert.Contains("dev", cut.Markup);
        Assert.Contains("4", cut.Markup);
    }

    [Fact]
    public async Task Group_click_invokes_selection()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        int? selected = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.GroupSidebar>(
            parameters => parameters
                .Add(p => p.Groups, new[]
                {
                    new MultiClusterMgmtSys.ViewModels.ClusterGroupViewModel { Id = 7, Name = "prod" }
                })
                .Add(p => p.OnGroupSelected, id => { selected = id; return Task.CompletedTask; }));

        var item = cut.FindAll(".mud-list-item")
            .First(e => e.TextContent.Contains("prod"));
        item.Click();

        Assert.Equal(7, selected!.Value);
    }
}
