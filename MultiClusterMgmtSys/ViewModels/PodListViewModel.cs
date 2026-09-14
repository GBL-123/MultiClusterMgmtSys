using MultiClusterMgmtSys.Common.Time;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// Pod 列表页展示数据:由 core/v1 Pod 映射的瘦模型,状态徽章按容器级信号优先语义提供。
/// </summary>
public class PodListViewModel
{
    /// <summary>所属集群 Id(数据库主键),详情路由生成依据。</summary>
    public int ClusterId { get; set; }

    /// <summary>Pod 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间,命名空间筛选依据。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>所在节点名(spec.nodeName),关键词筛选与调度定位依据。</summary>
    public string NodeName { get; set; } = "";

    /// <summary>Pod IP(podIP);未分配时为空。</summary>
    public string PodIp { get; set; } = "";

    /// <summary>Pod phase 原始值(Running/Pending/Succeeded/Failed/Unknown)。</summary>
    public string Phase { get; set; } = "";

    /// <summary>容器级异常信号(waiting/terminated 原因);无异常信号时为空,徽章语义的判定输入。</summary>
    public string? ContainerReason { get; set; }

    /// <summary>状态徽章中文主行:容器异常原因优先,否则 phase 中文;详情见 K8sDisplayText.PodStatusText。</summary>
    public string StatusText => K8sDisplayText.PodStatusText(Phase, ContainerReason);

    /// <summary>状态徽章英文次行(raw):容器异常原因优先,否则 phase 原文,排序与筛选依据。</summary>
    public string StatusRaw => K8sDisplayText.PodStatusRaw(Phase, ContainerReason);

    /// <summary>状态徽章 CSS 类:容器异常原因 offline;否则 Running online、Failed offline、其余 unknown。</summary>
    public string StatusCssClass => K8sDisplayText.PodStatusCssClass(Phase, ContainerReason);

    /// <summary>重启次数(常规容器 RestartCount 之和);独立展示,不参与徽章语义。</summary>
    public int Restarts { get; set; }

    /// <summary>状态分类(内存过滤与计数用):容器异常信号归「异常」,其余按 phase 归类。</summary>
    public string StatusGroup => ContainerReason is not null
        ? "异常"
        : Phase switch
        {
            "Running" => "运行中",
            "Pending" => "等待中",
            "Succeeded" => "已完成",
            "Failed" => "失败",
            _ => "未知"
        };

    /// <summary>启动时间(status.startTime);null 表示未启动。</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>启动时间相对展示(如「3 小时前」);无时间显示占位符 —。</summary>
    public string StartedAtText => RelativeTimeFormatter.Format(StartedAt);

    /// <summary>启动时间绝对文本(tooltip 使用,yyyy-MM-dd HH:mm:ss);无时间显示占位符 —。</summary>
    public string StartedAtAbsoluteTooltip => StartedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

    /// <summary>Pod 详情页路由(命名空间与名称齐备时);供列表行点击进入详情。</summary>
    public string DetailRoute => $"/pods/{ClusterId}/{Namespace}/{Name}";
}
