using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MultiClusterMgmtSys.Components.AuditLogs.Requests;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

public class AuditLogRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    public async Task AddAsync(AuditLog entity)
    {
        db.AuditLogs.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> Items, int Total)> GetPagedAsync(
        TableState state,
        AuditLogQueryRequest query,
        string? currentUserName,
        bool isAdmin)
    {
        var page = state.Page > 0 ? state.Page + 1 : Math.Max(query.Page, 1);
        var pageSize = Math.Max(state.PageSize > 0 ? state.PageSize : query.PageSize, 1);
        var sortDescending = state.SortDirection != SortDirection.Ascending;

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
