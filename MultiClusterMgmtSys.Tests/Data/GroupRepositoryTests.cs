using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Data;

public class GroupRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext db = SqliteDbFactory.CreateContext();
    private readonly GroupRepository repo;

    public GroupRepositoryTests()
    {
        repo = new GroupRepository(db);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetAllAsync_returns_groups_with_cluster_counts()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        var g2 = db.ClusterGroups.Add(TestData.NewGroup("g2")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.Clusters.Add(TestData.NewCluster("c1", groupId: g1.Id));
        db.Clusters.Add(TestData.NewCluster("c2", groupId: g1.Id));
        db.Clusters.Add(TestData.NewCluster("c3", groupId: g2.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var groups = await repo.GetAllAsync();

        Assert.True(groups.Count == 2);
        Assert.Equal(2, groups[0].Clusters.Count);
        Assert.Equal(1, groups[1].Clusters.Count);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_missing()
    {
        Assert.Null(await repo.GetByIdAsync(999));
    }

    [Fact]
    public async Task AddAsync_returns_entity_with_id()
    {
        var added = await repo.AddAsync(TestData.NewGroup("prod"));

        Assert.True(added.Id > 0);
        Assert.Equal("prod", (await repo.GetByIdAsync(added.Id))!.Name);
    }

    [Fact]
    public async Task RenameAsync_updates_name()
    {
        var added = await repo.AddAsync(TestData.NewGroup("old-name"));

        await repo.RenameAsync(added.Id, "new-name");

        Assert.Equal("new-name", (await repo.GetByIdAsync(added.Id))!.Name);
    }

    [Fact]
    public async Task DeleteAsync_ungroups_clusters_setnull()
    {
        var added = await repo.AddAsync(TestData.NewGroup("doomed"));
        db.Clusters.Add(TestData.NewCluster("orphan", groupId: added.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repo.DeleteAsync(added.Id);

        Assert.Null(await repo.GetByIdAsync(added.Id));
        var cluster = await db.Clusters.AsNoTracking().SingleAsync(c => c.Name == "orphan", TestContext.Current.CancellationToken);
        Assert.Null(cluster.GroupId);
    }
}
