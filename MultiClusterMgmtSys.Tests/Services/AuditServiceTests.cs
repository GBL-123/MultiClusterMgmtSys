using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class AuditServiceTests
{
    [Fact]
    public async Task GetRecent_ReturnsOnlyOwnLogs_NewestFirst()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.AuditLogs.AddRange(
            TestData.NewAuditLog("alice", AuditCategory.Authentication, AuditAction.Login, "old", new DateTime(2026, 1, 1)),
            TestData.NewAuditLog("alice", AuditCategory.Cluster, AuditAction.Update, "集群: prod", new DateTime(2026, 1, 3)),
            TestData.NewAuditLog("bob", AuditCategory.Cluster, AuditAction.Delete, "集群: staging", new DateTime(2026, 1, 2)));
        await db.SaveChangesAsync();
        var svc = TestServices.Audit(db);

        var items = await svc.GetRecentAsync("alice", 5);

        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal("alice", i.UserName));
        Assert.Equal("集群: prod", items[0].Target);
        Assert.Equal("old", items[1].Target);
    }

    [Fact]
    public async Task GetRecent_LimitsToCount()
    {
        using var db = SqliteDbFactory.CreateContext();
        for (var i = 0; i < 8; i++)
        {
            db.AuditLogs.Add(TestData.NewAuditLog("alice", AuditCategory.Authentication, AuditAction.Login, $"t{i}", new DateTime(2026, 1, 1).AddDays(i)));
        }
        await db.SaveChangesAsync();
        var svc = TestServices.Audit(db);

        var items = await svc.GetRecentAsync("alice", 5);

        Assert.Equal(5, items.Count);
        Assert.Equal("t7", items[0].Target);
    }

    [Fact]
    public async Task GetRecent_EmptyUser_ReturnsEmpty()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Audit(db);

        var items = await svc.GetRecentAsync("nobody", 5);

        Assert.Empty(items);
    }
}