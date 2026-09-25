using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Infrastructure.Persistence;

/// <summary>
/// 集群节点健康快照的数据库仓储:只负责追加与「每集群最新一条」查询,不访问 Kubernetes API;
/// 查询走 (ClusterId, CapturedAt) 复合索引,不随快照表行数增长而退化。
/// </summary>
public class ClusterHealthRepository(ApplicationDbContext db) : IClusterHealthRepository
{
    private readonly ApplicationDbContext _db = db;

    /// <summary>追加一条节点健康快照并立即保存;由上层服务在探测成功后调用。</summary>
    /// <param name="snapshot">待追加的快照记录。</param>
    public async Task AddAsync(ClusterHealthSnapshot snapshot)
    {
        _db.ClusterHealthSnapshots.Add(snapshot);
        await _db.SaveChangesAsync();
    }

    /// <summary>查询每个集群最近一条快照:按集群分组,组内按采集时间倒序、再按 Id 倒序取首条,无跟踪查询。</summary>
    /// <returns>集群 Id 到该集群最新一条快照的映射;无快照的集群不出现在结果中。</returns>
    public async Task<Dictionary<int, ClusterHealthSnapshot>> GetLatestPerClusterAsync()
    {
        var latest = await _db.ClusterHealthSnapshots
            .AsNoTracking()
            .GroupBy(s => s.ClusterId)
            .Select(g => g.OrderByDescending(s => s.CapturedAt)
                          .ThenByDescending(s => s.Id)
                          .First())
            .ToListAsync();

        return latest.ToDictionary(s => s.ClusterId);
    }
}
