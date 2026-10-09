using MultiClusterMgmtSys.Domain.Enums;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 告警列表行展示数据:目标集群、命中规则(含中文文案)、补充信息与状态机时间戳。
/// </summary>
public class AlertListItemViewModel
{
    /// <summary>告警记录 Id。</summary>
    public int Id { get; set; }

    /// <summary>目标集群 Id。</summary>
    public int ClusterId { get; set; }

    /// <summary>目标集群名称(经集群主档 join,集群改名后展示不漂移)。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>命中的告警规则类别(原始枚举,供过滤/排序)。</summary>
    public AlertRuleKind RuleKind { get; set; }

    /// <summary>规则中文名称(集群离线/节点未就绪/快照断流)。</summary>
    public string RuleText { get; set; } = "";

    /// <summary>告警补充信息(如未就绪节点数);无则为空。</summary>
    public string? Detail { get; set; }

    /// <summary>开立时间(UTC)。</summary>
    public DateTime OpenedAt { get; set; }

    /// <summary>解析时间(UTC);为空表示告警仍处于 open 状态。</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>状态派生:<c>true</c> = 已解析(resolved),<c>false</c> = 告警中(open)。</summary>
    public bool IsResolved => ResolvedAt is not null;
}
