using Bunit;
using Bunit.TestDoubles;
using MudBlazor;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;
using ClustersPage = MultiClusterMgmtSys.Components.Clusters.Pages.Clusters;

namespace MultiClusterMgmtSys.Tests.Components.Clusters.Pages;

/// <summary>
/// 接线契约:Admin/Member 角色门控——Member 看不到写操作按钮。
/// </summary>
public class ClustersPageTests
{
    private const string ClusterName = "测试集群";

    private static TestContext CreateContext(string role, out MultiClusterMgmtSys.Data.ApplicationDbContext db)
    {
        db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster(ClusterName, status: MultiClusterMgmtSys.Common.Enums.ClusterStatus.Online));
        db.SaveChanges();

        var ctx = BunitHost.Create(db);
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles([role]);
        return ctx;
    }

    private static bool HasButton(TestContext ctx, string text)
        => ctx.RenderComponent<ClustersPage>().FindAll("button").Any(b => b.TextContent.Contains(text));

    [Fact]
    public void Member_DoesNotSeeAdminActions()
    {
        using var ctx = CreateContext("Member", out _);

        var page = ctx.RenderComponent<ClustersPage>();
        page.WaitForState(() => page.FindAll("td").Count > 0);

        Assert.False(HasButton(ctx, "刷新所有集群"));
        Assert.False(HasButton(ctx, "添加集群"));
        Assert.False(HasButton(ctx, "批量操作"));
    }

    [Fact]
    public void Admin_SeesAdminActions()
    {
        using var ctx = CreateContext("Admin", out _);

        var page = ctx.RenderComponent<ClustersPage>();
        page.WaitForState(() => page.FindAll("td").Count > 0);

        Assert.True(HasButton(ctx, "刷新所有集群"));
        Assert.True(HasButton(ctx, "添加集群"));
        Assert.True(HasButton(ctx, "批量操作"));
    }
}