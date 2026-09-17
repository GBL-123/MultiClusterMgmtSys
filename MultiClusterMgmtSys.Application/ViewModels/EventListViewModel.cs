using MultiClusterMgmtSys.Application.Common.Time;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 事件管理页展示数据:由 core/v1 Event 映射,含类型/原因/关联对象/消息/次数/时间与来源。
/// </summary>
public class EventListViewModel
{
    /// <summary>事件类型原始值(Normal/Warning),筛选与徽章配色依据。</summary>
    public string Type { get; set; } = "";

    /// <summary>事件类型中文展示名(正常/警告);未登记值回退原文。</summary>
    public string TypeText => K8sDisplayText.EventTypeText(Type);

    /// <summary>事件类型对应的状态徽章 CSS 类(normal/warning);未登记回退 unknown。</summary>
    public string TypeCssClass => K8sDisplayText.EventTypeCssClass(Type);

    /// <summary>事件原因(如 BackOff/FailedScheduling),保持 K8s 原文。</summary>
    public string Reason { get; set; } = "";

    /// <summary>事件记录所在命名空间(metadata.namespace),命名空间筛选依据。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>关联对象 kind(如 Pod/Node/Deployment)。</summary>
    public string InvolvedKind { get; set; } = "";

    /// <summary>关联对象命名空间;集群级对象(如 Node)为空。</summary>
    public string InvolvedNamespace { get; set; } = "";

    /// <summary>关联对象名称。</summary>
    public string InvolvedName { get; set; } = "";

    /// <summary>关联对象字段路径(如 spec.containers{app});无则为空。</summary>
    public string InvolvedFieldPath { get; set; } = "";

    /// <summary>关联对象详情路由;无对应详情页的 kind 为 null(纯文本展示)。</summary>
    public string? DetailRoute { get; set; }

    /// <summary>完整消息,保持 K8s 原文。</summary>
    public string Message { get; set; } = "";

    /// <summary>重复发生次数(K8s 聚合计数;缺失按 1 计)。</summary>
    public int Count { get; set; } = 1;

    /// <summary>次数展示文本:大于 1 时以 ×N 展示,否则为 1。</summary>
    public string CountText => Count > 1 ? $"×{Count}" : "1";

    /// <summary>最近发生时间(四级回退链解析结果);候选字段全空时为 null。</summary>
    public DateTime? OccurredAt { get; set; }

    /// <summary>最近发生相对时间主展示(如「3 分钟前」);无时间显示占位符 —。</summary>
    public string OccurredAtText => RelativeTimeFormatter.Format(OccurredAt);

    /// <summary>最近发生绝对时间(列表 tooltip 使用,格式 yyyy-MM-dd HH:mm:ss);无时间显示占位符 —。</summary>
    public string OccurredAtAbsoluteText => AbsoluteText(OccurredAt);

    /// <summary>首次发生时间;无则 null。</summary>
    public DateTime? FirstOccurredAt { get; set; }

    /// <summary>首次发生绝对时间(详情对话框使用,格式 yyyy-MM-dd HH:mm:ss);无时间显示占位符 —。</summary>
    public string FirstOccurredAtText => AbsoluteText(FirstOccurredAt);

    /// <summary>来源组件(Source.Component,如 kubelet/default-scheduler);无则为空。</summary>
    public string SourceComponent { get; set; } = "";

    /// <summary>来源主机(Source.Host);无则为空。</summary>
    public string SourceHost { get; set; } = "";

    private static string AbsoluteText(DateTime? value) => value?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
}
