using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Daos;

public class GroupRepository(AppDbContext db)
{
    private readonly AppDbContext db = db;

    public async Task<List<ClusterGroup>> GetAllAsync()
    {
        return await db.ClusterGroups.Include(g => g.Clusters).OrderBy(g => g.Id).ToListAsync();
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
}
