namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点污点(Taint)条目展示数据。
/// </summary>
public class NodeTaintViewModel
{
    /// <summary>污点键。</summary>
    public string Key { get; set; } = "";

    /// <summary>污点值;无值污点为 null。</summary>
    public string? Value { get; set; }

    /// <summary>污点效果,如 NoSchedule/PreferNoSchedule/NoExecute。</summary>
    public string Effect { get; set; } = "";
}
