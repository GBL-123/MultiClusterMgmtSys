using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Components.Clusters.ViewModels;

/// <summary>
/// 集群端点展示 VM：详情页只读视图，不暴露任何编辑状态。
/// KindText 由 mapping 计算（"VIP" / "域名"）。
/// </summary>
public class ClusterEndpointViewModel
{
    public int Id { get; set; }

    public ClusterEndpointKind Kind { get; set; }

    public string KindText { get; set; } = "";

    public string Value { get; set; } = "";

    public string? Note { get; set; }

    public int SortOrder { get; set; }
}
