using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Time;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 全局集群看板服务:把集群主档、节点健康快照、审计日志与定时同步设置聚合为一份看板快照。
/// 只读本地持久化数据,不访问 Kubernetes API——集群全部离线时看板仍可正常展示,
/// 数据的实时性由后台定时同步保证,并由新鲜度判定向用户如实标注。
/// </summary>
public class DashboardService(
    IClusterRepository clusterRepo,
    IClusterHealthRepository healthRepo,
    AuditService auditService,
    ClusterSyncSettingService syncSettingService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<DashboardService> logger)
{
    private const string AdminRole = "Admin";

    private const int RecentActivityCount = 8;

    private const string UngroupedName = "未分组";

    private const string UnprobedVersionLabel = "未探测";

    /// <summary>
    /// 读取并聚合看板数据:状态计数、需要关注清单、分组健康、版本分布、舰队规模、数据新鲜度与最近操作。
    /// </summary>
    /// <returns>看板展示数据。</returns>
    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        logger.LogInformation("GetDashboard start");

        var clusters = await clusterRepo.GetAllForDashboardAsync();
        var snapshots = await healthRepo.GetLatestPerClusterAsync();
        var settings = await syncSettingService.GetClusterSyncSettingsAsync();
        var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole(AdminRole) == true;
        var recentActivity = await GetRecentActivityAsync(isAdmin);

        var model = new DashboardViewModel
        {
            TotalClusters = clusters.Count,
            OnlineClusters = clusters.Count(c => c.Status == ClusterStatus.Online),
            OfflineClusters = clusters.Count(c => c.Status == ClusterStatus.Offline),
            UnknownClusters = clusters.Count(c => c.Status == ClusterStatus.Unknown),
            AttentionItems = BuildAttentionItems(clusters),
            GroupHealth = BuildGroupHealth(clusters),
            VersionBuckets = BuildVersionBuckets(clusters),
            FleetSize = BuildFleetSize(clusters, snapshots),
            Freshness = BuildFreshness(clusters, settings),
            RecentActivity = recentActivity,
            RecentActivityIsSystemWide = isAdmin
        };

        logger.LogInformation("GetDashboard done clusters={ClusterCount} attention={AttentionCount} freshness={Freshness}",
            model.TotalClusters, model.AttentionItems.Count, model.Freshness.State);
        return model;
    }

    /// <summary>
    /// 构建「需要关注」清单:状态为离线的集群(最近一次探测失败)与从未被探测过的集群。
    /// 离线项排在从未探测项之前,同类内按集群名称排序。
    /// </summary>
    /// <param name="clusters">全部集群。</param>
    /// <returns>需要关注的集群清单;无命中时为空列表。</returns>
    private static List<DashboardAttentionItemViewModel> BuildAttentionItems(IReadOnlyList<ClusterInfo> clusters)
    {
        return [.. clusters
            .Where(c => c.Status == ClusterStatus.Offline || c.LastCheckedAt is null)
            .Select(c =>
            {
                var kind = c.Status == ClusterStatus.Offline
                    ? DashboardAttentionKind.Offline
                    : DashboardAttentionKind.NeverProbed;
                return new DashboardAttentionItemViewModel
                {
                    ClusterId = c.Id,
                    ClusterName = c.Name,
                    Kind = kind,
                    KindText = kind.ToDisplayText(),
                    BadgeCssClass = kind.ToBadgeCssClass(),
                    LastCheckedAt = c.LastCheckedAt is null ? null : AsUtc(c.LastCheckedAt.Value)
                };
            })
            .OrderBy(i => i.Kind)
            .ThenBy(i => i.ClusterName, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>构建分组健康:按分组统计成员数与其中在线数,未分组集群作为独立一项并排在最后。</summary>
    /// <param name="clusters">全部集群(需已加载分组)。</param>
    /// <returns>分组健康列表。</returns>
    private static List<DashboardGroupHealthViewModel> BuildGroupHealth(IReadOnlyList<ClusterInfo> clusters)
    {
        return [.. clusters
            .GroupBy(c => c.Group?.Name ?? UngroupedName)
            .Select(g => new DashboardGroupHealthViewModel
            {
                GroupName = g.Key,
                TotalClusters = g.Count(),
                OnlineClusters = g.Count(c => c.Status == ClusterStatus.Online)
            })
            .OrderBy(g => g.GroupName == UngroupedName ? 1 : 0)
            .ThenBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>构建版本分布:按版本分桶计数,未探测到版本的集群单独成桶并排在最后,其余按版本号倒序。</summary>
    /// <param name="clusters">全部集群。</param>
    /// <returns>版本分布桶列表。</returns>
    private static List<DashboardVersionBucketViewModel> BuildVersionBuckets(IReadOnlyList<ClusterInfo> clusters)
    {
        return [.. clusters
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Version) ? null : c.Version)
            .Select(g => new DashboardVersionBucketViewModel
            {
                Version = g.Key,
                Label = g.Key ?? UnprobedVersionLabel,
                Count = g.Count()
            })
            .OrderBy(b => b.Version is null ? 1 : 0)
            .ThenByDescending(b => b.Version, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// 构建舰队规模与就绪统计:取每个集群最近一条健康快照的节点总数、就绪数与未就绪数之和。
    /// 集群离线时其主档节点数已被降级为 0,这里改用快照中的最后已知值,使规模不凭空缩水;
    /// 从未成功探测过的集群不参与统计,其数量单独报出。
    /// </summary>
    /// <param name="clusters">全部集群。</param>
    /// <param name="snapshots">集群 Id 到其最近一条健康快照的映射。</param>
    /// <returns>舰队规模与就绪展示数据。</returns>
    private static DashboardFleetSizeViewModel BuildFleetSize(
        IReadOnlyList<ClusterInfo> clusters,
        IReadOnlyDictionary<int, ClusterHealthSnapshot> snapshots)
    {
        var contributing = clusters
            .Where(c => snapshots.ContainsKey(c.Id))
            .Select(c => (Cluster: c, Snapshot: snapshots[c.Id]))
            .ToList();

        return new DashboardFleetSizeViewModel
        {
            NodeCount = contributing.Sum(x => x.Snapshot.TotalNodes),
            ReadyNodes = contributing.Sum(x => x.Snapshot.ReadyNodes),
            NotReadyNodes = contributing.Sum(x => x.Snapshot.NotReadyNodes),
            OldestCapturedAt = contributing.Count == 0 ? null : AsUtc(contributing.Min(x => x.Snapshot.CapturedAt)),
            IncludesStaleData = contributing.Any(x => x.Cluster.Status != ClusterStatus.Online),
            ClustersWithoutSnapshot = clusters.Count - contributing.Count
        };
    }

    /// <summary>
    /// 构建数据新鲜度:最近同步时间取全体集群最近检测时间的最大值;
    /// 以生效间隔的两倍为过期阈值,同步停用与从未同步分别以专门状态表达,避免误报为过期。
    /// </summary>
    /// <param name="clusters">全部集群。</param>
    /// <param name="settings">生效的定时同步设置。</param>
    /// <returns>新鲜度展示数据。</returns>
    private static DashboardFreshnessViewModel BuildFreshness(
        IReadOnlyList<ClusterInfo> clusters,
        ClusterSyncSettingsViewModel settings)
    {
        DateTime? lastSyncedAt = null;
        foreach (var cluster in clusters)
        {
            if (cluster.LastCheckedAt is { } checkedAt && (lastSyncedAt is null || checkedAt > lastSyncedAt))
            {
                lastSyncedAt = checkedAt;
            }
        }

        var normalized = lastSyncedAt is null ? (DateTime?)null : AsUtc(lastSyncedAt.Value);
        var isStale = normalized is not null
            && DateTime.UtcNow - normalized.Value > TimeSpan.FromMinutes(settings.IntervalMinutes * 2.0);

        var state = normalized is null
            ? DashboardFreshnessState.NeverSynced
            : !settings.Enabled
                ? DashboardFreshnessState.SyncDisabled
                : isStale
                    ? DashboardFreshnessState.Stale
                    : DashboardFreshnessState.Fresh;

        return new DashboardFreshnessViewModel
        {
            LastSyncedAt = normalized,
            LastSyncedRelativeText = RelativeTimeFormatter.Format(normalized),
            State = state,
            SyncIntervalMinutes = settings.IntervalMinutes
        };
    }

    /// <summary>
    /// 读取最近操作:Admin 可见全站记录,非 Admin 仅可见本人记录——
    /// 复用审计服务既有的可见范围实现,看板不放宽该范围。
    /// </summary>
    /// <param name="isAdmin">当前用户是否为 Admin,决定可见范围。</param>
    /// <returns>最近操作清单。</returns>
    private async Task<List<DashboardActivityViewModel>> GetRecentActivityAsync(bool isAdmin)
    {
        var logs = isAdmin
            ? (await auditService.GetPagedAsync(new AuditLogQueryRequest { Page = 1, PageSize = RecentActivityCount })).Items
            : await auditService.GetRecentAsync(RecentActivityCount);

        return [.. logs.Select(l => new DashboardActivityViewModel
        {
            CreatedAt = AsUtc(l.CreatedAt),
            UserName = l.UserName,
            CategoryName = l.CategoryName,
            ActionName = l.ActionName,
            Target = l.Target
        })];
    }

    /// <summary>
    /// 将库中读出的时间按 UTC 解释:写入时一律为 UTC,而 SQLite 读回的时间 Kind 为 Unspecified,
    /// 若不归一化会被当成本地时间,导致相对时间与过期判定整体偏移一个时区。
    /// </summary>
    /// <param name="value">待归一化的时间。</param>
    /// <returns>Kind 为 UTC 的同一时刻。</returns>
    private static DateTime AsUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
