using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Data;

public class ClusterRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext db = SqliteDbFactory.CreateContext();
    private readonly ClusterRepository repo;

    public ClusterRepositoryTests()
    {
        repo = new ClusterRepository(db);
    }

    public void Dispose() => db.Dispose();

    private async Task<int> SeedClusterAsync(
        string name,
        int? groupId = null,
        ClusterStatus status = ClusterStatus.Online,
        string? version = "1.29.0",
        int nodeCount = 3,
        DateTime? createdAt = null)
    {
        var added = await repo.AddAsync(TestData.NewCluster(
            name, groupId, status, version, nodeCount, createdAt));
        return added.Id;
    }

    [Fact]
    public async Task GetByIdAsync_includes_group_endpoints_and_remarks()
    {
        var group = db.ClusterGroups.Add(TestData.NewGroup()).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cluster = db.Clusters.Add(TestData.NewCluster("full", groupId: group.Id)).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ClusterEndpoints.Add(TestData.NewEndpoint(cluster.Id));
        db.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var loaded = await repo.GetByIdAsync(cluster.Id);

        Assert.NotNull(loaded);
        Assert.Equal("group-1", loaded!.Group!.Name);
        Assert.Single(loaded.Endpoints);
        Assert.Single(loaded.NodeIpRemarks);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_missing_id()
    {
        Assert.Null(await repo.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetPagedAsync_null_groupid_returns_all()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        var g2 = db.ClusterGroups.Add(TestData.NewGroup("g2")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedClusterAsync("a", groupId: null);
        await SeedClusterAsync("b", groupId: g1.Id);
        await SeedClusterAsync("c", groupId: g2.Id);

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery());

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetPagedAsync_groupid_zero_returns_only_ungrouped()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedClusterAsync("grouped", groupId: g1.Id);
        await SeedClusterAsync("loose", groupId: null);

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery { GroupId = 0 });

        Assert.Equal(1, total);
        Assert.Equal("loose", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_positive_groupid_filters_equality()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        var g2 = db.ClusterGroups.Add(TestData.NewGroup("g2")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedClusterAsync("g1", groupId: g1.Id);
        await SeedClusterAsync("g2", groupId: g2.Id);
        await SeedClusterAsync("loose", groupId: null);

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery { GroupId = g2.Id });

        Assert.Equal(1, total);
        Assert.Equal("g2", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_name_contains_matches_substring()
    {
        await SeedClusterAsync("prod-cluster");
        await SeedClusterAsync("dev-cluster");

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery { NameContains = "prod" });

        Assert.Equal(1, total);
        Assert.Equal("prod-cluster", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_status_filter()
    {
        await SeedClusterAsync("online", status: ClusterStatus.Online);
        await SeedClusterAsync("offline", status: ClusterStatus.Offline);

        var (items, total) = await repo.GetPagedAsync(
            new ClusterPageQuery { Status = ClusterStatus.Offline });

        Assert.Equal(1, total);
        Assert.Equal("offline", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_only_null_version_sentinel()
    {
        await SeedClusterAsync("v129", version: "1.29.0");
        await SeedClusterAsync("noVersion", version: null);

        var (items, total) = await repo.GetPagedAsync(
            new ClusterPageQuery { Version = VersionFilterSentinel.OnlyNull });

        Assert.Equal(1, total);
        Assert.Equal("noVersion", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_empty_version_sentinel_matches_empty_string_exact()
    {
        await SeedClusterAsync("v129", version: "1.29.0");
        await SeedClusterAsync("blank", version: "");
        await SeedClusterAsync("noVersion", version: null);

        var (items, total) = await repo.GetPagedAsync(
            new ClusterPageQuery { Version = VersionFilterSentinel.All });

        Assert.Equal(1, total);
        Assert.Equal("blank", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_exact_version_filter()
    {
        await SeedClusterAsync("v129", version: "1.29.0");
        await SeedClusterAsync("v130", version: "1.30.0");

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery { Version = "1.30.0" });

        Assert.Equal(1, total);
        Assert.Equal("v130", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_created_date_range_includes_full_day()
    {
        await SeedClusterAsync("in-range", createdAt: new DateTime(2026, 3, 5, 23, 0, 0, DateTimeKind.Utc));
        await SeedClusterAsync("before", createdAt: new DateTime(2026, 3, 4, 0, 0, 0, DateTimeKind.Utc));
        await SeedClusterAsync("after", createdAt: new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc));

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery
        {
            CreatedAfter = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
            CreatedBefore = new DateTime(2026, 3, 6, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Equal(1, total);
        Assert.Equal("in-range", items.Single().Name);
    }

    [Fact]
    public async Task GetPagedAsync_sorts_by_name_ascending_with_stable_id_tiebreak()
    {
        var idC = await SeedClusterAsync("c", createdAt: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        var idA = await SeedClusterAsync("a", createdAt: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        var idB = await SeedClusterAsync("b", createdAt: new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var (items, _) = await repo.GetPagedAsync(new ClusterPageQuery { SortBy = ClusterSortField.Name, SortDescending = false });

        Assert.Equal([idA, idB, idC], items.Select(c => c.Id).ToArray());
    }

    [Fact]
    public async Task GetPagedAsync_sorts_by_node_count_descending()
    {
        await SeedClusterAsync("small", nodeCount: 1);
        await SeedClusterAsync("big", nodeCount: 9);

        var (items, _) = await repo.GetPagedAsync(new ClusterPageQuery { SortBy = ClusterSortField.NodeCount });

        Assert.Equal("big", items.First().Name);
    }

    [Fact]
    public async Task GetPagedAsync_paging_returns_correct_slice_and_total()
    {
        for (var i = 1; i <= 5; i++)
        {
            await SeedClusterAsync($"cluster-{i}", createdAt: new DateTime(2026, 1, i, 0, 0, 0, DateTimeKind.Utc));
        }

        var (page1, total) = await repo.GetPagedAsync(new ClusterPageQuery { Page = 1, PageSize = 2 });
        var (page2, _) = await repo.GetPagedAsync(new ClusterPageQuery { Page = 2, PageSize = 2 });

        Assert.Equal(5, total);
        Assert.Equal(2, page1.Count);
        Assert.Equal(2, page2.Count);
        Assert.DoesNotContain(page2, c => page1.Any(p => p.Id == c.Id));
    }

    [Fact]
    public async Task GetPagedAsync_zero_page_and_size_clamp_to_minimum()
    {
        await SeedClusterAsync("only");

        var (items, total) = await repo.GetPagedAsync(new ClusterPageQuery { Page = 0, PageSize = 0 });

        Assert.Equal(1, total);
        Assert.Single(items);
    }

    [Fact]
    public async Task GetDistinctVersionsAsync_returns_sorted_distinct()
    {
        await SeedClusterAsync("a", version: "1.30.0");
        await SeedClusterAsync("b", version: "1.29.0");
        await SeedClusterAsync("c", version: "1.29.0");
        await SeedClusterAsync("d", version: null);

        var versions = await repo.GetDistinctVersionsAsync();

        Assert.Equal(["1.29.0", "1.30.0"], versions);
    }

    [Fact]
    public async Task SetGroupIdForClustersAsync_moves_selected_clusters()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        var g2 = db.ClusterGroups.Add(TestData.NewGroup("g2")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var id1 = await SeedClusterAsync("one", groupId: g1.Id);
        var id2 = await SeedClusterAsync("two", groupId: g1.Id);

        var moved = await repo.SetGroupIdForClustersAsync([id1, id2], g2.Id);

        Assert.Equal(2, moved);
        var moved1 = await db.Clusters.AsNoTracking().SingleAsync(c => c.Id == id1);
        Assert.Equal(g2.Id, moved1.GroupId);
    }

    [Fact]
    public async Task SetGroupIdForClustersAsync_empty_ids_is_noop()
    {
        Assert.Equal(0, await repo.SetGroupIdForClustersAsync([], 3));
    }

    [Fact]
    public async Task CountUngroupedAsync_counts_null_groupid_only()
    {
        var g1 = db.ClusterGroups.Add(TestData.NewGroup("g1")).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedClusterAsync("loose-1", groupId: null);
        await SeedClusterAsync("loose-2", groupId: null);
        await SeedClusterAsync("grouped", groupId: g1.Id);

        Assert.Equal(2, await repo.CountUngroupedAsync());
    }

    [Fact]
    public async Task GetAllIdsAsync_returns_all_ids()
    {
        var id1 = await SeedClusterAsync("x");
        var id2 = await SeedClusterAsync("y");

        var ids = await repo.GetAllIdsAsync();

        Assert.Equal([id1, id2], [.. ids.OrderBy(i => i)]);
    }

    [Fact]
    public async Task DeleteAsync_removes_cascade_children_and_nulls_group()
    {
        var group = db.ClusterGroups.Add(TestData.NewGroup()).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var cluster = db.Clusters.Add(TestData.NewCluster("removable", groupId: group.Id)).Entity;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ClusterEndpoints.Add(TestData.NewEndpoint(cluster.Id));
        db.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repo.DeleteAsync(cluster.Id);

        Assert.Null(await repo.GetByIdAsync(cluster.Id));
        Assert.Empty(db.ClusterEndpoints.Where(e => e.ClusterId == cluster.Id).ToList());
        Assert.Empty(db.NodeIpRemarks.Where(r => r.ClusterId == cluster.Id).ToList());
    }
}
