namespace MultiClusterMgmtSys.Domain.Entities;

/// <summary>
/// 告警记录:告警评估器把集群异常固化为可追溯的记录,状态机为 open → resolved。
/// open 与 ResolvedAt 为空等价(不设独立状态列,派生态避免双写漂移);
/// 条件不再满足时由评估器自动解析并保留解析时间,历史记录持久保留、不物理删除。
/// 同一集群同一规则至多一条 open 记录(数据库部分唯一索引兜底)。
/// 随所属集群级联删除。
/// </summary>
public class AlertRecord
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>所属集群 Id;集群删除时本记录随其级联删除。</summary>
    public int ClusterId { get; set; }

    /// <summary>所属集群导航属性。</summary>
    public ClusterInfo? Cluster { get; set; }

    /// <summary>命中的告警规则类别,见 <see cref="Domain.Enums.AlertRuleKind"/>。</summary>
    public Domain.Enums.AlertRuleKind RuleKind { get; set; }

    /// <summary>告警补充信息(如未就绪节点数);仅部分规则填写,可为空。</summary>
    public string? Detail { get; set; }

    /// <summary>开立时间(UTC),即评估器判定规则命中并写入本记录的时刻。</summary>
    public DateTime OpenedAt { get; set; }

    /// <summary>解析时间(UTC,可空);为空表示告警仍处于 open 状态,非空表示已由评估器自动解析。</summary>
    public DateTime? ResolvedAt { get; set; }
}
