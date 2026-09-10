using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Models;

/// <summary>
/// 纯数据、与 UI 无关的分页查询条件,由 <c>ClusterRepository</c> 消费并翻译为 SQL。
/// </summary>
public record ClusterPageQuery
{
    /// <summary>
    /// 集群分组过滤:null = 不过滤(返回全部集群);
    /// 0 = 未分组哨兵(仓库翻译为 <c>WHERE GroupId IS NULL</c>);
    /// 正数 = 精确匹配该分组 Id。
    /// </summary>
    public int? GroupId { get; init; }

    /// <summary>集群名称模糊匹配(包含,忽略大小写;null = 不过滤)。</summary>
    public string? NameContains { get; init; }

    /// <summary>可达状态过滤(null = 不过滤)。</summary>
    public ClusterStatus? Status { get; init; }

    /// <summary>版本精确过滤(null = 不过滤)。</summary>
    public string? Version { get; init; }

    /// <summary>创建时间下限(含;null = 不限)。</summary>
    public DateTime? CreatedAfter { get; init; }

    /// <summary>创建时间上限(含;null = 不限)。</summary>
    public DateTime? CreatedBefore { get; init; }

    /// <summary>排序字段(默认创建时间)。</summary>
    public ClusterSortField SortBy { get; init; } = ClusterSortField.CreatedAt;

    /// <summary>是否降序排列(默认 true)。</summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>页码(从 1 起)。</summary>
    public int Page { get; init; } = 1;

    /// <summary>每页条数(默认 20)。</summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// 版本过滤哨兵值:前端下拉框与 <c>ClusterRepository</c> 之间的约定。
/// </summary>
public static class VersionFilterSentinel
{
    /// <summary>空字符串 = 不过滤版本(全部)。</summary>
    public const string All = "";

    /// <summary>哨兵串 = 仅查版本为 null 的集群。</summary>
    public const string OnlyNull = "__null__";
}
