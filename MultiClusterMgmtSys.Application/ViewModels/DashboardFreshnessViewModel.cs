using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板数据新鲜度:最近一次同步时间、相对时间描述与过期判定,判定基准为生效的定时同步间隔。
/// </summary>
public class DashboardFreshnessViewModel
{
    /// <summary>最近一次同步时间(UTC,取全体集群最近检测时间的最大值);从未同步时为空。</summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>最近一次同步的相对时间描述(刚刚/N 分钟前/N 小时前/N 天前);从未同步时为占位符。</summary>
    public string LastSyncedRelativeText { get; set; } = "";

    /// <summary>新鲜度状态(正常/过期/同步停用/从未同步)。</summary>
    public DashboardFreshnessState State { get; set; }

    /// <summary>当前生效的定时同步间隔(分钟),用于向用户说明过期判定的基准。</summary>
    public int SyncIntervalMinutes { get; set; }
}
