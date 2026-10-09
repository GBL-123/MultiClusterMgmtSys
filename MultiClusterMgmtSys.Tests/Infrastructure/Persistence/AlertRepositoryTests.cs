using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

/// <summary>
/// 告警记录仓储测试:分页/过滤/排序/总数、同键 open 唯一约束、解析回填与集群删除级联。
/// </summary>
public sealed class AlertRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    private readonly IAlertRepository _repo;

    private readonly int _clusterId;

    public AlertRepositoryTests()
    {
        _db = SqliteDbFactory.CreateContext();
        _repo = new AlertRepository(_db);
        var cluster = TestData.NewCluster("alert-cluster");
        _db.Clusters.Add(cluster);
        _db.SaveChanges();
        _clusterId = cluster.Id;
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task GetPagedAsync_filters_sorts_and_counts()
    {
        await SeedAsync(AlertRuleKind.ClusterOffline, hoursAgo: -3, resolvedAt: null);
        await SeedAsync(AlertRuleKind.NodeNotReady, hoursAgo: -2, resolvedAt: DateTime.UtcNow);
        await SeedAsync(AlertRuleKind.SnapshotStalled, hoursAgo: -1, resolvedAt: null);

        var (openItems, openTotal) = await _repo.GetPagedAsync(new AlertPageQuery { Resolved = false });

        Assert.Equal(2, openTotal);
        Assert.Equal(2, openItems.Count);
        Assert.All(openItems, r => Assert.Null(r.ResolvedAt));
        // 开立时间倒序:最近开立的 SnapshotStalled 在前。
        Assert.Equal(AlertRuleKind.SnapshotStalled, openItems[0].RuleKind);
        Assert.Equal(AlertRuleKind.ClusterOffline, openItems[1].RuleKind);

        var (resolvedItems, resolvedTotal) = await _repo.GetPagedAsync(new AlertPageQuery { Resolved = true });

        Assert.Equal(1, resolvedTotal);
        Assert.Equal(AlertRuleKind.NodeNotReady, Assert.Single(resolvedItems).RuleKind);

        var (allItems, allTotal) = await _repo.GetPagedAsync(new AlertPageQuery { Resolved = null });

        Assert.Equal(3, allTotal);
        Assert.Equal(3, allItems.Count);
    }

    [Fact]
    public async Task GetPagedAsync_paginates_beyond_page()
    {
        await SeedAsync(AlertRuleKind.ClusterOffline, hoursAgo: -3, resolvedAt: null);
        await SeedAsync(AlertRuleKind.NodeNotReady, hoursAgo: -2, resolvedAt: null);

        var (items, total) = await _repo.GetPagedAsync(new AlertPageQuery { Page = 2, PageSize = 1 });

        Assert.Equal(2, total);
        Assert.Equal(1, items.Count);
        Assert.Equal(AlertRuleKind.ClusterOffline, Assert.Single(items).RuleKind);
    }

    [Fact]
    public async Task AddAsync_second_open_with_same_key_conflicts()
    {
        await _repo.AddAsync(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.ClusterOffline,
            OpenedAt = DateTime.UtcNow
        });

        // 同一集群同一规则再开一条 open:部分唯一索引拒绝。
        _db.AlertRecords.Add(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.ClusterOffline,
            OpenedAt = DateTime.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(
            () => _db.SaveChangesAsync(TestContext.Current.CancellationToken));
        // 失败的插入实体仍留在变更跟踪器中,清空后继续后续断言。
        _db.ChangeTracker.Clear();

        // 先解析再开立:部分索引只约束未解析行,允许再次开立。
        var open = await _repo.GetOpenAsync();
        await _repo.ResolveAsync(Assert.Single(open).Id, DateTime.UtcNow);
        await _repo.AddAsync(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.ClusterOffline,
            OpenedAt = DateTime.UtcNow
        });
        Assert.Equal(1, await _repo.CountOpenAsync());
    }

    [Fact]
    public async Task ResolveAsync_backfills_resolved_at_once()
    {
        var record = new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.NodeNotReady,
            OpenedAt = new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc)
        };
        await _repo.AddAsync(record);

        var resolvedAt = new DateTime(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);
        await _repo.ResolveAsync(record.Id, resolvedAt);
        await _repo.ResolveAsync(record.Id, resolvedAt.AddHours(1));

        var stored = Assert.Single(_db.AlertRecords.ToList());
        Assert.Equal(resolvedAt, stored.ResolvedAt);
    }

    [Fact]
    public async Task ResolveAsync_unknown_id_is_silent()
    {
        await _repo.ResolveAsync(999, DateTime.UtcNow);

        Assert.Equal(0, await _db.AlertRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Cluster_deletion_cascades_alerts()
    {
        await _repo.AddAsync(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.ClusterOffline,
            OpenedAt = DateTime.UtcNow
        });
        await _repo.AddAsync(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = AlertRuleKind.NodeNotReady,
            OpenedAt = DateTime.UtcNow,
            ResolvedAt = DateTime.UtcNow
        });

        var cluster = await _db.Clusters.SingleAsync(c => c.Id == _clusterId, TestContext.Current.CancellationToken);
        _db.Clusters.Remove(cluster);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, await _db.AlertRecords.CountAsync(TestContext.Current.CancellationToken));
    }

    private async Task SeedAsync(AlertRuleKind kind, double hoursAgo, DateTime? resolvedAt)
    {
        _db.AlertRecords.Add(new AlertRecord
        {
            ClusterId = _clusterId,
            RuleKind = kind,
            OpenedAt = DateTime.UtcNow.AddHours(hoursAgo),
            ResolvedAt = resolvedAt
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
