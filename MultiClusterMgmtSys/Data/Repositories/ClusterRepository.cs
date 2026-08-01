using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Clusters.Requests;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

public class ClusterRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    public async Task<List<ClusterInfo>> GetAllAsync()
    {
        return await db.Clusters.Include(c => c.Group).ToListAsync();
    }

    public async Task<ClusterInfo?> GetByIdAsync(int id)
    {
        return await db.Clusters.Include(c => c.Group).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ClusterInfo> AddAsync(ClusterInfo entity)
    {
        db.Clusters.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(ClusterInfo entity)
    {
        db.Clusters.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Clusters.FindAsync(id);
        if (entity is not null)
        {
            db.Clusters.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task<(List<ClusterInfo> Items, int Total)> GetPagedAsync(ClusterQueryRequest q)
    {
        var query = db.Clusters.Include(c => c.Group).AsNoTracking();

        if (q.GroupId.HasValue)
            query = query.Where(c => c.GroupId == q.GroupId);

        if (!string.IsNullOrWhiteSpace(q.Name))
            query = query.Where(c => c.Name.Contains(q.Name));

        if (q.Status.HasValue)
            query = query.Where(c => c.Status == q.Status);

        if (q.Version == "__NULL__")
            query = query.Where(c => string.IsNullOrEmpty(c.Version));
        else if (q.Version != "__ALL__" && !string.IsNullOrEmpty(q.Version))
            query = query.Where(c => c.Version == q.Version);

        if (q.DateRange?.Start is not null)
        {
            var start = DateTime.SpecifyKind(q.DateRange.Start.Value, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= start);
        }

        if (q.DateRange?.End is not null)
        {
            var end = DateTime.SpecifyKind(q.DateRange.End.Value, DateTimeKind.Utc).AddDays(1);
            query = query.Where(c => c.CreatedAt < end);
        }

        var total = await query.CountAsync();

        IOrderedQueryable<ClusterInfo> ordered = q.SortBy switch
        {
            ClusterSortField.Name => q.SortDescending
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name),
            ClusterSortField.Status => q.SortDescending
                ? query.OrderByDescending(c => c.Status)
                : query.OrderBy(c => c.Status),
            ClusterSortField.Version => q.SortDescending
                ? query.OrderByDescending(c => c.Version)
                : query.OrderBy(c => c.Version),
            ClusterSortField.NodeCount => q.SortDescending
                ? query.OrderByDescending(c => c.NodeCount)
                : query.OrderBy(c => c.NodeCount),
            _ => q.SortDescending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt)
        };

        var stableOrdered = ordered.ThenByDescending(c => c.Id);

        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Max(q.PageSize, 1);
        var items = await stableOrdered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<string>> GetDistinctVersionsAsync()
    {
        return await db.Clusters
            .Select(c => c.Version)
            .Where(v => v != null)
            .Distinct()
            .OrderBy(v => v)
            .Select(v => v!)
            .ToListAsync();
    }
}