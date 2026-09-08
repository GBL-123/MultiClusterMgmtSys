using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Clusters;

public class ClusterTableTests
{
    [Fact]
    public async Task Rows_render_status_text_after_data_loads()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var harness = ctx.AddClusterStack();
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod-1", status: ClusterStatus.Online));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("down-1", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterTable>(
            parameters => parameters
                .Add(p => p.Query, new MultiClusterMgmtSys.Requests.ClusterQueryRequest())
                .Add(p => p.OnNavigateClusterDetail, id => Task.CompletedTask)
                .Add(p => p.OnRefreshCluster, id => Task.CompletedTask)
                .Add(p => p.OnEditCluster, id => Task.CompletedTask)
                .Add(p => p.OnDeleteCluster, vm => Task.CompletedTask));

        cut.WaitForState(() => cut.Markup.Contains("prod-1"));

        Assert.Contains("在线", cut.Markup);
        Assert.Contains("离线", cut.Markup);
        Assert.Contains("prod-1", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_when_no_clusters()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        ctx.AddClusterStack();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterTable>(
            parameters => parameters.Add(p => p.Query, new MultiClusterMgmtSys.Requests.ClusterQueryRequest()));

        cut.WaitForState(() => cut.Markup.Contains("empty-state"));

        Assert.Contains("暂无集群", cut.Markup);
    }

    [Fact]
    public async Task Admin_buttons_disappear_when_role_downgraded()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("user");
        auth.SetRoles("Admin");
        var harness = ctx.AddClusterStack();
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("seeded"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterTable>(
            parameters => parameters
                .Add(p => p.Query, new MultiClusterMgmtSys.Requests.ClusterQueryRequest())
                .Add(p => p.OnNavigateClusterDetail, id => Task.CompletedTask));

        cut.WaitForState(() => cut.Markup.Contains("seeded"));
        var iconsAdmin = cut.FindComponents<MudIconButton>().Count(b => b.Instance.Icon != null);

        auth.SetRoles("Member");
        cut.Render();

        var iconsMember = cut.FindComponents<MudIconButton>().Count(b => b.Instance.Icon != null);

        Assert.True(iconsAdmin > iconsMember);
    }

    [Fact]
    public async Task Row_name_click_invokes_navigate_with_cluster_id()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var harness = ctx.AddClusterStack();
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("clickable"));

        int? navigatedId = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterTable>(
            parameters => parameters
                .Add(p => p.Query, new MultiClusterMgmtSys.Requests.ClusterQueryRequest())
                .Add(p => p.OnNavigateClusterDetail, id => { navigatedId = id; return Task.CompletedTask; }));

        cut.WaitForState(() => cut.Markup.Contains("clickable"));

        var nameElement = cut.FindAll(".link-primary")
            .First(e => e.TextContent.Contains("clickable"));
        nameElement.Click();

        Assert.Equal(added.Id, navigatedId);
    }
}
