using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 集群分页查询入参:前端把筛选条件与表格状态收拢后传给服务层的查询契约,
/// 由 <see cref="MultiClusterMgmtSys.Services.ClusterService"/> 的分页查询方法(GetPagedAsync)接收,
/// 翻译为纯数据查询对象 <see cref="MultiClusterMgmtSys.Models.ClusterPageQuery"/> 后交给仓库执行。
/// </summary>
public class ClusterQueryRequest
{
    /// <summary>集群名称模糊匹配(包含,忽略大小写;null = 不过滤)。</summary>
    public string? Name { get; set; }

    /// <summary>
    /// 集群分组过滤:<c>null</c> = 不过滤(返回全部集群);
    /// <c>0</c> = 未分组哨兵(仓库将其翻译为 <c>WHERE GroupId IS NULL</c>);
    /// 正数 = 精确匹配该分组 Id。
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>可达状态过滤(<see cref="ClusterStatus"/>;null = 不过滤)。</summary>
    public ClusterStatus? Status { get; set; }

    /// <summary>版本过滤选择:空字符串(<see cref="VersionFilterSentinel.All"/>)= 不过滤;哨兵串 <c>"__null__"</c>(<see cref="VersionFilterSentinel.OnlyNull"/>)= 仅查版本为 null 的集群;其他非空字符串 = 按该版本精确匹配。默认不过滤。</summary>
    public string VersionSelection { get; set; } = VersionFilterSentinel.All;

    /// <summary>创建时间过滤起点(含当天;null = 不限)。</summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>创建时间过滤终点(含当天全天;null = 不限)。</summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>页码,从 1 起(小于 1 会被服务层归一为 1)。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数,默认 20(小于 1 会被服务层归一为 1)。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>排序字段(默认按创建时间),见 <see cref="ClusterSortField"/>。</summary>
    public ClusterSortField SortBy { get; set; } = ClusterSortField.CreatedAt;

    /// <summary>是否降序排列(默认 true;并列时以 Id 降序作为稳定次序)。</summary>
    public bool SortDescending { get; set; } = true;
}

    
