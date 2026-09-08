using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Data;

public class AuditLogRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext db = SqliteDbFactory.CreateContext();
    private readonly AuditLogRepository repo;

    public AuditLogRepositoryTests()
    {
        repo = new AuditLogRepository(db);
    }

    public void Dispose() => db.Dispose();

    private async Task SeedLogsAsync()
    {
        var logs = new[]
        {
            TestData.NewAudit("admin", AuditCategory.Cluster, AuditAction.Create, "c1",
                createdAt: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)),
            TestData.NewAudit("admin", AuditCategory.Cluster, AuditAction.Delete, "c2",
                createdAt: new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc)),
            TestData.NewAudit("member", AuditCategory.Account, AuditAction.Update, "u1",
                createdAt: new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc))
        };

        await repo.AddAsync(logs[0]);
        await repo.AddAsync(logs[1]);
        await repo.AddAsync(logs[2]);
    }

    [Fact]
    public async Task GetRecentForUserAsync_returns_newest_first_limited()
    {
        await SeedLogsAsync();

        var recent = await repo.GetRecentForUserAsync("admin", 1);

        Assert.Single(recent);
        Assert.Equal(AuditAction.Delete, recent[0].Action);
    }

    [Fact]
    public async Task GetPagedAsync_admin_sees_all_and_can_search()
    {
        await SeedLogsAsync();

        var (items, total) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 1, PageSize = 10 }, "admin", isAdmin: true);

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_admin_search_filters_by_user()
    {
        await SeedLogsAsync();

        var (items, total) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 1, PageSize = 10, SearchName = "mem" }, "admin", isAdmin: true);

        Assert.Equal(1, total);
        Assert.Equal("member", items.Single().UserName);
    }

    [Fact]
    public async Task GetPagedAsync_non_admin_sees_only_own_logs()
    {
        await SeedLogsAsync();

        var (items, total) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 1, PageSize = 10 }, "member", isAdmin: false);

        Assert.Equal(1, total);
        Assert.All(items, l => Assert.Equal("member", l.UserName));
    }

    [Fact]
    public async Task GetPagedAsync_category_filter()
    {
        await SeedLogsAsync();

        var (items, total) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 1, PageSize = 10, Category = AuditCategory.Cluster }, "admin", isAdmin: true);

        Assert.Equal(2, total);
        Assert.All(items, l => Assert.Equal(AuditCategory.Cluster, l.Category));
    }

    [Fact]
    public async Task GetPagedAsync_sort_ascending_by_created_at()
    {
        await SeedLogsAsync();

        var (items, _) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 1, PageSize = 10, SortDescending = false }, "admin", isAdmin: true);

        Assert.Equal(AuditAction.Create, items[0].Action);
        Assert.Equal(AuditAction.Update, items[1].Action);
        Assert.Equal(AuditAction.Delete, items[2].Action);
    }

    [Fact]
    public async Task GetPagedAsync_zero_page_clamps_to_first()
    {
        await SeedLogsAsync();

        var (items, total) = await repo.GetPagedAsync(
            new AuditLogQueryRequest { Page = 0, PageSize = 2 }, "admin", isAdmin: true);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);
    }
}
