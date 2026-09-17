using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Application.Requests;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 账号分页查询端口:承载账号列表的数据访问(用户名搜索、角色过滤、排序与分页),
/// 使 Application 层无需依赖 EF Core/DbContext。
/// </summary>
public interface IAccountQueryRepository
{
    /// <summary>
    /// 按查询条件分页查询账号:用户名模糊搜索、角色过滤(角色不存在时返回空页),
    /// 排序以 Id 倒序作稳定次序键,页码/页大小小于 1 时按 1 处理。
    /// </summary>
    /// <param name="query">分页、搜索、过滤与排序参数。</param>
    /// <returns>当页用户列表,以及过滤后(分页前)的命中总数。</returns>
    Task<(List<ApplicationUser> Items, int Total)> GetPagedAsync(AccountQueryRequest query);

    /// <summary>按 Id 集合批量加载用户(跟踪查询,供后续 Identity 变更操作复用同一实例),供批量删除/批量改角色校验使用;Id 集为空时返回空列表。</summary>
    /// <param name="ids">目标用户 Id 集合。</param>
    /// <returns>命中的用户列表(不含不存在的 Id)。</returns>
    Task<List<ApplicationUser>> GetByIdsAsync(IReadOnlyCollection<int> ids);
}
