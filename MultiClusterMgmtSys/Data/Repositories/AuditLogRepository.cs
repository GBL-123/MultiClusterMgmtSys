using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;

namespace MultiClusterMgmtSys.Data.Repositories;

public class AuditLogRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    public async Task AddAsync(AuditLog entity)
    {
        db.AuditLogs.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentForUserAsync(string userName, int count)
    {
        var items = await db.AuditLogs.AsNoTracking()
            .Where(l => l.UserName == userName)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();

        return items;
    }

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
