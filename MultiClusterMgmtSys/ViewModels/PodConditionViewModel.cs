using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// Pod 单个条件(PodCondition)的展示数据。
/// </summary>
public class PodConditionViewModel
{
    /// <summary>条件类型原始值(Ready/Initialized/PodsScheduled 等)。</summary>
    public string Type { get; set; } = "";

    /// <summary>条件类型中文展示名;未登记值回退原文。</summary>
    public string TypeText => K8sDisplayText.PodConditionTypeText(Type);

    /// <summary>条件状态原始值(True/False/Unknown)。</summary>
    public string Status { get; set; } = "";

    /// <summary>条件状态中文展示名(成立/不成立/未知)。</summary>
    public string StatusText => K8sDisplayText.ConditionStatusText(Status);

    /// <summary>条件状态对应的状态徽章 CSS 类。</summary>
    public string StatusCssClass => K8sDisplayText.ConditionStatusCssClass(Type, Status);

    /// <summary>最近一次状态迁移时间;null 表示 API 未返回。</summary>
    public DateTime? LastTransitionAt { get; set; }

    /// <summary>最近迁移时间绝对文本(yyyy-MM-dd HH:mm:ss);无时间显示占位符 —。</summary>
    public string LastTransitionAtText => LastTransitionAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
}
