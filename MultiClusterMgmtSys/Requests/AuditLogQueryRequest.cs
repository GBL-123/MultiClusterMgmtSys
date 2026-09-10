using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 审计日志分页查询入参,由 <see cref="MultiClusterMgmtSys.Services.AuditService"/> 的分页查询方法(GetPagedAsync)消费;
/// 非管理员登录者固定只能看到自己的日志,管理员可按操作人过滤。
/// </summary>
public class AuditLogQueryRequest
{
    /// <summary>按操作人用户名模糊搜索(仅管理员生效;null 或空白 = 不过滤)。</summary>
    public string? SearchName { get; set; }

    /// <summary>按审计类别过滤(<see cref="AuditCategory"/>;null = 不过滤)。</summary>
    public AuditCategory? Category { get; set; }

    /// <summary>页码,从 1 起(小于 1 会被仓库归一为 1)。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数,默认 20(小于 1 会被仓库归一为 1)。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>排序字段(预留,当前实现固定按创建时间排序),默认 CreatedAt。</summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>是否按创建时间降序排列(默认 true;并列时以 Id 降序作为稳定次序)。</summary>
    public bool SortDescending { get; set; } = true;
}
