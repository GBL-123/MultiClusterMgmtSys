using Bunit;
using Bunit.TestDoubles;
using MultiClusterMgmtSys.Components.Clusters.Shared;
using MultiClusterMgmtSys.Components.Clusters.ViewModels;
using MultiClusterMgmtSys.Common.Enums;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Components.Clusters.Shared;

/// <summary>
/// 接线契约:状态徽章按集群状态映射到正确的 CSS 类。
/// </summary>
public class ClusterDetailToolbarTests
{
    [Theory]
    [InlineData(ClusterStatus.Online, "status-badge online")]
    [InlineData(ClusterStatus.Offline, "status-badge offline")]
    [InlineData(ClusterStatus.Unknown, "status-badge unknown")]
    public void StatusBadge_MapsStatusToClass(ClusterStatus status, string expectedClass)
    {
        using var ctx = new TestContext();
        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles(["Admin"]);
        var vm = new ClusterDetailViewModel
        {
            Id = 1,
            Name = "c1",
            Status = status,
            StatusText = status == ClusterStatus.Online ? "在线" : status == ClusterStatus.Offline ? "离线" : "未知"
        };

        var toolbar = ctx.RenderComponent<ClusterDetailToolbar>(p => p.Add(x => x.Cluster, vm));

        var badge = toolbar.Find(".status-badge");
        Assert.Contains("status-badge", badge.ClassList);
        Assert.Contains(expectedClass.Split(' ')[1], badge.ClassList);
        Assert.NotNull(badge.QuerySelector(".status-dot"));
    }
}