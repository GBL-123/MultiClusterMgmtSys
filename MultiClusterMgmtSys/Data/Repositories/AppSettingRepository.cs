using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Data.Repositories;

/// <summary>
/// 应用设置的数据库仓储:按键读写持久化配置,不访问 Kubernetes API。
/// </summary>
public class AppSettingRepository(ApplicationDbContext db)
{
    /// <summary>按一组键批量读取设置,返回键到值的字典;库中不存在的键不出现在结果里,无副作用。</summary>
    /// <param name="keys">要读取的设置键集合。</param>
    /// <returns>键到值的字典(仅包含库中已存在的键)。</returns>
    public async Task<Dictionary<string, string>> GetByKeysAsync(IReadOnlyCollection<string> keys)
    {
        var items = await db.AppSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync();
        return items.ToDictionary(s => s.Key, s => s.Value);
    }

    /// <summary>按键写入设置值:已存在则更新,不存在则新增,并同步刷新 UpdatedAt 为当前 UTC 时间。</summary>
    /// <param name="key">设置键(唯一)。</param>
    /// <param name="value">设置值。</param>
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
