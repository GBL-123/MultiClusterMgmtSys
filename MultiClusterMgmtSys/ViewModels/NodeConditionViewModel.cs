namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点状况(Condition)条目展示数据。
/// </summary>
public class NodeConditionViewModel
{
    /// <summary>状况类型,如 Ready/MemoryPressure/DiskPressure。</summary>
    public string Type { get; set; } = "";

    /// <summary>状况状态,取值 True/False/Unknown。</summary>
    public string Status { get; set; } = "";

    /// <summary>触发该状态的机器可读原因;null 表示 API 未返回。</summary>
    public string? Reason { get; set; }

    /// <summary>人类可读的详细说明;null 表示 API 未返回。</summary>
    public string? Message { get; set; }

    /// <summary>最近心跳时间;null 表示 API 未返回。</summary>
    public DateTime? LastHeartbeatTime { get; set; }

    /// <summary>最近状态切换时间;null 表示 API 未返回。</summary>
    public DateTime? LastTransitionTime { get; set; }
}
