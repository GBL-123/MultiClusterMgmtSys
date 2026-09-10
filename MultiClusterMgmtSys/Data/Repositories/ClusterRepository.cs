using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Data.Repositories;

/// <summary>
/// 集群数据的数据库仓储:只负责持久化与查询翻译,不访问 Kubernetes API;
/// 把 <see cref="ClusterPageQuery"/> 的过滤/排序/分页语义翻译为 SQL(契约见 cluster-query-layering spec)。
/// </summary>
public class ClusterRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    /// <summary>按 Id 加载集群,附带分组、端点与节点 IP 备注集合(跟踪查询,可修改后保存);不存在时返回 null。</summary>
    /// <param name="id">集群 Id。</param>
    /// <returns>集群实体;不存在为 null。</returns>
    public async Task<ClusterInfo?> GetByIdAsync(int id)
    {
        return await db.Clusters
            .Include(c => c.Group)
            .Include(c => c.Endpoints)
            .Include(c => c.NodeIpRemarks)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>新增集群并保存;返回带自增 Id 的实体,审计由上层服务写入。</summary>
    /// <param name="entity">待新增的集群(凭据、状态等由调用方填充)。</param>
    /// <returns>保存后的集群实体(含生成的 Id)。</returns>
    public async Task<ClusterInfo> AddAsync(ClusterInfo entity)
    {
        db.Clusters.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    /// <summary>将集群实体标记为已修改并保存(全字段更新),供编辑、状态同步等场景使用。</summary>
    /// <param name="entity">待更新的集群实体。</param>
    public async Task UpdateAsync(ClusterInfo entity)
    {
        db.Clusters.Update(entity);
        await db.SaveChangesAsync();
    }

    /// <summary>删除指定集群并保存;不存在时静默跳过,其端点与节点 IP 备注级联删除。</summary>
    /// <param name="id">集群 Id。</param>
    public async Task DeleteAsync(int id)
    {
        var entity = await db.Clusters.FindAsync(id);
        if (entity is not null)
        {
            db.Clusters.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 按查询条件分页筛选集群,语义如下:
    /// GroupId null=不过滤、0=未分组哨兵(WHERE GroupId IS NULL)、正数=精确匹配该分组;
    /// 名称模糊包含;状态精确匹配;版本 ""=全部、"__null__"=仅版本为空(null 或空串),其余为精确匹配;
    /// 创建时间下限含当日,上限按加一天换算为开区间,同样覆盖到当日末尾。
    /// 排序后以 Id 倒序追加为稳定次序键,页码/页大小小于 1 时按 1 处理。
    /// </summary>
    /// <param name="q">过滤、排序与分页条件。</param>
    /// <returns>当页集群列表,以及过滤后(分页前)的命中总数。</returns>
    public async Task<(List<ClusterInfo> Items, int Total)> GetPagedAsync(ClusterPageQuery q)
    {
        var query = db.Clusters.Include(c => c.Group).AsNoTracking();

        if (q.GroupId.HasValue)
        {
            if (q.GroupId == 0)
                query = query.Where(c => c.GroupId == null);
            else
                query = query.Where(c => c.GroupId == q.GroupId);
        }

        if (!string.IsNullOrWhiteSpace(q.NameContains))
            query = query.Where(c => c.Name.Contains(q.NameContains));

        if (q.Status.HasValue)
            query = query.Where(c => c.Status == q.Status);

        if (q.Version == VersionFilterSentinel.OnlyNull)
            query = query.Where(c => string.IsNullOrEmpty(c.Version));
        else if (q.Version is not null)
            query = query.Where(c => c.Version == q.Version);

        if (q.CreatedAfter is not null)
        {
            var start = q.CreatedAfter.Value;
            query = query.Where(c => c.CreatedAt >= start);
        }

        if (q.CreatedBefore is not null)
        {
            var end = q.CreatedBefore.Value.AddDays(1);
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

    /// <summary>查询全部非空集群版本,去重后按版本号升序,供版本筛选下拉使用;无副作用。</summary>
    /// <returns>去重升序后的版本列表。</returns>
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

    /// <summary>
    /// 以 ExecuteUpdate 批量修改一组集群的所属分组(targetGroupId 传 null 即移出分组,未分组化)。
    /// 直接生成单条 UPDATE,不经 EF 变更跟踪;返回受影响行数,集群 Id 列表为空时直接返回 0。
    /// </summary>
    /// <param name="clusterIds">目标集群 Id 集合。</param>
    /// <param name="targetGroupId">目标分组 Id;null 表示脱离分组。</param>
    /// <returns>受影响的行数。</returns>
    public async Task<int> SetGroupIdForClustersAsync(IEnumerable<int> clusterIds, int? targetGroupId)
    {
        var ids = clusterIds.ToList();
        if (ids.Count == 0) return 0;

        return await db.Clusters
            .Where(c => ids.Contains(c.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.GroupId, targetGroupId));
    }

    /// <summary>统计未分组(GroupId 为空)的集群数量;无副作用。</summary>
    /// <returns>未分组集群数。</returns>
    public async Task<int> CountUngroupedAsync()
        => await db.Clusters.CountAsync(c => c.GroupId == null);

    /// <summary>查询全部集群 Id,用于全量同步等批量任务;无副作用。</summary>
    /// <returns>全部集群 Id 列表。</returns>
    public async Task<List<int>> GetAllIdsAsync()
        => await db.Clusters.Select(c => c.Id).ToListAsync();
}