namespace MultiClusterMgmtSys.Application.Enums;

/// <summary>
/// 看板「需要关注」项的类别:说明该集群为何需要人工介入。
/// </summary>
public enum DashboardAttentionKind
{
    /// <summary>集群不可达(最近一次探测失败,状态为离线)。</summary>
    Offline = 0,

    /// <summary>集群自登记以来从未被成功探测(从未同步,最近检测时间为空)。</summary>
    NeverProbed = 1
}

/// <summary>看板「需要关注」类别到展示文本与徽章样式的映射。</summary>
public static class DashboardAttentionKindExtensions
{
    /// <summary>类别的中文展示文本(离线/从未探测)。</summary>
    /// <param name="kind">需要关注类别。</param>
    /// <returns>中文展示文本。</returns>
    public static string ToDisplayText(this DashboardAttentionKind kind) => kind switch
    {
        DashboardAttentionKind.Offline => "离线",
        DashboardAttentionKind.NeverProbed => "从未探测",
        _ => kind.ToString()
    };

    /// <summary>类别对应的状态徽章样式类(离线→offline,从未探测→unknown);取值遵循 StatusBadge 的 CssClass 契约。</summary>
    /// <param name="kind">需要关注类别。</param>
    /// <returns>徽章样式类名。</returns>
    public static string ToBadgeCssClass(this DashboardAttentionKind kind) => kind switch
    {
        DashboardAttentionKind.Offline => "offline",
        DashboardAttentionKind.NeverProbed => "unknown",
        _ => "unknown"
    };
}
