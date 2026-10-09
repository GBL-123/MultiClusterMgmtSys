using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Alerts;

/// <summary>
/// 告警铃铛与 AppBar 角色门控 bUnit 测试:open 数驱动角标、点击导航与 Admin/Member 差异。
/// </summary>
public sealed class AlertBellTests
{
    private static (BunitContext Ctx, ServiceHarness Harness) CreateHost(string actor, string[] roles)
    {
        var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized(actor);
        if (roles.Length > 0)
        {
            auth.SetRoles(roles);
        }

        var (harness, _) = ctx.AddAlertStack(actor: actor == "admin" ? "admin" : "member");
        return (ctx, harness);
    }

    private static async Task SeedOpenAlertsAsync(ServiceHarness harness, string clusterName, int count)
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster(clusterName, status: ClusterStatus.Offline));
        var alertRepo = new MultiClusterMgmtSys.Infrastructure.Persistence.AlertRepository(harness.Db);
        for (var i = 0; i < count; i++)
        {
            await alertRepo.AddAsync(new AlertRecord
            {
                ClusterId = cluster.Id,
                RuleKind = (AlertRuleKind)((int)AlertRuleKind.ClusterOffline + (i % 3)),
                OpenedAt = DateTime.UtcNow.AddHours(-i)
            });
        }
    }

    [Fact]
    public async Task Zero_open_alerts_renders_no_badge()
    {
        var (ctx, _) = CreateHost("admin", ["Admin"]);
        try
        {
            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Alerts.Shared.AlertBell>();

            cut.WaitForState(() => BellInnerText(cut) is not null, TimeSpan.FromSeconds(10));
            // 角标容器恒渲染,但 open 数为 0 时内容为空(不显示数字)。
            Assert.Equal("", BellInnerText(cut));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Three_open_alerts_render_badge_with_count()
    {
        var (ctx, harness) = CreateHost("admin", ["Admin"]);
        try
        {
            await SeedOpenAlertsAsync(harness, "bell-cluster", 3);

            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Alerts.Shared.AlertBell>();
            cut.WaitForState(() => BellInnerText(cut) == "3", TimeSpan.FromSeconds(10));

            Assert.Equal("3", BellInnerText(cut));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    private static string? BellInnerText(IRenderedComponent<MultiClusterMgmtSys.Web.Components.Alerts.Shared.AlertBell> cut)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            cut.Markup, @"mud-badge-overlap"">([^<]*)</span>");
        return match.Success ? match.Groups[1].Value : null;
    }

    [Fact]
    public async Task Click_navigates_to_alerts_page()
    {
        var (ctx, _) = CreateHost("admin", ["Admin"]);
        try
        {
            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Alerts.Shared.AlertBell>();
            cut.WaitForState(() => cut.Markup.Contains("告警中心") || cut.FindAll("button").Count > 0, TimeSpan.FromSeconds(5));

            cut.Find("button").Click();

            var nav = ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            cut.WaitForState(() => nav.Uri.EndsWith("/alerts", StringComparison.OrdinalIgnoreCase), TimeSpan.FromSeconds(5));
            Assert.EndsWith("/alerts", nav.Uri, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task AppBar_shows_bell_for_admin_only()
    {
        var (adminCtx, adminHarness) = CreateHost("admin", ["Admin"]);
        try
        {
            await SeedOpenAlertsAsync(adminHarness, "appbar-cluster", 1);
            var adminBar = adminCtx.Render<MultiClusterMgmtSys.Web.Components.Layout.AppBar>();

            Assert.Contains("告警中心", adminBar.Markup);
        }
        finally
        {
            await adminCtx.DisposeAsync();
        }

        var (memberCtx, _) = CreateHost("member", Array.Empty<string>());
        try
        {
            var memberBar = memberCtx.Render<MultiClusterMgmtSys.Web.Components.Layout.AppBar>();

            Assert.DoesNotContain("告警中心", memberBar.Markup);
        }
        finally
        {
            await memberCtx.DisposeAsync();
        }
    }
}
