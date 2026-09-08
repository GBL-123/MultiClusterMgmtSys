using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext db = SqliteDbFactory.CreateContext();
    private readonly AuditLogRepository repo;

    public AuditServiceTests()
    {
        repo = new AuditLogRepository(db);
    }

    public void Dispose() => db.Dispose();

    private AuditService BuildService(string? actor = "admin", params string[] roles)
    {
        var accessor = actor is null
            ? TestHttpContext.Anonymous().Object
            : TestHttpContext.For(actor, roles).Object;
        return new AuditService(repo, accessor, NullLogger<AuditService>.Instance);
    }

    private async Task SeedAsync(string userName, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await repo.AddAsync(TestData.NewAudit(userName, createdAt: DateTime.UtcNow.AddMinutes(i)));
        }
    }

    [Fact]
    public async Task LogAsync_uses_http_context_identity_by_default()
    {
        await BuildService().LogAsync(AuditCategory.Cluster, AuditAction.Create, "集群: c1");

        var log = db.AuditLogs.Single();
        Assert.Equal("admin", log.UserName);
        Assert.Equal(AuditCategory.Cluster, log.Category);
        Assert.Equal(AuditAction.Create, log.Action);
    }

    [Fact]
    public async Task LogAsync_explicit_user_overrides_context()
    {
        await BuildService(actor: "someone-else").LogAsync(AuditCategory.Authentication, AuditAction.Login, "账号: bob", userName: "bob");

        Assert.Equal("bob", db.AuditLogs.Single().UserName);
    }

    [Fact]
    public async Task LogAsync_anonymous_context_writes_null_actor()
    {
        await BuildService(actor: null).LogAsync(AuditCategory.Cluster, AuditAction.Create, "集群: c1");

        Assert.Null(db.AuditLogs.Single().UserName);
    }

    [Fact]
    public async Task LogAsync_swallows_persistence_failure()
    {
        var service = BuildService();
        await db.DisposeAsync();

        var ex = await Record.ExceptionAsync(() => service.LogAsync(AuditCategory.Cluster, AuditAction.Create, "集群: c1"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task GetRecentAsync_no_context_returns_empty()
    {
        var service = BuildService(actor: null);

        var items = await service.GetRecentAsync(5);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetRecentAsync_returns_latest_for_current_user()
    {
        await SeedAsync("admin", 2);
        await SeedAsync("member", 1);
        var service = BuildService();

        var items = await service.GetRecentAsync(2);

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal("admin", i.UserName));
    }

    [Fact]
    public async Task GetPagedAsync_admin_sees_all_and_can_search()
    {
        await SeedAsync("admin", 2);
        await SeedAsync("member", 1);
        var service = BuildService("admin", "Admin");

        var result = await service.GetPagedAsync(new AuditLogQueryRequest { Page = 1, PageSize = 10 });

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task GetPagedAsync_member_sees_only_own()
    {
        await SeedAsync("admin", 2);
        await SeedAsync("member", 1);
        var service = BuildService("member");

        var result = await service.GetPagedAsync(new AuditLogQueryRequest { Page = 1, PageSize = 10 });

        Assert.Equal(1, result.Total);
    }
}
