using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板「需要关注」清单项:一个需要人工介入的集群。
/// </summary>
public class DashboardAttentionItemViewModel
{
    /// <summary>集群主键,用于跳转集群详情。</summary>
    public int ClusterId { get; set; }

    /// <summary>集群名称。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>需要关注的类别(离线/从未探测)。</summary>
    public DashboardAttentionKind Kind { get; set; }

    /// <summary>类别的中文展示文本,同时作为状态徽章主行。</summary>
    public string KindText { get; set; } = "";

    /// <summary>状态徽章样式类(online/offline/unknown),取值遵循 StatusBadge 的 CssClass 契约。</summary>
    public string BadgeCssClass { get; set; } = "";

    /// <summary>最近一次检测时间(UTC);从未探测时为空。</summary>
    public DateTime? LastCheckedAt { get; set; }
}
