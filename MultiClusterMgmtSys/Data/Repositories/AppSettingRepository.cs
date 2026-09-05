using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

public class AppSettingRepository(ApplicationDbContext db)
{
    public async Task<Dictionary<string, string>> GetByKeysAsync(IReadOnlyCollection<string> keys)
    {
        var items = await db.AppSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();
        return items.ToDictionary(s => s.Key, s => s.Value);
    }

    public async Task SetAsync(string key, string value)
    {
        var existing = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == key);
        if (existing is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
