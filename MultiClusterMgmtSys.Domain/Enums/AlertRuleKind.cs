namespace MultiClusterMgmtSys.Domain.Enums;

/// <summary>
/// 告警规则类别(告警中心 v1 的三条评估规则)。
/// </summary>
public enum AlertRuleKind
{
    /// <summary>集群离线:集群状态为离线且持续时间超过离线阈值。</summary>
    ClusterOffline,

    /// <summary>节点未就绪:最近一次健康快照存在未就绪节点。</summary>
    NodeNotReady,

    /// <summary>快照断流:最近探测尝试时间超过新鲜度判定阈值(生效同步间隔 × 2)。</summary>
    SnapshotStalled
}

/// <summary>
/// 告警规则的中文显示文案扩展。
/// </summary>
public static class AlertRuleText
{
    /// <summary>将告警规则类别转为中文显示文案(集群离线/节点未就绪/快照断流)。</summary>
    public static string ToChineseText(this AlertRuleKind kind) => kind switch
    {
        AlertRuleKind.ClusterOffline => "集群离线",
        AlertRuleKind.NodeNotReady => "节点未就绪",
        _ => "快照断流"
    };
}
