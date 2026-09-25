using Bunit;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using MultiClusterMgmtSys.Web.Components.Dashboard.Pages;
using MultiClusterMgmtSys.Web.Components.Layout;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class DashboardPageTests
{
    private static void AuthorizeAdmin(BunitContext ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Dashboard_shows_empty_state_without_any_cluster()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        ctx.AddDashboardStack();

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".empty-state").Count > 0, TimeSpan.FromSeconds(5));

        Assert.Contains("暂无集群", cut.Find(".empty-state").TextContent);
        Assert.Empty(cut.FindAll(".dashboard-stat-bar"));
        Assert.Empty(cut.FindAll(".dashboard-freshness"));
        Assert.Empty(cut.FindAll(".dashboard-stale-banner"));
    }

    [Fact]
    public async Task Dashboard_renders_stat_bar_attention_group_version_and_activity_blocks()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _) = ctx.AddDashboardStack();
        var group = harness.Db.ClusterGroups.Add(TestData.NewGroup("生产环境")).Entity;
        await harness.Db.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("prod-ok", groupId: group.Id, status: ClusterStatus.Online, version: "v1.31.2"));
        await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("prod-down", groupId: group.Id, status: ClusterStatus.Offline, version: "v1.30.6"));
        await harness.Db.AuditLogs.AddAsync(TestData.NewAudit(userName: "admin", target: "集群 prod-ok"));
        await harness.Db.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-stat-bar").Count > 0, TimeSpan.FromSeconds(5));

        var statValues = cut.FindAll(".dashboard-stat-value").Select(v => v.TextContent.Trim()).ToList();
        Assert.Equal(["2", "1", "1", "0", "0"], statValues);

        var attention = cut.FindAll(".dashboard-attention-row");
        Assert.Single(attention);
        Assert.Contains("prod-down", attention[0].TextContent);

        Assert.Equal(3, cut.FindAll(".dashboard-breakdown-row").Count);
        Assert.Contains("1 / 2 在线", cut.Markup);
        Assert.Contains("v1.31.2", cut.Markup);
        Assert.Contains("v1.30.6", cut.Markup);

        Assert.Single(cut.FindAll(".dashboard-activity-row"));
        Assert.Contains("集群 prod-ok", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_shows_empty_attention_state_when_all_clusters_are_online()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _) = ctx.AddDashboardStack();
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("healthy", status: ClusterStatus.Online));

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-stat-bar").Count > 0, TimeSpan.FromSeconds(5));

        Assert.Empty(cut.FindAll(".dashboard-attention-row"));
        Assert.Contains("暂无需要关注的集群", cut.Markup);
    }

    [Fact]
    public async Task Dashboard_shows_stale_banner_when_last_sync_is_older_than_two_intervals()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _) = ctx.AddDashboardStack();
        var cluster = TestData.NewCluster("stale", status: ClusterStatus.Online);
        cluster.LastCheckedAt = DateTime.UtcNow.AddMinutes(-47);
        await harness.ClusterRepo.AddAsync(cluster);

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-stale-banner").Count > 0, TimeSpan.FromSeconds(5));

        Assert.Contains("后台同步可能已停止", cut.Find(".dashboard-stale-banner").TextContent);
        Assert.Contains("每 5 分钟自动同步", cut.Find(".dashboard-freshness").TextContent);
    }

    [Fact]
    public async Task Dashboard_shows_never_synced_hint_without_stale_banner()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _) = ctx.AddDashboardStack();
        var cluster = TestData.NewCluster("never", status: ClusterStatus.Unknown);
        cluster.LastCheckedAt = null;
        await harness.ClusterRepo.AddAsync(cluster);

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-freshness").Count > 0, TimeSpan.FromSeconds(5));

        Assert.Contains("尚未同步", cut.Find(".dashboard-freshness").TextContent);
        Assert.Empty(cut.FindAll(".dashboard-stale-banner"));
    }

    [Fact]
    public async Task Dashboard_refresh_all_shows_progress_and_disables_repeat_trigger()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddDashboardStack();
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("a", status: ClusterStatus.Online));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("b", status: ClusterStatus.Online));

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        k8s.Setup(x => x.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyDictionary<string, IReadOnlyList<string>> _, CancellationToken _) =>
            {
                await gate.Task;
                throw new InvalidOperationException("probe blocked");
            });

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-stat-bar").Count > 0, TimeSpan.FromSeconds(5));

        cut.Find(".dashboard-refresh-button").Click();

        var disabled = await WaitForAsync(cut, () => cut.Find(".dashboard-refresh-button").HasAttribute("disabled"));
        Assert.True(disabled, "刷新进行中「刷新全部」入口应处于禁用状态");

        // bUnit 只测接线契约:进度文本以「已完成 / 总数」形态渲染即可。
        // 具体数值由 ClusterService 的服务层测试覆盖 —— 全量刷新的互斥锁是静态信号量,
        // 并行执行时本轮可能仍在等待锁,进度数值不可控。
        var progressText = cut.Find(".dashboard-refresh-progress").TextContent.Trim();
        Assert.Matches(@"^\d+ / \d+$", progressText);

        gate.SetResult();

        var reenabled = await WaitForAsync(cut, () => !cut.Find(".dashboard-refresh-button").HasAttribute("disabled"));
        Assert.True(reenabled, "刷新完成后「刷新全部」入口应恢复可用");
    }

    [Fact]
    public async Task Dashboard_page_root_stretches_to_fill_the_layout()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _) = ctx.AddDashboardStack();
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("healthy", status: ClusterStatus.Online));

        var cut = ctx.Render<Dashboard>();
        cut.WaitForState(() => cut.FindAll(".dashboard-stat-bar").Count > 0, TimeSpan.FromSeconds(5));

        // MainLayout 的 MudContainer 是 d-flex(row):页面根节点不带 flex-auto 会收缩到内容宽度
        var root = cut.Find(".dashboard-page");
        Assert.Contains("flex-auto", root.ClassList);
    }

    [Fact]
    public async Task Drawer_lists_dashboard_entry_before_cluster_management()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);

        var cut = ctx.Render<Drawer>();

        var links = cut.FindComponents<MudNavLink>();
        Assert.True(links.Count >= 2, $"侧边导航至少应有看板与集群管理两项,实际 {links.Count} 项");
        Assert.Equal("/dashboard", links[0].Instance.Href);
        Assert.Equal("/clusters", links[1].Instance.Href);
    }

    private static async Task<bool> WaitForAsync(IRenderedComponent<Dashboard> cut, Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await cut.InvokeAsync(() => { });
            if (condition())
            {
                return true;
            }

            await Task.Delay(20);
        }

        return condition();
    }
}
