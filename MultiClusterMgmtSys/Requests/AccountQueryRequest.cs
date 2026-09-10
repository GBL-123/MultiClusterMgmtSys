namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 账号分页查询入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的分页查询方法(GetPagedAccountsAsync)消费。
/// </summary>
public class AccountQueryRequest
{
    /// <summary>按用户名模糊搜索(包含;null 或空白 = 不过滤)。</summary>
    public string? SearchName { get; set; }

    /// <summary>按角色名精确过滤(如 Admin / Member;null 或空 = 不过滤;角色不存在时返回空结果)。</summary>
    public string? RoleFilter { get; set; }

    /// <summary>页码,从 1 起(小于 1 会被服务层归一为 1)。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数,默认 20(小于 1 会被服务层归一为 1)。</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>排序字段:UserName / LastLoginAt / CreatedAt(其他值按 CreatedAt 处理),默认 CreatedAt。</summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>是否降序排列(默认 true;并列时以 Id 升序作为稳定次序)。</summary>
    public bool SortDescending { get; set; } = true;
}
