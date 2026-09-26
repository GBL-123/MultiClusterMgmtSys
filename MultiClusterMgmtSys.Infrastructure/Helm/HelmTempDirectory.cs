namespace MultiClusterMgmtSys.Infrastructure.Helm;

/// <summary>
/// Helm 操作临时目录管理:根目录为系统临时目录下的 mcm-helm;
/// 每次操作创建独立子目录并在结束后删除,应用启动时清扫超龄遗留目录。
/// </summary>
internal static class HelmTempDirectory
{
    /// <summary>受管临时根目录(系统临时目录下的 mcm-helm)。</summary>
    public static string Root { get; } = Path.Combine(Path.GetTempPath(), "mcm-helm");

    /// <summary>在根目录下创建本次操作的独立子目录。</summary>
    /// <returns>子目录绝对路径。</returns>
    public static string Create()
    {
        Directory.CreateDirectory(Root);
        var directory = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>删除临时目录(best-effort,失败返回 false 由调用方记录日志)。</summary>
    /// <param name="directory">待删除目录。</param>
    /// <returns>是否删除成功。</returns>
    public static bool TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>清扫根目录下最后写入时间早于保留时长的子目录;单个目录失败不阻断整轮清扫。</summary>
    /// <param name="root">待清扫的根目录。</param>
    /// <param name="maxAge">保留时长。</param>
    /// <param name="utcNow">当前 UTC 时间(注入以便测试)。</param>
    /// <returns>实际删除的目录数。</returns>
    public static int CleanupStale(string root, TimeSpan maxAge, DateTime utcNow)
    {
        if (!Directory.Exists(root))
        {
            return 0;
        }

        var removed = 0;
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            try
            {
                if (utcNow - Directory.GetLastWriteTimeUtc(directory) <= maxAge)
                {
                    continue;
                }
                Directory.Delete(directory, recursive: true);
                removed++;
            }
            catch
            {
                // 单个目录删除失败(如文件被占用)不阻断整轮清扫
            }
        }
        return removed;
    }
}
