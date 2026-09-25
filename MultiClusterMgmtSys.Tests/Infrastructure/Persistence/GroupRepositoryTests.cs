using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

public class GroupRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db = SqliteDbFactory.CreateContext();
    private readonly GroupRepository _repo;

    public GroupRepositoryTests()
    {
        _repo = new GroupRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_returns_groups_with_cluster_counts()
    {
        var g1 = _db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        var g2 = _db.ClusterGroups.Add(TestData.NewGroup("g2")).Entity;
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
        _db.Clusters.Add(TestData.NewCluster("c1", groupId: g1.Id));
        _db.Clusters.Add(TestData.NewCluster("c2", groupId: g1.Id));
        _db.Clusters.Add(TestData.NewCluster("c3", groupId: g2.Id));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var groups = await _repo.GetAllAsync();

        Assert.True(groups.Count == 2);
        Assert.Equal(2, groups[0].Clusters.Count);
        Assert.Equal(1, groups[1].Clusters.Count);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_missing()
    {
        Assert.Null(await _repo.GetByIdAsync(999));
    }

    [Fact]
    public async Task AddAsync_returns_entity_with_id()
    {
        var added = await _repo.AddAsync(TestData.NewGroup("prod"));

        Assert.True(added.Id > 0);
        Assert.Equal("prod", (await _repo.GetByIdAsync(added.Id))!.Name);
    }

    [Fact]
    public async Task RenameAsync_updates_name()
    {
        var added = await _repo.AddAsync(TestData.NewGroup("old-name"));

        await _repo.RenameAsync(added.Id, "new-name");

        Assert.Equal("new-name", (await _repo.GetByIdAsync(added.Id))!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ungroups_clusters_setnull()
    {
        var added = await _repo.AddAsync(TestData.NewGroup("doomed"));
        _db.Clusters.Add(TestData.NewCluster("orphan", groupId: added.Id));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _repo.DeleteAsync(added.Id);

        Assert.Null(await _repo.GetByIdAsync(added.Id));
        var cluster = await _db.Clusters.AsNoTracking().SingleAsync(c => c.Name == "orphan", TestContext.Current.CancellationToken);
        Assert.Null(cluster.GroupId);
    }
}
