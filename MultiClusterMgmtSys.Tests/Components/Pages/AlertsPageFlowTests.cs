using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

/// <summary>
/// 告警页 bUnit 测试:默认 open 过滤、行渲染与集群链接、resolved 过滤与空态。
/// </summary>
public sealed class AlertsPageFlowTests
{
    private static (BunitContext Ctx, ServiceHarness Harness) CreateAuthorizedHost()
    {
        var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var (harness, _) = ctx.AddAlertStack();
        return (ctx, harness);
    }

    private static async Task<(int ClusterId, int OpenId, int ResolvedId)> SeedAlertsAsync(ServiceHarness harness)
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("alert-target", status: ClusterStatus.Offline));
        var alertRepo = new MultiClusterMgmtSys.Infrastructure.Persistence.AlertRepository(harness.Db);
        var openRecord = new AlertRecord
        {
            ClusterId = cluster.Id,
            RuleKind = AlertRuleKind.ClusterOffline,
            OpenedAt = new DateTime(2026, 10, 8, 8, 0, 0, DateTimeKind.Utc)
        };
        await alertRepo.AddAsync(openRecord);
        var resolvedRecord = new AlertRecord
        {
            ClusterId = cluster.Id,
            RuleKind = AlertRuleKind.NodeNotReady,
            Detail = "未就绪节点 2 个",
            OpenedAt = new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Utc),
            ResolvedAt = new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc)
        };
        await alertRepo.AddAsync(resolvedRecord);
        return (cluster.Id, openRecord.Id, resolvedRecord.Id);
    }

    private static IRenderedComponent<MultiClusterMgmtSys.Web.Components.Alerts.Pages.Alerts> RenderAlerts(BunitContext ctx)
    {
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Alerts.Pages.Alerts>();
        cut.WaitForState(
            () => !cut.Markup.Contains("// 正在加载..."),
            TimeSpan.FromSeconds(10));
        return cut;
    }

    private static async Task FlipFilterAndReloadAsync(
        IRenderedComponent<MultiClusterMgmtSys.Web.Components.Alerts.Pages.Alerts> cut,
        bool? resolved)
    {
        typeof(MultiClusterMgmtSys.Web.Components.Alerts.Pages.Alerts)
            .GetField("_resolvedFilter", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(cut.Instance, resolved);
        var table = (MudTable<AlertListItemViewModel>)typeof(MultiClusterMgmtSys.Web.Components.Alerts.Pages.Alerts)
            .GetField("_table", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance)!;
        await cut.InvokeAsync(() => table.ReloadServerData());
        cut.WaitForState(() => !cut.Markup.Contains("// 正在加载..."), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Default_filter_shows_open_only()
    {
        var (ctx, harness) = CreateAuthorizedHost();
        try
        {
            await SeedAlertsAsync(harness);
            var cut = RenderAlerts(ctx);

            Assert.Contains("集群离线", cut.Markup);
            Assert.Contains("告警中", cut.Markup);
            Assert.DoesNotContain("未就绪节点 2 个", cut.Markup);
            Assert.DoesNotContain("2026-10-07 09:00:00", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Rows_render_cluster_link_and_rule_text()
    {
        var (ctx, harness) = CreateAuthorizedHost();
        try
        {
            var (clusterId, _, _) = await SeedAlertsAsync(harness);
            var cut = RenderAlerts(ctx);

            Assert.Contains("alert-target", cut.Markup);
            Assert.Contains($"/clusters/{clusterId}", cut.Markup);
            Assert.Contains("alert-status-filter", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Resolved_filter_shows_resolved_rows()
    {
        var (ctx, harness) = CreateAuthorizedHost();
        try
        {
            await SeedAlertsAsync(harness);
            var cut = RenderAlerts(ctx);

            await FlipFilterAndReloadAsync(cut, resolved: true);

            Assert.Contains("已解析", cut.Markup);
            Assert.Contains("未就绪节点 2 个", cut.Markup);
            Assert.Contains("2026-10-07 09:00:00", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Empty_state_when_no_open_alerts()
    {
        var (ctx, harness) = CreateAuthorizedHost();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("quiet-cluster"));
            _ = cluster.Id;
            var cut = RenderAlerts(ctx);

            Assert.Contains("[ 暂无告警记录 ]", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}
