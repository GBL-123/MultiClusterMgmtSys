using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class MainPageShellTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Clusters_page_renders_sidebar_filter_and_table()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("shell-cluster"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();

        cut.WaitForState(() => cut.Markup.Contains("shell-cluster"));

        Assert.Contains("集群管理", cut.Markup);
        Assert.Contains("自动同步", cut.Markup);
    }

    [Fact]
    public async Task Cluster_detail_page_shows_toolbar_and_overview_tab()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var added = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("detail-page", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.ClusterDetail>(
            parameters => parameters.Add(p => p.Id, added.Id));

        cut.WaitForState(() => cut.Markup.Contains("detail-page"));

        Assert.Contains("概览", cut.Markup);
        Assert.Contains("离线", cut.Markup);
    }

    [Fact]
    public async Task Cluster_detail_page_missing_shows_not_found()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.ClusterDetail>(
            parameters => parameters.Add(p => p.Id, 999));

        cut.WaitForState(() => cut.Markup.Contains("未找到该集群"));
    }

    [Fact]
    public async Task Nodes_page_without_cluster_shows_hint()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Pages.Nodes>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择") || cut.Markup.Contains("节点管理"));
    }

    [Fact]
    public async Task Configmaps_page_without_cluster_shows_hint()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        ctx.Services.AddScoped<ConfigMapService>();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群") || cut.Markup.Contains("配置管理"));
    }

    [Fact]
    public async Task Accounts_page_lists_seeded_accounts()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Admin", NormalizedName = "ADMIN" });
            var member = new ApplicationUser { UserName = "visible-user", CreatedAt = DateTime.UtcNow };
            await identity.Users.CreateAsync(member, "Passw0rd1");

            var audit = new AuditService(
                new AuditLogRepository(identity.Db),
                TestHttpContext.Anonymous().Object,
                NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("admin", userId: 77, "Admin").Object,
                NullLogger<AccountService>.Instance));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Pages.Accounts>();

            cut.WaitForState(() => cut.Markup.Contains("visible-user"));

            Assert.Contains("账号管理", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Audit_logs_page_renders_with_entries()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        await harness.Audit.LogAsync(AuditCategory.Cluster, AuditAction.Create, "集群: shell-audit");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.AuditLogs.Pages.AuditLogs>();

        cut.WaitForState(() => cut.Markup.Contains("shell-audit"));

        Assert.Contains("审计日志", cut.Markup);
    }
}
