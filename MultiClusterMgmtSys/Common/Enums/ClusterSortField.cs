namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 集群列表的排序字段(由 <c>ClusterRepository</c> 翻译为 SQL 排序)。
/// </summary>
public enum ClusterSortField
{
    /// <summary>集群名称。</summary>
    Name,

    /// <summary>可达状态。</summary>
    Status,

    /// <summary>版本。</summary>
    Version,

    /// <summary>节点数量。</summary>
    NodeCount,

    /// <summary>创建时间(默认)。</summary>
    CreatedAt
}