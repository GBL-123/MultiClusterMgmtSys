using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 告警评估与查询服务:基于集群主档、节点健康快照与同步/告警设置评估三条规则,
/// 把异常固化为可追溯的告警记录(open → resolved,恢复也留痕)。
/// 纯读 SQLite,零 K8s 调用;评估不写审计(追溯以告警记录自身为准)。
/// </summary>
public class AlertService(
    IAlertRepository alertRepo,
    IClusterRepository clusterRepo,
    IClusterHealthRepository healthRepo,
    ClusterSyncSettingService syncSettingService,
    AlertSettingService alertSettingService,
    ILogger<AlertService> logger)
{
    /// <summary>
    /// 执行一轮评估:读取设置与本地数据,由三条规则求出「应开集合」,与当前 open 记录 diff——
    /// 新命中则开立(开立时间取当前时刻),不再命中则解析(回填解析时间)。
    /// 三条规则独立评估、不做跨规则抑制;评估不发起 K8s 调用、不写审计日志。
    /// </summary>
    /// <returns>(本轮开立的告警条数, 解析的告警条数)。</returns>
    public async Task<(int Opened, int Resolved)> EvaluateAsync()
    {
        logger.LogInformation("AlertEvaluate start");
        var now = DateTime.UtcNow;

        var syncSettings = await syncSettingService.GetClusterSyncSettingsAsync();
        var alertSettings = await alertSettingService.GetAlertSettingsAsync();
        var clusters = await clusterRepo.GetAllForDashboardAsync();
        var snapshots = await healthRepo.GetLatestPerClusterAsync();
        var openAlerts = await alertRepo.GetOpenAsync();

        var desired = CollectDesiredAlerts(clusters, snapshots, syncSettings.Enabled, syncSettings.IntervalMinutes,
            alertSettings.OfflineThresholdMinutes, now);

        var openKeys = openAlerts
            .Select(a => (a.ClusterId, a.RuleKind))
            .ToHashSet();

        var opened = 0;
        foreach (var (clusterId, ruleKind, detail) in desired)
        {
            if (openKeys.Contains((clusterId, ruleKind)))
            {
                continue;
            }

            await alertRepo.AddAsync(new MultiClusterMgmtSys.Domain.Entities.AlertRecord
            {
                ClusterId = clusterId,
                RuleKind = ruleKind,
                Detail = detail,
                OpenedAt = now
            });
            opened++;
        }

        var desiredKeys = desired
            .Select(d => (d.ClusterId, d.RuleKind))
            .ToHashSet();

        var resolved = 0;
        foreach (var alert in openAlerts)
        {
            if (desiredKeys.Contains((alert.ClusterId, alert.RuleKind)))
            {
                continue;
            }

            await alertRepo.ResolveAsync(alert.Id, now);
            resolved++;
        }

        logger.LogInformation("AlertEvaluate done opened={Opened} resolved={Resolved}", opened, resolved);
        return (opened, resolved);
    }

    /// <summary>统计当前 open 告警总数,供 AppBar 铃铛角标展示;无副作用。</summary>
    /// <returns>open 告警条数。</returns>
    public async Task<int> GetOpenCountAsync()
    {
        var count = await alertRepo.CountOpenAsync();
        logger.LogInformation("GetOpenAlertCount count={Count}", count);
        return count;
    }

    /// <summary>按状态过滤分页查询告警列表;集群名经集群主档 join(不存名称快照,改名不漂移),规则名转中文。</summary>
    /// <param name="request">状态过滤与分页条件。</param>
    /// <returns>当页告警列表,以及过滤后(分页前)的命中总数。</returns>
    public async Task<PagedResult<AlertListItemViewModel>> GetAlertsAsync(AlertListRequest request)
    {
        logger.LogInformation("GetAlerts page={Page} size={PageSize} resolved={Resolved}",
            request.Page, request.PageSize, request.Resolved);

        var clusters = await clusterRepo.GetAllForDashboardAsync();
        var clusterNames = clusters.ToDictionary(c => c.Id, c => c.Name);

        var (items, total) = await alertRepo.GetPagedAsync(new AlertPageQuery
        {
            Resolved = request.Resolved,
            Page = request.Page,
            PageSize = request.PageSize
        });

        var viewItems = items
            .Select(a => new AlertListItemViewModel
            {
                Id = a.Id,
                ClusterId = a.ClusterId,
                ClusterName = clusterNames.GetValueOrDefault(a.ClusterId, $"集群 {a.ClusterId}"),
                RuleKind = a.RuleKind,
                RuleText = a.RuleKind.ToChineseText(),
                Detail = a.Detail,
                OpenedAt = a.OpenedAt,
                ResolvedAt = a.ResolvedAt
            })
            .ToList();

        logger.LogInformation("GetAlerts returned {Count} of {Total}", viewItems.Count, total);
        return new PagedResult<AlertListItemViewModel>(viewItems, total);
    }

    /// <summary>
    /// 由三条规则求出「应开集合」:集群离线(离线状态持续超过离线持续时间阈值,锚最近快照采集时间)、
    /// 节点未就绪(最近快照存在未就绪节点,补充信息含数量)、快照断流(同步启用且最近探测尝试时间超过生效间隔 × 2)。
    /// 从未成功探测(无快照)不给规则一提供锚点,LastCheckedAt 为空不给规则三提供基线,均不开立。
    /// </summary>
    /// <param name="clusters">全部集群。</param>
    /// <param name="snapshots">集群 Id 到该集群最新一条健康快照的映射。</param>
    /// <param name="syncEnabled">定时同步是否启用。</param>
    /// <param name="syncIntervalMinutes">生效同步间隔(分钟)。</param>
    /// <param name="offlineThresholdMinutes">离线持续阈值(分钟)。</param>
    /// <param name="now">评估基准时刻(UTC)。</param>
    /// <returns>应开告警的(集群 Id, 规则类别, 补充信息)集合;补充信息仅规则二填写。</returns>
    private static List<(int ClusterId, AlertRuleKind RuleKind, string? Detail)> CollectDesiredAlerts(
        IReadOnlyList<MultiClusterMgmtSys.Domain.Entities.ClusterInfo> clusters,
        Dictionary<int, MultiClusterMgmtSys.Domain.Entities.ClusterHealthSnapshot> snapshots,
        bool syncEnabled,
        int syncIntervalMinutes,
        int offlineThresholdMinutes,
        DateTime now)
    {
        var desired = new List<(int, AlertRuleKind, string?)>();
        foreach (var cluster in clusters)
        {
            if (snapshots.TryGetValue(cluster.Id, out var snapshot))
            {
                var silentFor = now - snapshot.CapturedAt;
                if (cluster.Status == ClusterStatus.Offline
                    && silentFor > TimeSpan.FromMinutes(offlineThresholdMinutes))
                {
                    desired.Add((cluster.Id, AlertRuleKind.ClusterOffline, null));
                }

                if (snapshot.NotReadyNodes > 0)
                {
                    desired.Add((cluster.Id, AlertRuleKind.NodeNotReady, $"未就绪节点 {snapshot.NotReadyNodes} 个"));
                }
            }

            if (syncEnabled
                && cluster.LastCheckedAt is not null
                && now - cluster.LastCheckedAt.Value > TimeSpan.FromMinutes(syncIntervalMinutes * 2))
            {
                desired.Add((cluster.Id, AlertRuleKind.SnapshotStalled, null));
            }
        }

        return desired;
    }
}
