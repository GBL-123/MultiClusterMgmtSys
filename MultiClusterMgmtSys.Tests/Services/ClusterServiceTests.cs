using k8s;
using k8s.Autorest;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
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
        req.CreatedFrom = new DateTime(2026, 1, 10);
        req.CreatedTo = new DateTime(2026, 1, 10);

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
            () => svc.UpdateClusterEndpointsAsync(new ClusterEndpointsUpdateRequest(999, [])));

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

    [Fact]
    public async Task RefreshAll_StatusFlips_RecordsAuditWithScheduledSource()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("down", status: ClusterStatus.Online));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var succeeded = await svc.RefreshAllClustersStatusAsync(source: ClusterSyncSource.Scheduled);

        Assert.Equal(1, succeeded);
        var entry = Assert.Single(db.AuditLogs.ToList());
        Assert.Equal(AuditCategory.Cluster, entry.Category);
        Assert.Equal(AuditAction.Update, entry.Action);
        Assert.Contains("状态由 在线 变为 离线(定时同步)", entry.Target);
    }

    [Fact]
    public async Task RefreshAll_StatusFlips_RecordsAuditWithManualSource()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("down", status: ClusterStatus.Online));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        var succeeded = await svc.RefreshAllClustersStatusAsync();

        Assert.Equal(1, succeeded);
        var entry = Assert.Single(db.AuditLogs.ToList());
        Assert.Contains("状态由 在线 变为 离线(手动刷新)", entry.Target);
    }

    [Fact]
    public async Task RefreshAll_StatusUnchanged_NoAudit()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("still-down", status: ClusterStatus.Offline));
        await db.SaveChangesAsync();
        var svc = TestServices.Cluster(db, TestServices.ThrowingFactory());

        await svc.RefreshAllClustersStatusAsync();

        Assert.Empty(db.AuditLogs.ToList());
    }

    [Fact]
    public async Task RefreshAll_ConcurrentCalls_AreSerialized()
    {
        using var dbA = SqliteDbFactory.CreateContext();
        dbA.Clusters.Add(TestData.NewCluster("blocked"));
        await dbA.SaveChangesAsync();

        using var dbB = SqliteDbFactory.CreateContext();
        dbB.Clusters.Add(TestData.NewCluster("other"));
        await dbB.SaveChangesAsync();

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                entered.TrySetResult();
                await release.Task;
                return new HttpOperationResponse<VersionInfo> { Body = new VersionInfo { GitVersion = "v1.30.3" } };
            });
        fake.Setup(c => c.CoreV1.ListNodeWithHttpMessagesAsync(
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1NodeList> { Body = new V1NodeList() });

        var svcA = TestServices.Cluster(dbA, _ => fake.Object);
        var svcB = TestServices.Cluster(dbB, TestServices.ThrowingFactory());

        var first = svcA.RefreshAllClustersStatusAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = svcB.RefreshAllClustersStatusAsync();
        await Task.Delay(300);
        Assert.False(second.IsCompleted);

        release.TrySetResult();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal([1, 1], results);
    }
}