using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Infrastructure.Persistence;

namespace MultiClusterMgmtSys.Infrastructure.Persistence;

/// <summary>
/// 账号分页查询的数据库实现:只做 EF 查询翻译,不访问 Kubernetes API;
/// 角色过滤经 AspNetRoles/AspNetUserRoles 关联,角色不存在时返回空页。
/// </summary>
public class AccountQueryRepository(ApplicationDbContext db) : IAccountQueryRepository
{
    /// <summary>按查询条件分页查询账号(用户名模糊、角色过滤、排序),返回当页用户与过滤后总数。</summary>
    /// <param name="query">分页、搜索、过滤与排序参数。</param>
    /// <returns>当页用户列表,以及过滤后(分页前)的命中总数。</returns>
    public async Task<(List<ApplicationUser> Items, int Total)> GetPagedAsync(AccountQueryRequest query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.PageSize, 1);
        var sortDescending = query.SortDescending;

        IQueryable<ApplicationUser> q = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchName))
        {
            var search = query.SearchName.Trim();
            q = q.Where(u => u.UserName != null && u.UserName.Contains(search));
        }

        if (!string.IsNullOrEmpty(query.RoleFilter))
        {
            var roleId = await db.Roles
                .Where(r => r.NormalizedName == query.RoleFilter.ToUpperInvariant())
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();
            if (roleId is null)
            {
                return ([], 0);
            }

            var userIdsInRole = await db.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();
            q = q.Where(u => userIdsInRole.Contains(u.Id));
        }

        var total = await q.CountAsync();
        var users = await (query.SortBy switch
        {
            "UserName" => sortDescending
                ? q.OrderByDescending(u => u.UserName)
                : q.OrderBy(u => u.UserName),
            "LastLoginAt" => sortDescending
                ? q.OrderByDescending(u => u.LastLoginAt)
                : q.OrderBy(u => u.LastLoginAt),
            _ => sortDescending
                ? q.OrderByDescending(u => u.CreatedAt)
                : q.OrderBy(u => u.CreatedAt)
        })
        .ThenBy(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        return (users, total);
    }

    /// <summary>按 Id 集合批量加载用户(跟踪查询,供后续 Identity 变更操作复用同一实例),供批量删除/批量改角色校验使用;Id 集为空时返回空列表。</summary>
    /// <param name="ids">目标用户 Id 集合。</param>
    /// <returns>命中的用户列表(不含不存在的 Id)。</returns>
    public async Task<List<ApplicationUser>> GetByIdsAsync(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
    }
}
