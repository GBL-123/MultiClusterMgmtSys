using k8s;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterServiceTests
{
    private static ClusterQueryRequest Query() => new();

    [Fact]
    public async Task GetPaged_GroupIdZero_ReturnsOnlyUngrouped()
    {
        using var db = SqliteDbFactory.CreateContext();
        var group = TestData.NewGroup("生产");
        db.ClusterGroups.Add(group);
        await db.SaveChangesAsync();
        db.Clusters.AddRange(
            TestData.NewCluster("grouped", groupId: group.Id),
            TestData.NewCluster("ungrouped", groupId: null));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var req = Query();
        req.GroupId = 0;

        var result = await svc.GetPagedAsync(req);

        var names = result.Items.Select(c => c.Name).ToList();
        Assert.Equal(["ungrouped"], names);
    }

    [Fact]
    public async Task GetPaged_GroupIdNull_ReturnsAll()
    {
        using var db = SqliteDbFactory.CreateContext();
        var group = TestData.NewGroup("生产");
        db.ClusterGroups.Add(group);
        await db.SaveChangesAsync();
        db.Clusters.AddRange(
            TestData.NewCluster("grouped", groupId: group.Id),
            TestData.NewCluster("ungrouped", groupId: null));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var result = await svc.GetPagedAsync(Query());

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetPaged_VersionOnlyNull_ReturnsOnlyUnknownVersion()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.AddRange(
            TestData.NewCluster("has-version", version: "v1.30.3"),
            TestData.NewCluster("no-version", version: null));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var req = Query();
        req.VersionSelection = VersionFilterSentinel.OnlyNull;

        var result = await svc.GetPagedAsync(req);

        Assert.Equal(["no-version"], result.Items.Select(c => c.Name).ToList());
    }

    [Fact]
    public async Task GetPaged_DateRange_FiltersByCreatedAt()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.AddRange(
            TestData.NewCluster("old", createdAt: new DateTime(2026, 1, 1, 8, 0, 0)),
            TestData.NewCluster("middle", createdAt: new DateTime(2026, 1, 10, 8, 0, 0)),
            TestData.NewCluster("new", createdAt: new DateTime(2026, 1, 20, 8, 0, 0)));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var req = Query();
        req.DateRange = new DateRange(new DateTime(2026, 1, 10), new DateTime(2026, 1, 10));

        var result = await svc.GetPagedAsync(req);

        Assert.Equal(["middle"], result.Items.Select(c => c.Name).ToList());
    }

    [Fact]
    public async Task GetPaged_SortByNameAscending()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.AddRange(
            TestData.NewCluster("bravo"),
            TestData.NewCluster("alpha"),
            TestData.NewCluster("charlie"));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var req = Query();
        req.SortBy = ClusterSortField.Name;
        req.SortDescending = false;

        var result = await svc.GetPagedAsync(req);

        Assert.Equal(["alpha", "bravo", "charlie"], result.Items.Select(c => c.Name).ToList());
    }

    [Fact]
    public async Task GetClusterDetail_Missing_ReturnsNull()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var result = await svc.GetClusterDetailAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClusterDetail_OnlineCluster_NodeLoadFailure_DegradesToUnreachable()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("online", status: ClusterStatus.Online));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var result = await svc.GetClusterDetailAsync(1);

        Assert.NotNull(result);
        Assert.False(result.IsReachable);
    }

    [Fact]
    public async Task UpdateClusterEndpoints_Missing_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => svc.UpdateClusterEndpointsAsync(999, []));

        Assert.Contains("999", ex.UserMessage);
    }

    [Fact]
    public async Task RefreshAll_EmptyDb_ReturnsZero()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var succeeded = await svc.RefreshAllClustersStatusAsync();

        Assert.Equal(0, succeeded);
    }
}