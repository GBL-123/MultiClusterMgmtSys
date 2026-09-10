namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 集群可达状态(由后台同步探测得出)。
/// </summary>
public enum ClusterStatus
{
    /// <summary>未探测过或探测结果不可判定。</summary>
    Unknown,

    /// <summary>在线可达。</summary>
    Online,

    /// <summary>探测失败/不可达。</summary>
    Offline
}

/// <summary>
/// 集群状态的中文显示文案扩展。
/// </summary>
public static class ClusterStatusText
{
    /// <summary>将集群状态转为中文显示文案(在线/离线/未知)。</summary>
    public static string ToChineseText(this ClusterStatus status) => status switch
    {
        ClusterStatus.Online => "在线",
        ClusterStatus.Offline => "离线",
        _ => "未知"
    };
}
