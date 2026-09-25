namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板舰队节点规模:口径为每个集群最近一次成功探测到的节点总数之和。
/// 集群离线时其主档节点数会被降级为 0,但规模取快照中的最后已知值,因此不会凭空缩水。
/// </summary>
public class DashboardFleetSizeViewModel
{
    /// <summary>舰队节点总数(各集群最近一条健康快照的节点数之和)。</summary>
    public int NodeCount { get; set; }

    /// <summary>参与规模的快照中最早的采集时间(UTC);无任何快照时为空。</summary>
    public DateTime? OldestCapturedAt { get; set; }

    /// <summary>规模中是否包含非在线集群的最后已知值(即该数字早于最近一轮同步)。</summary>
    public bool IncludesStaleData { get; set; }

    /// <summary>尚无任何健康快照、因而未参与规模的集群数(从未成功探测过)。</summary>
    public int ClustersWithoutSnapshot { get; set; }
}
