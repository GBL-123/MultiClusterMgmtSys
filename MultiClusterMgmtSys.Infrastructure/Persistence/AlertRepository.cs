using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Infrastructure.Persistence;

/// <summary>
/// 告警记录的数据库仓储:只负责开立、解析与查询,不访问 Kubernetes API;
/// 同键 open 唯一性由部分唯一索引兜底(评估器单写者,索引为纵深防御)。
/// </summary>
public class AlertRepository(ApplicationDbContext db) : IAlertRepository
{
    private readonly ApplicationDbContext _db = db;

    /// <summary>查询当前全部 open 告警(ResolvedAt 为空),按 Id 升序,无跟踪查询。</summary>
    /// <returns>open 告警记录列表。</returns>
    public async Task<List<AlertRecord>> GetOpenAsync()
    {
        return await _db.AlertRecords
            .AsNoTracking()
            .Where(r => r.ResolvedAt == null)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    /// <summary>统计当前 open 告警总数(ResolvedAt 为空)。</summary>
    /// <returns>open 告警条数。</returns>
    public async Task<int> CountOpenAsync()
    {
        return await _db.AlertRecords
            .AsNoTracking()
            .CountAsync(r => r.ResolvedAt == null);
    }

    /// <summary>按状态过滤分页查询告警记录:开立时间倒序、Id 倒序稳定次序,页码/页大小小于 1 时按 1 处理。</summary>
    /// <param name="query">状态过滤与分页条件。</param>
    /// <returns>当页告警记录列表,以及过滤后(分页前)的命中总数。</returns>
    public async Task<(List<AlertRecord> Items, int Total)> GetPagedAsync(AlertPageQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.PageSize, 1);

        IQueryable<AlertRecord> q = _db.AlertRecords.AsNoTracking();

        if (query.Resolved.HasValue)
        {
            q = query.Resolved.Value
                ? q.Where(r => r.ResolvedAt != null)
                : q.Where(r => r.ResolvedAt == null);
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(r => r.OpenedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>开立一条告警并立即保存(ResolvedAt 保持为空)。</summary>
    /// <param name="record">待开立的告警记录。</param>
    public async Task AddAsync(AlertRecord record)
    {
        _db.AlertRecords.Add(record);
        await _db.SaveChangesAsync();
    }

    /// <summary>把指定告警解析为 resolved(回填解析时间并保存);记录不存在或已解析时静默跳过。</summary>
    /// <param name="id">告警记录 Id。</param>
    /// <param name="resolvedAtUtc">解析时间(UTC)。</param>
    public async Task ResolveAsync(int id, DateTime resolvedAtUtc)
    {
        var record = await _db.AlertRecords.FirstOrDefaultAsync(r => r.Id == id);
        if (record is null || record.ResolvedAt is not null)
        {
            return;
        }

        record.ResolvedAt = resolvedAtUtc;
        await _db.SaveChangesAsync();
    }
}
