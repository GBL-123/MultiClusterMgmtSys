using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

public class ClusterHealthRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db = SqliteDbFactory.CreateContext();
    private readonly ClusterHealthRepository _repo;
    private readonly ClusterRepository _clusterRepo;

    public ClusterHealthRepositoryTests()
    {
        _repo = new ClusterHealthRepository(_db);
        _clusterRepo = new ClusterRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<int> SeedClusterAsync(string name = "cluster-1")
    {
        var added = await _clusterRepo.AddAsync(TestData.NewCluster(name));
        return added.Id;
    }

    [Fact]
    public async Task AddAsync_appends_snapshot_row()
    {
        var clusterId = await SeedClusterAsync();

        await _repo.AddAsync(TestData.NewSnapshot(clusterId, totalNodes: 8, readyNodes: 6, notReadyNodes: 2));

        var stored = Assert.Single(await _db.ClusterHealthSnapshots.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(clusterId, stored.ClusterId);
        Assert.Equal(8, stored.TotalNodes);
        Assert.Equal(6, stored.ReadyNodes);
        Assert.Equal(2, stored.NotReadyNodes);
    }

    [Fact]
    public async Task AddAsync_keeps_previous_rows_instead_of_overwriting()
    {
        var clusterId = await SeedClusterAsync();
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: first, totalNodes: 8));
        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: first.AddMinutes(5), totalNodes: 9));

        Assert.Equal(2, await _db.ClusterHealthSnapshots.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestPerClusterAsync_picks_newest_snapshot_per_cluster()
    {
        var clusterA = await SeedClusterAsync("a");
        var clusterB = await SeedClusterAsync("b");
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _repo.AddAsync(TestData.NewSnapshot(clusterA, capturedAt: first, totalNodes: 3, readyNodes: 3, notReadyNodes: 0));
        await _repo.AddAsync(TestData.NewSnapshot(clusterA, capturedAt: first.AddMinutes(5), totalNodes: 5, readyNodes: 4, notReadyNodes: 1));
        await _repo.AddAsync(TestData.NewSnapshot(clusterB, capturedAt: first, totalNodes: 7, readyNodes: 7, notReadyNodes: 0));

        var latest = await _repo.GetLatestPerClusterAsync();

        Assert.Equal(2, latest.Count);
        Assert.Equal(5, latest[clusterA].TotalNodes);
        Assert.Equal(1, latest[clusterA].NotReadyNodes);
        Assert.Equal(7, latest[clusterB].TotalNodes);
    }

    [Fact]
    public async Task GetLatestPerClusterAsync_breaks_timestamp_ties_by_id()
    {
        var clusterId = await SeedClusterAsync();
        var sameMoment = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: sameMoment, totalNodes: 3));
        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: sameMoment, totalNodes: 9));

        var latest = await _repo.GetLatestPerClusterAsync();

        Assert.Equal(9, latest[clusterId].TotalNodes);
    }

    [Fact]
    public async Task GetLatestPerClusterAsync_excludes_clusters_without_snapshots()
    {
        var withSnapshot = await SeedClusterAsync("with");
        await SeedClusterAsync("without");
        await _repo.AddAsync(TestData.NewSnapshot(withSnapshot));

        var latest = await _repo.GetLatestPerClusterAsync();

        Assert.Single(latest);
        Assert.True(latest.ContainsKey(withSnapshot));
    }

    [Fact]
    public async Task GetLatestPerClusterAsync_is_empty_when_no_snapshots_exist()
    {
        await SeedClusterAsync();

        Assert.Empty(await _repo.GetLatestPerClusterAsync());
    }

    [Fact]
    public async Task Deleting_cluster_cascades_its_snapshots()
    {
        var clusterId = await SeedClusterAsync();
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: first));
        await _repo.AddAsync(TestData.NewSnapshot(clusterId, capturedAt: first.AddMinutes(5)));
        Assert.Equal(2, await _db.ClusterHealthSnapshots.CountAsync(TestContext.Current.CancellationToken));

        await _clusterRepo.DeleteAsync(clusterId);

        Assert.Equal(0, await _db.ClusterHealthSnapshots.CountAsync(TestContext.Current.CancellationToken));
    }
}
