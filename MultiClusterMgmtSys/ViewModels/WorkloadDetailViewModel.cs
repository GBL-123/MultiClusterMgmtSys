using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 工作负载详情页条件(Condition)条目展示数据。
/// </summary>
public class WorkloadConditionViewModel
{
    /// <summary>条件类型,如 Available、Progressing。</summary>
    public string Type { get; set; } = "";

    /// <summary>条件状态,取值 True/False/Unknown。</summary>
    public string Status { get; set; } = "";

    /// <summary>触发该状态的机器可读原因;API 未返回时为空字符串。</summary>
    public string Reason { get; set; } = "";

    /// <summary>人类可读的详细说明;API 未返回时为空字符串。</summary>
    public string Message { get; set; } = "";

    /// <summary>最近一次状态切换时间;null 表示 API 未返回。</summary>
    public DateTime? LastTransitionAt { get; set; } = null;
}

/// <summary>
/// 工作负载详情页展示数据,含就绪度、滚动三态、条件列表与原始 YAML。
/// </summary>
public class WorkloadDetailViewModel
{
    /// <summary>资源名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>资源所在的 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>API 对象唯一标识(Uid)。</summary>
    public string Uid { get; set; } = "";

    /// <summary>工作负载类型(<see cref="WorkloadKind"/>)。</summary>
    public WorkloadKind Kind { get; set; }

    /// <summary>滚动三态,详情页状态徽标依据。</summary>
    public WorkloadRolloutState RolloutState { get; set; } = WorkloadRolloutState.NotReady;

    /// <summary>期望副本数(DaemonSet 为应调度节点数)。</summary>
    public int DesiredCount { get; set; }

    /// <summary>当前就绪副本数(DaemonSet 为已就绪节点数)。</summary>
    public int ReadyCount { get; set; }

    /// <summary>已更新到新版本的副本数;ReplicaSet 不产生该计数,恒为 0。</summary>
    public int UpdatedCount { get; set; }

    /// <summary>就绪度展示文本,格式"就绪/期望",如 2/3。</summary>
    public string ReadyText => $"{ReadyCount}/{DesiredCount}";

    /// <summary>标签选择器展示文本,多项以逗号分隔;无选择器时为空字符串。</summary>
    public string Selector { get; set; } = "";

    /// <summary>条件列表;ReplicaSet 详情恒为空列表。</summary>
    public List<WorkloadConditionViewModel> Conditions { get; set; } = new();

    /// <summary>资源创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>资源原始 YAML 文本,用于详情页 YAML 视图。</summary>
    public string Yaml { get; set; } = "";
}
