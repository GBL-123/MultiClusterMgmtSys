using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

/// <summary>
/// 集群健康快照仓储测试:窗口区间查询(边界含入)与过期快照删除。
/// </summary>
public sealed class ClusterHealthRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

    private readonly ApplicationDbContext _db;

    private readonly ClusterHealthRepository _repo;

    private readonly int _clusterId;

    public ClusterHealthRepositoryTests()
    {
        _db = SqliteDbFactory.CreateContext();
        _repo = new ClusterHealthRepository(_db);
        var cluster = TestData.NewCluster("snap-cluster");
        _db.Clusters.Add(cluster);
        _db.SaveChanges();
        _clusterId = cluster.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetWindowAsync_returns_rows_within_window_ordered()
    {
        await SeedSnapshotAsync(-30);
        await SeedSnapshotAsync(-2);
        await SeedSnapshotAsync(-1);

        var rows = await _repo.GetWindowAsync(Now.AddHours(-24));

        var times = rows.Select(r => r.CapturedAt).ToArray();
        Assert.Equal([Now.AddHours(-2), Now.AddHours(-1)], times);
    }

    [Fact]
    public async Task GetWindowAsync_boundary_is_inclusive()
    {
        await SeedSnapshotAsync(-25);
        await SeedSnapshotAsync(-24);

        var rows = await _repo.GetWindowAsync(Now.AddDays(-1));

        Assert.Equal(Now.AddDays(-1), Assert.Single(rows).CapturedAt);
    }

    [Fact]
    public async Task DeleteCapturedBeforeAsync_deletes_strictly_older_rows()
    {
        await SeedSnapshotAsync(-91);
        await SeedSnapshotAsync(-89);
        await SeedSnapshotAsync(-10);

        var deleted = await _repo.DeleteCapturedBeforeAsync(Now.AddHours(-90));

        Assert.Equal(1, deleted);
        var remaining = await _db.ClusterHealthSnapshots
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal([Now.AddHours(-89), Now.AddHours(-10)], [.. remaining.Select(r => r.CapturedAt)]);
    }

    [Fact]
    public async Task DeleteCapturedBeforeAsync_without_expired_rows_deletes_nothing()
    {
        await SeedSnapshotAsync(-1);

        var deleted = await _repo.DeleteCapturedBeforeAsync(Now.AddHours(-90));

        Assert.Equal(0, deleted);
        Assert.Equal(1, await _db.ClusterHealthSnapshots.CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task SeedSnapshotAsync(double hoursAgo)
    {
        _db.ClusterHealthSnapshots.Add(new ClusterHealthSnapshot
        {
            ClusterId = _clusterId,
            CapturedAt = Now.AddHours(hoursAgo),
            TotalNodes = 4,
            ReadyNodes = 3,
            NotReadyNodes = 1
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
