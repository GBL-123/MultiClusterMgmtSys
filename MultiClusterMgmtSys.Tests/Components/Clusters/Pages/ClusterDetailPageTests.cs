using Bunit;
using Bunit.TestDoubles;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Clusters.Shared;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;
using ClusterDetailPage = MultiClusterMgmtSys.Components.Clusters.Pages.ClusterDetail;

namespace MultiClusterMgmtSys.Tests.Components.Clusters.Pages;

/// <summary>
/// 接线契约:详情页 tab 化——默认选中第一个 tab、三面板、工具栏在 tab 区之外。
/// </summary>
public class ClusterDetailPageTests
{
    private const string ClusterName = "测试集群";

    private static TestContext CreateContext(string role)
    {
        var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster(ClusterName, ClusterStatus.Online));
        db.SaveChanges();

        var ctx = BunitHost.Create(db);
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles([role]);
        return ctx;
    }

    [Fact]
    public void Tabs_DefaultShowsFirstOfThree()
    {
        using var ctx = CreateContext("Member");

        var page = ctx.RenderComponent<ClusterDetailPage>(p => p.Add(x => x.Id, 1));
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        Assert.Equal(3, tabs.Instance.Panels.Count);
        Assert.Single(page.FindComponents<ClusterOverviewCard>());
        Assert.Empty(page.FindComponents<ClusterEndpointsCard>());
        Assert.Empty(page.FindComponents<ClusterNodesCard>());
        Assert.Contains("集群端点", page.Markup);
        Assert.Contains("节点", page.Markup);
    }

    [Fact]
    public void Toolbar_RendersBeforeTabArea()
    {
        using var ctx = CreateContext("Admin");

        var page = ctx.RenderComponent<ClusterDetailPage>(p => p.Add(x => x.Id, 1));
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var markup = page.Markup;
        Assert.Single(page.FindComponents<ClusterDetailToolbar>());
        Assert.Single(page.FindComponents<MudTabs>());
        Assert.Contains("刷新状态", markup);
        Assert.True(markup.IndexOf("刷新状态", StringComparison.Ordinal) < markup.IndexOf("detail-tabs", StringComparison.Ordinal));
    }
}
