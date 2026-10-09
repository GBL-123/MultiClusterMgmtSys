using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class DashboardServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var httpContext = TestHttpContext.For("admin", "Admin");
        var syncSettings = new ClusterSyncSettingService(
            new AppSettingRepository(_harness.Db),
            EmptyConfiguration(),
            httpContext.Object,
            _harness.Audit,
            NullLogger<ClusterSyncSettingService>.Instance);
        _service = new DashboardService(
            _harness.ClusterRepo, _harness.ClusterHealthRepo, _harness.Audit, syncSettings,
            httpContext.Object, NullLogger<DashboardService>.Instance);
    }

    public void Dispose() => _harness.Dispose();

    private static IConfiguration EmptyConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

    private async Task<int> SeedClusterAsync(
        string name,
        ClusterStatus status = ClusterStatus.Online,
        string? version = "v1.31.2",
        int? groupId = null,
        int nodeCount = 3,
        DateTime? lastCheckedAt = null)
    {
        var cluster = TestData.NewCluster(name, groupId: groupId, status: status, version: version, nodeCount: nodeCount);
        cluster.LastCheckedAt = lastCheckedAt ?? DateTime.UtcNow;
        var added = await _harness.ClusterRepo.AddAsync(cluster);
        return added.Id;
    }

    private async Task<int> SeedNeverProbedAsync(string name)
    {
        var cluster = TestData.NewCluster(name, status: ClusterStatus.Unknown, version: null);
        cluster.LastCheckedAt = null;
        var added = await _harness.ClusterRepo.AddAsync(cluster);
        return added.Id;
    }

    private async Task<int> SeedGroupAsync(string name)
    {
        var group = _harness.Db.ClusterGroups.Add(TestData.NewGroup(name)).Entity;
        await _harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return group.Id;
    }

    private async Task SeedSnapshotAsync(int clusterId, DateTime capturedAtUtc, int ready, int notReady)
    {
        var captured = DateTime.SpecifyKind(capturedAtUtc, DateTimeKind.Utc);
        await _harness.ClusterHealthRepo.AddAsync(new ClusterHealthSnapshot
        {
            ClusterId = clusterId,
            CapturedAt = captured,
            TotalNodes = ready + notReady,
            ReadyNodes = ready,
            NotReadyNodes = notReady
        });
    }

    [Fact]
    public async Task GetDashboardAsync_reports_empty_state_when_no_clusters_exist()
    {
        var model = await _service.GetDashboardAsync();

        Assert.False(model.HasClusters);
        Assert.Equal(0, model.TotalClusters);
        Assert.Empty(model.AttentionItems);
        Assert.Empty(model.GroupHealth);
        Assert.Empty(model.VersionBuckets);
        Assert.Equal(0, model.FleetSize.NodeCount);
    }

    [Fact]
    public async Task GetDashboardAsync_aggregates_status_counts()
    {
        await SeedClusterAsync("online-1");
        await SeedClusterAsync("online-2", status: ClusterStatus.Online);
        await SeedClusterAsync("down", status: ClusterStatus.Offline);
        await SeedNeverProbedAsync("unknown");

        var model = await _service.GetDashboardAsync();

        Assert.True(model.HasClusters);
        Assert.Equal(4, model.TotalClusters);
        Assert.Equal(2, model.OnlineClusters);
        Assert.Equal(1, model.OfflineClusters);
        Assert.Equal(1, model.UnknownClusters);
    }

    [Fact]
    public async Task GetDashboardAsync_aggregates_group_health_with_ungrouped_last()
    {
        var prod = await SeedGroupAsync("生产环境");
        await SeedClusterAsync("prod-a", groupId: prod);
        await SeedClusterAsync("prod-b", status: ClusterStatus.Offline, groupId: prod);
        await SeedClusterAsync("loose", status: ClusterStatus.Unknown, groupId: null);

        var model = await _service.GetDashboardAsync();

        var prodHealth = model.GroupHealth.Single(g => g.GroupName == "生产环境");
        Assert.Equal(2, prodHealth.TotalClusters);
        Assert.Equal(1, prodHealth.OnlineClusters);

        var ungrouped = model.GroupHealth.Single(g => g.GroupName == "未分组");
        Assert.Equal(1, ungrouped.TotalClusters);
        Assert.Equal(0, ungrouped.OnlineClusters);
        Assert.Equal("未分组", model.GroupHealth[^1].GroupName);
    }

    [Fact]
    public async Task GetDashboardAsync_aggregates_version_distribution_with_unprobed_last()
    {
        await SeedClusterAsync("a", version: "v1.31.2");
        await SeedClusterAsync("b", version: "v1.31.2");
        await SeedClusterAsync("c", version: "v1.30.6");
        await SeedNeverProbedAsync("d");

        var model = await _service.GetDashboardAsync();

        Assert.Equal(2, model.VersionBuckets.Single(b => b.Version == "v1.31.2").Count);
        Assert.Equal(1, model.VersionBuckets.Single(b => b.Version == "v1.30.6").Count);

        var unprobed = model.VersionBuckets.Single(b => b.Version is null);
        Assert.Equal("未探测", unprobed.Label);
        Assert.Equal(1, unprobed.Count);
        Assert.Null(model.VersionBuckets[^1].Version);
    }

    [Fact]
    public async Task GetDashboardAsync_lists_offline_and_never_probed_clusters_in_attention()
    {
        await SeedClusterAsync("healthy");
        await SeedClusterAsync("down", status: ClusterStatus.Offline);
        await SeedNeverProbedAsync("never");

        var model = await _service.GetDashboardAsync();

        Assert.Equal(2, model.AttentionItems.Count);

        var downItem = model.AttentionItems.Single(i => i.ClusterName == "down");
        Assert.Equal(DashboardAttentionKind.Offline, downItem.Kind);
        Assert.Equal("离线", downItem.KindText);
        Assert.Equal("offline", downItem.BadgeCssClass);
        Assert.NotNull(downItem.LastCheckedAt);

        var neverItem = model.AttentionItems.Single(i => i.ClusterName == "never");
        Assert.Equal(DashboardAttentionKind.NeverProbed, neverItem.Kind);
        Assert.Equal("从未探测", neverItem.KindText);
        Assert.Equal("unknown", neverItem.BadgeCssClass);
        Assert.Null(neverItem.LastCheckedAt);

        Assert.Equal(DashboardAttentionKind.Offline, model.AttentionItems[0].Kind);
    }

    [Fact]
    public async Task GetDashboardAsync_has_no_attention_items_when_all_clusters_are_online()
    {
        await SeedClusterAsync("healthy-1");
        await SeedClusterAsync("healthy-2");

        var model = await _service.GetDashboardAsync();

        Assert.Empty(model.AttentionItems);
    }

    [Fact]
    public async Task GetDashboardAsync_fleet_size_keeps_last_known_nodes_of_offline_cluster()
    {
        var offline = await SeedClusterAsync("down", status: ClusterStatus.Offline, nodeCount: 0);
        await _harness.ClusterHealthRepo.AddAsync(TestData.NewSnapshot(
            offline, capturedAt: DateTime.UtcNow.AddMinutes(-30), totalNodes: 8, readyNodes: 6, notReadyNodes: 2));
        var online = await SeedClusterAsync("up", status: ClusterStatus.Online, nodeCount: 3);
        await _harness.ClusterHealthRepo.AddAsync(TestData.NewSnapshot(
            online, capturedAt: DateTime.UtcNow, totalNodes: 5, readyNodes: 5, notReadyNodes: 0));
        await SeedNeverProbedAsync("never");

        var model = await _service.GetDashboardAsync();

        Assert.Equal(13, model.FleetSize.NodeCount);
        Assert.Equal(11, model.FleetSize.ReadyNodes);
        Assert.Equal(2, model.FleetSize.NotReadyNodes);
        Assert.Equal(model.FleetSize.NodeCount, model.FleetSize.ReadyNodes + model.FleetSize.NotReadyNodes);
        Assert.Equal(1, model.FleetSize.ClustersWithoutSnapshot);
        Assert.True(model.FleetSize.IncludesStaleData);
        Assert.NotNull(model.FleetSize.OldestCapturedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_fleet_size_is_zero_and_not_stale_without_snapshots()
    {
        await SeedClusterAsync("no-snapshot");

        var model = await _service.GetDashboardAsync();

        Assert.Equal(0, model.FleetSize.NodeCount);
        Assert.Equal(0, model.FleetSize.ReadyNodes);
        Assert.Equal(0, model.FleetSize.NotReadyNodes);
        Assert.Equal(1, model.FleetSize.ClustersWithoutSnapshot);
        Assert.Null(model.FleetSize.OldestCapturedAt);
        Assert.False(model.FleetSize.IncludesStaleData);
    }

    [Fact]
    public async Task GetDashboardAsync_fleet_size_is_not_stale_when_all_contributors_are_online()
    {
        var online = await SeedClusterAsync("up");
        await _harness.ClusterHealthRepo.AddAsync(TestData.NewSnapshot(online, capturedAt: DateTime.UtcNow, totalNodes: 5));

        var model = await _service.GetDashboardAsync();

        Assert.Equal(5, model.FleetSize.NodeCount);
        Assert.False(model.FleetSize.IncludesStaleData);
    }

    [Fact]
    public async Task GetDashboardAsync_freshness_is_fresh_within_two_intervals()
    {
        await SeedClusterAsync("recent", lastCheckedAt: DateTime.UtcNow.AddMinutes(-3));

        var model = await _service.GetDashboardAsync();

        Assert.Equal(DashboardFreshnessState.Fresh, model.Freshness.State);
        Assert.Equal(5, model.Freshness.SyncIntervalMinutes);
        Assert.NotNull(model.Freshness.LastSyncedAt);
        Assert.NotEqual("—", model.Freshness.LastSyncedRelativeText);
    }

    [Fact]
    public async Task GetDashboardAsync_freshness_is_stale_beyond_two_intervals()
    {
        await SeedClusterAsync("old", lastCheckedAt: DateTime.UtcNow.AddMinutes(-47));

        var model = await _service.GetDashboardAsync();

        Assert.Equal(DashboardFreshnessState.Stale, model.Freshness.State);
    }

    [Fact]
    public async Task GetDashboardAsync_freshness_reports_never_synced_without_any_probe()
    {
        await SeedNeverProbedAsync("never");

        var model = await _service.GetDashboardAsync();

        Assert.Equal(DashboardFreshnessState.NeverSynced, model.Freshness.State);
        Assert.Null(model.Freshness.LastSyncedAt);
    }

    [Fact]
    public async Task GetDashboardAsync_freshness_reports_sync_disabled_instead_of_stale()
    {
        await SeedClusterAsync("old", lastCheckedAt: DateTime.UtcNow.AddMinutes(-47));
        await _harness.Db.AppSettings.AddAsync(TestData.NewSetting("ClusterSync:Enabled", "false"));
        await _harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var model = await _service.GetDashboardAsync();

        Assert.Equal(DashboardFreshnessState.SyncDisabled, model.Freshness.State);
    }

    [Fact]
    public async Task GetDashboardAsync_admin_sees_system_wide_recent_activity()
    {
        await _harness.Db.AuditLogs.AddRangeAsync(
            TestData.NewAudit(userName: "admin", target: "集群 prod"),
            TestData.NewAudit(userName: "member", target: "集群 dev"));
        await _harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var model = await _service.GetDashboardAsync();

        Assert.Equal(2, model.RecentActivity.Count);
        Assert.Contains(model.RecentActivity, a => a.UserName == "member");
    }

    [Fact]
    public async Task GetDashboardAsync_member_sees_only_own_recent_activity()
    {
        using var memberHarness = new ServiceHarness("member");
        await memberHarness.Db.AuditLogs.AddRangeAsync(
            TestData.NewAudit(userName: "admin", target: "集群 prod"),
            TestData.NewAudit(userName: "member", target: "集群 dev"));
        await memberHarness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var memberHttp = TestHttpContext.For("member");
        var syncSettings = new ClusterSyncSettingService(
            new AppSettingRepository(memberHarness.Db),
            EmptyConfiguration(),
            memberHttp.Object,
            memberHarness.Audit,
            NullLogger<ClusterSyncSettingService>.Instance);
        var memberService = new DashboardService(
            memberHarness.ClusterRepo, memberHarness.ClusterHealthRepo, memberHarness.Audit,
            syncSettings, memberHttp.Object, NullLogger<DashboardService>.Instance);

        var model = await memberService.GetDashboardAsync();

        Assert.Single(model.RecentActivity);
        Assert.Equal("member", model.RecentActivity[0].UserName);
    }

    [Fact]
    public async Task GetNodeReadinessTrend_advances_fleet_totals_per_snapshot_event()
    {
        var baseTime = DateTime.UtcNow.AddHours(-5);
        var alpha = await SeedClusterAsync("alpha");
        var beta = await SeedClusterAsync("beta");
        await SeedSnapshotAsync(beta, baseTime.AddHours(-2), 4, 0);
        await SeedSnapshotAsync(alpha, baseTime.AddHours(-1), 6, 2);
        await SeedSnapshotAsync(alpha, baseTime, 5, 3);

        var points = await _service.GetNodeReadinessTrendAsync();

        Assert.Equal(3, points.Count);
        Assert.Equal(new DashboardTrendPointViewModel(baseTime.AddHours(-2), 4, 0), points[0]);
        Assert.Equal(new DashboardTrendPointViewModel(baseTime.AddHours(-1), 10, 2), points[1]);
        Assert.Equal(new DashboardTrendPointViewModel(baseTime, 9, 3), points[2]);
    }

    [Fact]
    public async Task GetNodeReadinessTrend_aggregates_same_second_events_keeping_last()
    {
        var nowTruncated = new DateTime(
            DateTime.UtcNow.AddHours(-5).Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond,
            DateTimeKind.Utc);
        var baseTime = nowTruncated;
        var alpha = await SeedClusterAsync("alpha-second");
        var beta = await SeedClusterAsync("beta-second");
        await SeedSnapshotAsync(alpha, baseTime.AddHours(-2), 4, 0);
        await SeedSnapshotAsync(beta, baseTime.AddHours(-1), 3, 1);
        await SeedSnapshotAsync(alpha, baseTime.AddHours(-1).AddMilliseconds(250), 5, 3);

        var points = await _service.GetNodeReadinessTrendAsync();

        Assert.Equal(2, points.Count);
        Assert.Equal(new DashboardTrendPointViewModel(baseTime.AddHours(-2), 4, 0), points[0]);
        Assert.Equal(new DashboardTrendPointViewModel(baseTime.AddHours(-1).AddMilliseconds(250), 8, 4), points[1]);
    }

    [Fact]
    public async Task GetNodeReadinessTrend_excludes_snapshots_outside_window()
    {
        var clusterId = await SeedClusterAsync("old-cluster");
        await SeedSnapshotAsync(clusterId, DateTime.UtcNow.AddHours(-25), 8, 0);
        await SeedSnapshotAsync(clusterId, DateTime.UtcNow.AddHours(-1), 6, 2);

        var points = await _service.GetNodeReadinessTrendAsync();

        var point = Assert.Single(points);
        Assert.Equal(6, point.ReadyNodes);
        Assert.Equal(2, point.NotReadyNodes);
    }

    [Fact]
    public async Task GetNodeReadinessTrend_returns_empty_list_without_window_snapshots()
    {
        await SeedClusterAsync("no-snapshots");

        var points = await _service.GetNodeReadinessTrendAsync();

        Assert.Empty(points);
    }
}
