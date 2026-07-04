using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Daos;

public class ClusterRepository(AppDbContext db)
{
    private readonly AppDbContext db = db;

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
}
