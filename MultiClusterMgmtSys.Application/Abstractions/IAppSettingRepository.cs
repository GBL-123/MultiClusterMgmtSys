namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 应用设置的持久化端口:按键读写持久化配置,不访问 Kubernetes API。
/// </summary>
public interface IAppSettingRepository
{
    /// <summary>按一组键批量读取设置,返回键到值的字典;库中不存在的键不出现在结果里,无副作用。</summary>
    /// <param name="keys">要读取的设置键集合。</param>
    /// <returns>键到值的字典(仅包含库中已存在的键)。</returns>
    Task<Dictionary<string, string>> GetByKeysAsync(IReadOnlyCollection<string> keys);

    /// <summary>按键写入设置:已存在则更新,不存在则新增,并同步刷新 UpdatedAt 为当前 UTC 时间。</summary>
    /// <param name="key">设置键(唯一)。</param>
    /// <param name="value">设置值。</param>
    Task SetAsync(string key, string value);
}
