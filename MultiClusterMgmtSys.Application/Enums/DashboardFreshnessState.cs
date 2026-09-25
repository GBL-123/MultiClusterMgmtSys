namespace MultiClusterMgmtSys.Application.Enums;

/// <summary>
/// 看板数据新鲜度状态:以生效的定时同步间隔为基准,判定展示的数字是否仍然可信。
/// </summary>
public enum DashboardFreshnessState
{
    /// <summary>最近一次同步在生效间隔的两倍以内,数据正常。</summary>
    Fresh = 0,

    /// <summary>最近一次同步距今超过生效间隔的两倍,后台同步可能已停止。</summary>
    Stale = 1,

    /// <summary>定时同步处于停用状态,数据陈旧属于预期而非故障。</summary>
    SyncDisabled = 2,

    /// <summary>从未有过任何同步记录(全部集群最近检测时间为空)。</summary>
    NeverSynced = 3
}
