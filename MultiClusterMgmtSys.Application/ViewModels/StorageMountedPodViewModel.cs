using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 挂载某持久卷声明的 Pod 行(详情页内嵌卡)。
/// </summary>
public class StorageMountedPodViewModel
{
    /// <summary>Pod 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>Pod 原始 Phase(Registered 由既有 PodPhaseText 映射,未登记值原样保留)。</summary>
    public string Phase { get; set; } = "";

    /// <summary>Pod Phase 中文展示名(契约见 display-conventions)。</summary>
    public string PhaseText => K8sDisplayText.PodPhaseText(Phase);

    /// <summary>Pod Phase 对应的状态徽章 CSS 类。</summary>
    public string PhaseCssClass => K8sDisplayText.PodStatusCssClass(Phase, null);

    /// <summary>开始时间;null 表示未运行。</summary>
    public DateTime? StartedAt { get; set; } = null;
}
