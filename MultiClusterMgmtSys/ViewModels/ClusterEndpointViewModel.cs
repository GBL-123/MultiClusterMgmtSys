using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 集群端点展示 VM：详情页只读视图，不暴露任何编辑状态。
/// KindText 由 mapping 计算（"VIP" / "域名"）。
/// </summary>
public class ClusterEndpointViewModel
{
    /// <summary>端点记录主键。</summary>
    public int Id { get; set; }

    /// <summary>端点类型(VIP/域名)。</summary>
    public ClusterEndpointKind Kind { get; set; }

    /// <summary>端点类型的中文展示文本("VIP" 或 "域名"),由映射层计算。</summary>
    public string KindText { get; set; } = "";

    /// <summary>端点地址值(VIP 地址或域名)。</summary>
    public string Value { get; set; } = "";

    /// <summary>备注说明;未设置时为 null。</summary>
    public string? Note { get; set; }

    /// <summary>组内排序序号,升序展示。</summary>
    public int SortOrder { get; set; }
}
