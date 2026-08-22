using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

public class GroupRepository(ApplicationDbContext db)
{
    private readonly ApplicationDbContext db = db;

    public async Task<List<ClusterGroup>> GetAllAsync()
    {
        // AsNoTracking: 侧栏的 ClusterCount 是只读读模型。若跟踪实体, EF identity resolution
        // 会在 ExecuteUpdateAsync(SetGroupIdForClustersAsync) 改了库后仍返回内存里的旧 GroupId,
        // 导致批量移动分组后每个分组的数量不刷新。
        return await db.ClusterGroups.AsNoTracking().Include(g => g.Clusters).OrderBy(g => g.Id).ToListAsync();
    }

    public async Task<ClusterGroup?> GetByIdAsync(int id)
    {
        return await db.ClusterGroups.Include(g => g.Clusters).FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<ClusterGroup> AddAsync(ClusterGroup entity)
    {
        db.ClusterGroups.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.ClusterGroups.FindAsync(id);
        if (entity is not null)
        {
            db.ClusterGroups.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

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
