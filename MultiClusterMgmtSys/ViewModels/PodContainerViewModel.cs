using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// Pod 单个容器的状态展示数据(容器状态卡行):状态文本与徽章语义统一由映射层提供(容器信号优先)。
/// </summary>
public class PodContainerViewModel
{
    /// <summary>容器名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>是否就绪(containerStatuses.ready)。</summary>
    public bool Ready { get; set; }

    /// <summary>就绪徽章 CSS 类。</summary>
    public string ReadyCssClass => Ready ? "online" : "offline";

    /// <summary>重启次数(容器级)。</summary>
    public int RestartCount { get; set; }

    /// <summary>容器镜像。</summary>
    public string Image { get; set; } = "";

    /// <summary>当前容器状态原始值(Running/Waiting/Terminated)。</summary>
    public string State { get; set; } = "";

    /// <summary>当前 waiting 原始原因(waiting 状态时);其余状态为空。</summary>
    public string WaitingReason { get; set; } = "";

    /// <summary>terminated 原始原因(terminated 状态时,如 Completed/OOMKilled);其余状态为空。</summary>
    public string TerminatedReason { get; set; } = "";

    /// <summary>状态徽章英文次行(raw):异常原因优先,否则容器状态原文。</summary>
    public string StateRaw => K8sDisplayText.PodContainerStateRaw(State, WaitingReason, TerminatedReason);

    /// <summary>容器状态中文主行:waiting/terminated 时以原因中文为主文案;详情见 K8sDisplayText.PodContainerStateText。</summary>
    public string StateText => K8sDisplayText.PodContainerStateText(State, WaitingReason, TerminatedReason);

    /// <summary>状态徽章 CSS 类:运行中 online、异常原因 offline、初始化/已完成等中性 unknown。</summary>
    public string StateCssClass => K8sDisplayText.PodContainerStateCssClass(State, WaitingReason, TerminatedReason);

    /// <summary>最近一次终止原因原始值(lastState.terminated.reason);无记录为空。</summary>
    public string LastTerminatedReason { get; set; } = "";

    /// <summary>最近一次终止原因中文展示名;未登记值回退原文,无记录为空串。</summary>
    public string LastTerminatedReasonText
        => string.IsNullOrEmpty(LastTerminatedReason) ? "" : K8sDisplayText.PodContainerReasonText(LastTerminatedReason);

    /// <summary>最近一次终止时间;无记录为 null。</summary>
    public DateTime? LastTerminatedAt { get; set; }

    /// <summary>最近一次终止时间绝对文本(yyyy-MM-dd HH:mm:ss);无记录显示占位符 —。</summary>
    public string LastTerminatedAtText => LastTerminatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
}
