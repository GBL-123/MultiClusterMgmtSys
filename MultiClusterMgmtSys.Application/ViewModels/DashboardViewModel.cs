namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 全局集群看板展示数据:跨集群的聚合快照。
/// 全部字段来自本地持久化数据,不含任何实时 Kubernetes 调用,因此集群全部离线时看板仍可展示。
/// </summary>
public class DashboardViewModel
{
    /// <summary>集群总数。</summary>
    public int TotalClusters { get; set; }

    /// <summary>状态为在线的集群数。</summary>
    public int OnlineClusters { get; set; }

    /// <summary>状态为离线的集群数。</summary>
    public int OfflineClusters { get; set; }

    /// <summary>状态未知(从未探测过)的集群数。</summary>
    public int UnknownClusters { get; set; }

    /// <summary>需要人工介入的集群清单(离线与从未探测)。</summary>
    public List<DashboardAttentionItemViewModel> AttentionItems { get; set; } = [];

    /// <summary>分组健康:每个分组的规模与在线数,未分组集群独立成项。</summary>
    public List<DashboardGroupHealthViewModel> GroupHealth { get; set; } = [];

    /// <summary>版本分布:按 Kubernetes 版本分桶的集群数,未探测版本单独成桶。</summary>
    public List<DashboardVersionBucketViewModel> VersionBuckets { get; set; } = [];

    /// <summary>舰队节点规模及其数据时间。</summary>
    public DashboardFleetSizeViewModel FleetSize { get; set; } = new();

    /// <summary>数据新鲜度与过期判定。</summary>
    public DashboardFreshnessViewModel Freshness { get; set; } = new();

    /// <summary>最近操作清单(Admin 为全站记录,非 Admin 仅本人记录)。</summary>
    public List<DashboardActivityViewModel> RecentActivity { get; set; } = [];

    /// <summary>最近操作是否为全站范围(Admin 为 true);为 false 时清单只含当前用户自己的记录。</summary>
    public bool RecentActivityIsSystemWide { get; set; }

    /// <summary>是否存在任何已登记集群;为 false 时页面展示空态而非零值统计。</summary>
    public bool HasClusters => TotalClusters > 0;
}
