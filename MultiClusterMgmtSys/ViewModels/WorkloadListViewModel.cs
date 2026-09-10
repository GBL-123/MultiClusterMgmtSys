using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 工作负载列表页展示数据,统一聚合 apps/v1 四型
/// (Deployment/StatefulSet/DaemonSet/ReplicaSet)的就绪度与滚动状态。
/// </summary>
public class WorkloadListViewModel
{
    /// <summary>资源名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>资源所在的 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>工作负载类型(<see cref="WorkloadKind"/>)。</summary>
    public WorkloadKind Kind { get; set; }

    /// <summary>当前就绪副本数(DaemonSet 为已就绪节点数)。</summary>
    public int ReadyCount { get; set; }

    /// <summary>期望副本数(DaemonSet 为应调度节点数)。</summary>
    public int DesiredCount { get; set; }

    /// <summary>就绪度展示文本,格式"就绪/期望",如 2/3。</summary>
    public string ReadyText => $"{ReadyCount}/{DesiredCount}";

    /// <summary>滚动三态,列表页状态徽标依据。</summary>
    public WorkloadRolloutState RolloutState { get; set; } = WorkloadRolloutState.NotReady;

    /// <summary>资源创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
