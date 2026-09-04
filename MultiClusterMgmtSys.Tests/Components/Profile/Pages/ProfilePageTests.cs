using Bunit;
using Bunit.TestDoubles;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;
using ProfilePage = MultiClusterMgmtSys.Components.Profile.Pages.Profile;

namespace MultiClusterMgmtSys.Tests.Components.Profile.Pages;

/// <summary>
/// 接线契约:个人资料页的最近操作卡在无记录时渲染空态。
/// </summary>
public class ProfilePageTests
{
    [Fact]
    public void RecentOps_Empty_ShowsEmptyState()
    {
        using var db = SqliteDbFactory.CreateContext();
        var ctx = BunitHost.Create(db);
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles(["Member"]);

        var page = ctx.RenderComponent<ProfilePage>();

        page.WaitForState(() => page.FindAll(".empty-state").Count > 0);

        var empty = page.FindAll(".empty-state").First();
        Assert.Contains("暂无操作记录", empty.TextContent);
    }
}