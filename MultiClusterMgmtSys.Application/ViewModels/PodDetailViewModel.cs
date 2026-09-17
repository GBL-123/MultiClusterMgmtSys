using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Pod 详情页展示数据:由 core/v1 Pod 映射,含基本信息、条件与容器状态。
/// </summary>
public class PodDetailViewModel
{
    /// <summary>Pod 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>所在节点名(spec.nodeName)。</summary>
    public string NodeName { get; set; } = "";

    /// <summary>Pod IP(podIP);未分配时为空。</summary>
    public string PodIp { get; set; } = "";

    /// <summary>Pod phase 原始值。</summary>
    public string Phase { get; set; } = "";

    /// <summary>状态徽章中文主行:容器异常原因优先,否则 phase 中文;判定输入取容器状态行(由映射层填充)。</summary>
    public string StatusText => K8sDisplayText.PodStatusText(Phase, ContainerReason);

    /// <summary>状态徽章英文次行(raw):容器异常原因优先,否则 phase 原文。</summary>
    public string StatusRaw => K8sDisplayText.PodStatusRaw(Phase, ContainerReason);

    /// <summary>状态徽章 CSS 类:容器异常原因 offline;否则 Running online、Failed offline、其余 unknown。</summary>
    public string StatusCssClass => K8sDisplayText.PodStatusCssClass(Phase, ContainerReason);

    /// <summary>容器级异常信号(与列表同一口径,由映射层在详情映射时解析);无信号为空。</summary>
    public string? ContainerReason { get; set; }

    /// <summary>QoS 类原始值(Guaranteed/Burstable/BestEffort);未上报为空。</summary>
    public string QosClass { get; set; } = "";

    /// <summary>QoS 类中文展示名;未登记值回退原文。</summary>
    public string QosClassText => K8sDisplayText.PodQosClassText(QosClass);

    /// <summary>启动时间(status.startTime);null 表示未启动。</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>启动时间绝对文本(yyyy-MM-dd HH:mm:ss);无时间显示占位符 —。</summary>
    public string StartedAtAbsoluteText => StartedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

    /// <summary>Pod 条件列表(Ready 等的类型/状态/最近迁移时间)。</summary>
    public List<PodConditionViewModel> Conditions { get; set; } = [];

    /// <summary>容器状态列表(常规容器在前,与 K8s 返回顺序一致;init 容器追加其后)。</summary>
    public List<PodContainerViewModel> Containers { get; set; } = [];
}
