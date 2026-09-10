using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

/// <summary>
/// 集群分组的数据库仓储:只做实体持久化与查询,不访问 Kubernetes API;
/// 业务规则(重名校验、移动分组时的审计等)由上层服务负责。
/// </summary>
public class GroupRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    /// <summary>
    /// 查询全部分组,按 Id 升序,附带组内集群集合用于计算侧栏计数;无副作用。
    /// </summary>
    /// <returns>未跟踪的分组列表(含集群集合)。</returns>
    public async Task<List<ClusterGroup>> GetAllAsync()
    {
        // AsNoTracking: 侧栏的 ClusterCount 是只读读模型。若跟踪实体, EF identity resolution
        // 会在 ExecuteUpdateAsync(SetGroupIdForClustersAsync) 改了库后仍返回内存里的旧 GroupId,
        // 导致批量移动分组后每个分组的数量不刷新。
        return await db.ClusterGroups.AsNoTracking().Include(g => g.Clusters).OrderBy(g => g.Id).ToListAsync();
    }

    /// <summary>按 Id 查询单个分组,附带组内集群集合;不存在时返回 null。</summary>
    /// <param name="id">分组 Id。</param>
    /// <returns>分组实体;不存在为 null。</returns>
    public async Task<ClusterGroup?> GetByIdAsync(int id)
    {
        return await db.ClusterGroups.Include(g => g.Clusters).FirstOrDefaultAsync(g => g.Id == id);
    }

    /// <summary>新增分组并保存;返回带自增 Id 的实体,审计由上层服务写入。</summary>
    /// <param name="entity">待新增的分组。</param>
    /// <returns>保存后的分组实体(含生成的 Id)。</returns>
    public async Task<ClusterGroup> AddAsync(ClusterGroup entity)
    {
        db.ClusterGroups.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    /// <summary>删除指定分组并保存;分组不存在时静默跳过。组内集群不删除,GroupId 由数据库按 SetNull 置空。</summary>
    /// <param name="id">分组 Id。</param>
    public async Task DeleteAsync(int id)
    {
        var entity = await db.ClusterGroups.FindAsync(id);
        if (entity is not null)
        {
            db.ClusterGroups.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>重命名指定分组并保存;分组不存在时静默跳过,重名校验由上层服务负责。</summary>
    /// <param name="id">分组 Id。</param>
    /// <param name="newName">新名称。</param>
    public async Task RenameAsync(int id, string newName)
    {
        var entity = await db.ClusterGroups.FindAsync(id);
        if (entity is not null)
        {
            entity.Name = newName;
            await db.SaveChangesAsync();
        }
    }
}
