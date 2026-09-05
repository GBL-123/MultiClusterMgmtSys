namespace MultiClusterMgmtSys.Common.Enums;

public enum ClusterStatus
{
    Unknown,
    Online,
    Offline
}

public static class ClusterStatusText
{
    public static string ToChineseText(this ClusterStatus status) => status switch
    {
        ClusterStatus.Online => "在线",
        ClusterStatus.Offline => "离线",
        _ => "未知"
    };
}
