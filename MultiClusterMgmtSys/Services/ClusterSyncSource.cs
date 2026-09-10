namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群状态刷新来源常量,写入审计文案用于区分手动刷新与后台定时同步两条触发路径。
/// </summary>
public static class ClusterSyncSource
{
    /// <summary>由用户在页面上手动触发。</summary>
    public const string Manual = "手动刷新";

    /// <summary>由后台定时任务自动触发。</summary>
    public const string Scheduled = "定时同步";
}
