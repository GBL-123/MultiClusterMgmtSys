using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

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

    /// <summary>污点效果中文展示名(禁止调度/尽量不调度/驱逐)。</summary>
    public string EffectText => K8sDisplayText.TaintEffectText(Effect);
}
