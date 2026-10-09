using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Application.Services;

/// <summary>
/// 告警评估与查询服务测试:三条规则的判定矩阵、状态机开立/解析与查询面。
/// 用种子数据的时间差驱动全部开关边界,不引入时钟注入。
/// </summary>
public sealed class AlertServiceTests : IDisposable
{
    private readonly DateTime Now = DateTime.UtcNow;

    private readonly ServiceHarness _harness = new("admin", "Admin");

    private readonly IAlertRepository _alertRepo;

    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _alertRepo = new AlertRepository(_harness.Db);
        _service = CreateService(intervalMinutes: 5, syncEnabled: true, alertThresholdMinutes: 10);
    }

    public void Dispose() => _harness.Dispose();

    private AlertService CreateService(int intervalMinutes, bool syncEnabled, int alertThresholdMinutes)
    {
        var syncSettingService = new ClusterSyncSettingService(
            new AppSettingRepository(_harness.Db),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ClusterSync:Enabled"] = syncEnabled.ToString(),
                    ["ClusterSync:IntervalMinutes"] = intervalMinutes.ToString()
                })
                .Build(),
            TestHttpContext.For("admin", "Admin").Object,
            _harness.Audit,
            NullLogger<ClusterSyncSettingService>.Instance);
        var alertSettingService = new AlertSettingService(
            new AppSettingRepository(_harness.Db),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Alert:OfflineThresholdMinutes"] = alertThresholdMinutes.ToString()
                })
                .Build(),
            TestHttpContext.For("admin", "Admin").Object,
            _harness.Audit,
            NullLogger<AlertSettingService>.Instance);
        return new AlertService(
            _alertRepo,
            _harness.ClusterRepo,
            _harness.ClusterHealthRepo,
            syncSettingService,
            alertSettingService,
            NullLogger<AlertService>.Instance);
    }

    private async Task<int> SeedClusterAsync(string name, ClusterStatus status, DateTime? lastCheckedAt = null)
    {
        var cluster = TestData.NewCluster(name, status: status);
        cluster.LastCheckedAt = lastCheckedAt;
        _harness.Db.Clusters.Add(cluster);
        await _harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return cluster.Id;
    }

    private async Task SeedSnapshotAsync(int clusterId, DateTime capturedAt, int notReadyNodes = 0)
    {
        _harness.Db.ClusterHealthSnapshots.Add(new ClusterHealthSnapshot
        {
            ClusterId = clusterId,
            CapturedAt = capturedAt,
            TotalNodes = 3,
            ReadyNodes = 3 - notReadyNodes,
            NotReadyNodes = notReadyNodes
        });
        await _harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<int> SeedOpenAlertAsync(int clusterId, AlertRuleKind kind)
    {
        var record = new AlertRecord
        {
            ClusterId = clusterId,
            RuleKind = kind,
            OpenedAt = Now.AddHours(-1)
        };
        await _alertRepo.AddAsync(record);
        return record.Id;
    }

    [Fact]
    public async Task Offline_beyond_threshold_opens_alert()
    {
        var clusterId = await SeedClusterAsync("c-off-11", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));

        var (opened, resolved) = await _service.EvaluateAsync();

        Assert.Equal(1, opened);
        Assert.Equal(0, resolved);
        var open = await _alertRepo.GetOpenAsync();
        var record = Assert.Single(open);
        Assert.Equal(clusterId, record.ClusterId);
        Assert.Equal(AlertRuleKind.ClusterOffline, record.RuleKind);
        Assert.Null(record.ResolvedAt);
    }

    [Fact]
    public async Task Offline_within_threshold_does_not_open()
    {
        var clusterId = await SeedClusterAsync("c-off-9", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-9));

        var (opened, _) = await _service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Empty(await _alertRepo.GetOpenAsync());
    }

    [Fact]
    public async Task Already_open_is_not_duplicated()
    {
        var clusterId = await SeedClusterAsync("c-dup", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));
        await SeedOpenAlertAsync(clusterId, AlertRuleKind.ClusterOffline);

        var (opened, resolved) = await _service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Equal(0, resolved);
        Assert.Single(await _alertRepo.GetOpenAsync());
    }

    [Fact]
    public async Task Not_ready_snapshot_opens_alert_with_detail()
    {
        var clusterId = await SeedClusterAsync("c-notready", ClusterStatus.Online, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-1), notReadyNodes: 2);

        var (opened, _) = await _service.EvaluateAsync();

        Assert.Equal(1, opened);
        var open = await _alertRepo.GetOpenAsync();
        var record = Assert.Single(open);
        Assert.Equal(AlertRuleKind.NodeNotReady, record.RuleKind);
        Assert.Equal("未就绪节点 2 个", record.Detail);
    }

    [Fact]
    public async Task Not_ready_back_to_zero_resolves_alert()
    {
        var clusterId = await SeedClusterAsync("c-recovered", ClusterStatus.Online, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-1), notReadyNodes: 0);
        await SeedOpenAlertAsync(clusterId, AlertRuleKind.NodeNotReady);

        var (_, resolved) = await _service.EvaluateAsync();

        Assert.Equal(1, resolved);
        Assert.Empty(await _alertRepo.GetOpenAsync());
        var all = _harness.Db.AlertRecords.ToList();
        Assert.Single(all);
        Assert.NotNull(all[0].ResolvedAt);
    }

    [Fact]
    public async Task No_snapshot_opens_nothing()
    {
        await SeedClusterAsync("c-never-probed", ClusterStatus.Offline, lastCheckedAt: null);

        var (opened, _) = await _service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Empty(await _alertRepo.GetOpenAsync());
    }

    [Fact]
    public async Task Sync_enabled_and_probe_stalled_beyond_window_opens_stall_alert()
    {
        var service = CreateService(intervalMinutes: 5, syncEnabled: true, alertThresholdMinutes: 10);
        var clusterId = await SeedClusterAsync("c-stalled", ClusterStatus.Online, Now.AddMinutes(-11));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));

        var (opened, _) = await service.EvaluateAsync();

        Assert.Equal(1, opened);
        var open = await _alertRepo.GetOpenAsync();
        Assert.Equal(AlertRuleKind.SnapshotStalled, Assert.Single(open).RuleKind);
    }

    [Fact]
    public async Task Sync_disabled_does_not_open_stall_alert()
    {
        var service = CreateService(intervalMinutes: 5, syncEnabled: false, alertThresholdMinutes: 10);
        var clusterId = await SeedClusterAsync("c-sync-off", ClusterStatus.Online, Now.AddMinutes(-600));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-600));

        var (opened, _) = await service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Empty(await _alertRepo.GetOpenAsync());
    }

    [Fact]
    public async Task Last_checked_at_null_does_not_open_stall_alert()
    {
        var clusterId = await SeedClusterAsync("c-no-baseline", ClusterStatus.Online, lastCheckedAt: null);
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-600));

        var (opened, _) = await _service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Empty(await _alertRepo.GetOpenAsync());
    }

    [Fact]
    public async Task Stall_threshold_follows_interval_setting()
    {
        // 生效间隔 30 分钟 → 断流阈值 60 分钟:20 分钟陈旧不开、70 分钟陈旧开。
        var wide = CreateService(intervalMinutes: 30, syncEnabled: true, alertThresholdMinutes: 10);
        var staleId = await SeedClusterAsync("c-stale-20", ClusterStatus.Online, Now.AddMinutes(-20));
        await SeedSnapshotAsync(staleId, Now.AddMinutes(-20));

        var (opened, _) = await wide.EvaluateAsync();

        Assert.Equal(0, opened);

        var veryStaleId = await SeedClusterAsync("c-stale-70", ClusterStatus.Online, Now.AddMinutes(-70));
        await SeedSnapshotAsync(veryStaleId, Now.AddMinutes(-70));

        var (secondOpened, _) = await wide.EvaluateAsync();

        Assert.Equal(1, secondOpened);
        var open = await wide.GetAlertsAsync(new AlertListRequest { Page = 1, PageSize = 10 });
        Assert.Contains(open.Items, a => a.RuleKind == AlertRuleKind.SnapshotStalled && a.ClusterId == veryStaleId);
    }

    [Fact]
    public async Task Recovery_resolves_with_trace()
    {
        var clusterId = await SeedClusterAsync("c-back-online", ClusterStatus.Online, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-1));
        await SeedOpenAlertAsync(clusterId, AlertRuleKind.ClusterOffline);

        var (opened, resolved) = await _service.EvaluateAsync();

        Assert.Equal(0, opened);
        Assert.Equal(1, resolved);
        var all = _harness.Db.AlertRecords.ToList();
        var record = Assert.Single(all);
        Assert.Equal(AlertRuleKind.ClusterOffline, record.RuleKind);
        Assert.NotNull(record.ResolvedAt);
    }

    [Fact]
    public async Task Evaluate_writes_no_audit_entries()
    {
        var clusterId = await SeedClusterAsync("c-no-audit", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));

        await _service.EvaluateAsync();
        await _service.EvaluateAsync();

        Assert.Empty(_harness.Db.AuditLogs.ToList());
    }

    [Fact]
    public async Task GetAlerts_joins_cluster_name_and_translates_rule_text()
    {
        var clusterId = await SeedClusterAsync("prod-web", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11), notReadyNodes: 1);

        await _service.EvaluateAsync();

        var page = await _service.GetAlertsAsync(new AlertListRequest { Resolved = false });
        Assert.Equal(2, page.Total);
        Assert.All(page.Items, a => Assert.Equal("prod-web", a.ClusterName));
        var notReady = Assert.Single(page.Items, a => a.RuleKind == AlertRuleKind.NodeNotReady);
        Assert.Equal("未就绪节点 1 个", notReady.Detail);
        var offline = Assert.Single(page.Items, a => a.RuleKind == AlertRuleKind.ClusterOffline);
        Assert.Null(offline.Detail);
        Assert.Contains(page.Items, a => a.RuleText == "集群离线");
        Assert.Contains(page.Items, a => a.RuleText == "节点未就绪");
    }

    [Fact]
    public async Task GetAlerts_filters_by_status_and_paginates()
    {
        var clusterId = await SeedClusterAsync("c-page", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));
        await _service.EvaluateAsync();
        // 造一条已解析记录:直接开一条不会再命中的规则类别再手动解析。
        var resolvedId = await SeedOpenAlertAsync(clusterId, AlertRuleKind.SnapshotStalled);
        await _alertRepo.ResolveAsync(resolvedId, Now);

        var openPage = await _service.GetAlertsAsync(new AlertListRequest { Resolved = false, Page = 1, PageSize = 2 });
        Assert.Equal(1, openPage.Total);
        Assert.All(openPage.Items, a => Assert.False(a.IsResolved));

        var resolvedPage = await _service.GetAlertsAsync(new AlertListRequest { Resolved = true });
        Assert.Equal(1, resolvedPage.Total);
        Assert.All(resolvedPage.Items, a => Assert.True(a.IsResolved));

        var emptyPage = await _service.GetAlertsAsync(new AlertListRequest { Resolved = true, Page = 5, PageSize = 10 });
        Assert.Equal(1, emptyPage.Total);
        Assert.Empty(emptyPage.Items);
    }

    [Fact]
    public async Task GetOpenCount_reflects_open_alerts()
    {
        Assert.Equal(0, await _service.GetOpenCountAsync());

        var clusterId = await SeedClusterAsync("c-bell", ClusterStatus.Offline, Now.AddMinutes(-1));
        await SeedSnapshotAsync(clusterId, Now.AddMinutes(-11));
        await _service.EvaluateAsync();

        Assert.Equal(1, await _service.GetOpenCountAsync());
    }
}
