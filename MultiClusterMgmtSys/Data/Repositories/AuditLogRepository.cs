using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;

namespace MultiClusterMgmtSys.Data.Repositories;

/// <summary>
/// 审计日志的数据库仓储:负责审计记录的写入与分页查询,不访问 Kubernetes API;
/// 写入失败时的静默降级由上层 AuditService 负责。
/// </summary>
public class AuditLogRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    /// <summary>写入一条审计记录并立即保存;调用方需自行处理异常(写失败不打扰用户)。</summary>
    /// <param name="entity">待写入的审计记录。</param>
    public async Task AddAsync(AuditLog entity)
    {
        db.AuditLogs.Add(entity);
        await db.SaveChangesAsync();
    }

    /// <summary>查询指定用户最近 count 条审计记录,按 CreatedAt 倒序(个人资料页「最近动态」用);无副作用。</summary>
    /// <param name="userName">用户名,精确匹配。</param>
    /// <param name="count">最多返回的条数。</param>
    /// <returns>按时间倒序的审计记录列表(至多 count 条)。</returns>
    public async Task<List<AuditLog>> GetRecentForUserAsync(string userName, int count)
    {
        var items = await db.AuditLogs.AsNoTracking()
            .Where(l => l.UserName == userName)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();

        return items;
    }

    /// <summary>
    /// 分页查询审计日志:仅按 CreatedAt 排序(请求里的其他排序字段不生效),
    /// 以 Id 倒序追加保证同一时刻记录的次序稳定。
    /// 非管理员强制只看本人记录并忽略 SearchName;管理员可用 SearchName 模糊过滤操作者。
    /// </summary>
    /// <param name="query">分页、排序与过滤条件。</param>
    /// <param name="currentUserName">当前用户名,非管理员时用于限定可见范围。</param>
    /// <param name="isAdmin">是否管理员,决定数据可见范围与搜索能力。</param>
    /// <returns>当页审计记录列表,以及过滤后(分页前)的命中总数。</returns>
    public async Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        AuditLogQueryRequest query,
        string? currentUserName,
        bool isAdmin)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.PageSize, 1);
        var sortDescending = query.SortDescending;

        IQueryable<AuditLog> q = db.AuditLogs.AsNoTracking();

        if (!isAdmin)
        {
            q = q.Where(l => l.UserName == currentUserName);
        }
        else if (!string.IsNullOrWhiteSpace(query.SearchName))
        {
            var search = query.SearchName.Trim();
            q = q.Where(l => l.UserName != null && l.UserName.Contains(search));
        }

        if (query.Category.HasValue)
        {
            q = q.Where(l => l.Category == query.Category.Value);
        }

        var total = await q.CountAsync();

        IOrderedQueryable<AuditLog> ordered = sortDescending
            ? q.OrderByDescending(l => l.CreatedAt)
            : q.OrderBy(l => l.CreatedAt);

        var items = await ordered
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
