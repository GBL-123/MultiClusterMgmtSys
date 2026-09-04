using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class GroupServiceTests
{
    [Fact]
    public async Task AddGroup_CreatesGroup()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Group(db);

        await svc.AddGroupAsync("测试分组");

        Assert.Equal(1, db.ClusterGroups.Count(g => g.Name == "测试分组"));
    }

    [Fact]
    public async Task RenameGroup_Missing_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Group(db);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => svc.RenameGroupAsync(999, "新名"));

        Assert.Contains("999", ex.UserMessage);
    }

    [Fact]
    public async Task RenameGroup_UpdatesName()
    {
        using var db = SqliteDbFactory.CreateContext();
        var group = TestData.NewGroup("旧名");
        db.ClusterGroups.Add(group);
        await db.SaveChangesAsync();
        var svc = TestServices.Group(db);

        await svc.RenameGroupAsync(group.Id, "新名");

        Assert.Equal("新名", db.ClusterGroups.Single(g => g.Id == group.Id).Name);
    }

    [Fact]
    public async Task MoveClusters_SentinelZero_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Group(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => svc.MoveClustersToGroupAsync([1], 0));
    }

    [Fact]
    public async Task MoveClusters_ToNull_UngroupsClusters()
    {
        using var db = SqliteDbFactory.CreateContext();
        var group = TestData.NewGroup("生产");
        db.ClusterGroups.Add(group);
        await db.SaveChangesAsync();
        db.Clusters.Add(TestData.NewCluster("c1", groupId: group.Id));
        await db.SaveChangesAsync();
        var svc = TestServices.Group(db);

        var affected = await svc.MoveClustersToGroupAsync([1], null);

        Assert.Equal(1, affected);
        Assert.Null(db.Clusters.AsNoTracking().Single(c => c.Id == 1).GroupId);
    }

    [Fact]
    public async Task MoveClusters_ToGroup_AssignsGroup()
    {
        using var db = SqliteDbFactory.CreateContext();
        var group = TestData.NewGroup("生产");
        db.ClusterGroups.Add(group);
        await db.SaveChangesAsync();
        db.Clusters.Add(TestData.NewCluster("c1", groupId: null));
        await db.SaveChangesAsync();
        var svc = TestServices.Group(db);

        var affected = await svc.MoveClustersToGroupAsync([1], group.Id);

        Assert.Equal(1, affected);
        Assert.Equal(group.Id, db.Clusters.AsNoTracking().Single(c => c.Id == 1).GroupId);
    }
}