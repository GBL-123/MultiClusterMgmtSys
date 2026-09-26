using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

public class HelmReleaseOwnershipRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db = SqliteDbFactory.CreateContext();
    private readonly HelmReleaseOwnershipRepository _repo;
    private readonly ClusterRepository _clusterRepo;

    public HelmReleaseOwnershipRepositoryTests()
    {
        _repo = new HelmReleaseOwnershipRepository(_db);
        _clusterRepo = new ClusterRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task UpsertAsync_inserts_then_get_returns_record()
    {
        var clusterId = await SeedClusterAsync();

        await _repo.UpsertAsync(NewOwnership(clusterId));

        var stored = await _repo.GetAsync(clusterId, "web", "nginx");
        Assert.NotNull(stored);
        Assert.Equal(7, stored!.OwnerUserId);
        Assert.Equal("alice", stored.OwnerUserName);
        Assert.Equal(1, stored.InstalledRevision);
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_record_without_duplicating()
    {
        var clusterId = await SeedClusterAsync();
        await _repo.UpsertAsync(NewOwnership(clusterId));

        await _repo.UpsertAsync(NewOwnership(clusterId, ownerUserId: 8, ownerUserName: "bob", installedRevision: 4));

        var stored = await _repo.GetAsync(clusterId, "web", "nginx");
        Assert.Equal(8, stored!.OwnerUserId);
        Assert.Equal("bob", stored.OwnerUserName);
        Assert.Equal(4, stored.InstalledRevision);
        Assert.Equal(1, await _db.HelmReleaseOwnerships.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByClusterAsync_returns_only_that_cluster()
    {
        var clusterA = await SeedClusterAsync("a");
        var clusterB = await SeedClusterAsync("b");
        await _repo.UpsertAsync(NewOwnership(clusterA, releaseName: "nginx"));
        await _repo.UpsertAsync(NewOwnership(clusterA, releaseName: "redis"));
        await _repo.UpsertAsync(NewOwnership(clusterB, releaseName: "nginx"));

        var records = await _repo.GetByClusterAsync(clusterA);

        Assert.Equal(2, records.Count);
        Assert.All(records, record => Assert.Equal(clusterA, record.ClusterId));
    }

    [Fact]
    public async Task DeleteAsync_removes_record_and_is_silent_when_missing()
    {
        var clusterId = await SeedClusterAsync();
        await _repo.UpsertAsync(NewOwnership(clusterId));

        await _repo.DeleteAsync(clusterId, "web", "nginx");
        await _repo.DeleteAsync(clusterId, "web", "nginx");

        Assert.Null(await _repo.GetAsync(clusterId, "web", "nginx"));
    }

    [Fact]
    public async Task Unique_index_rejects_duplicate_key()
    {
        var clusterId = await SeedClusterAsync();
        _db.HelmReleaseOwnerships.Add(NewOwnership(clusterId));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _db.HelmReleaseOwnerships.Add(NewOwnership(clusterId));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deleting_cluster_cascades_ownership_records()
    {
        var clusterId = await SeedClusterAsync();
        await _repo.UpsertAsync(NewOwnership(clusterId));

        await _clusterRepo.DeleteAsync(clusterId);

        Assert.Equal(0, await _db.HelmReleaseOwnerships.CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task<int> SeedClusterAsync(string name = "cluster-1")
    {
        var added = await _clusterRepo.AddAsync(TestData.NewCluster(name));
        return added.Id;
    }

    private static HelmReleaseOwnership NewOwnership(
        int clusterId,
        int ownerUserId = 7,
        string ownerUserName = "alice",
        int installedRevision = 1,
        string releaseName = "nginx")
        => new()
        {
            ClusterId = clusterId,
            Namespace = "web",
            ReleaseName = releaseName,
            OwnerUserId = ownerUserId,
            OwnerUserName = ownerUserName,
            InstalledAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
            InstalledRevision = installedRevision
        };
}
