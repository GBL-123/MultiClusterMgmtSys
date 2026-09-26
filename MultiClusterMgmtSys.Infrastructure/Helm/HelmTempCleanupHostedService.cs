namespace MultiClusterMgmtSys.Infrastructure.Helm;

/// <summary>
/// 应用启动时清扫受管临时目录中超龄(24 小时)的遗留 Helm 操作目录;
/// best-effort、仅记日志,失败不影响应用启动。
/// </summary>
public sealed class HelmTempCleanupHostedService(ILogger<HelmTempCleanupHostedService> logger) : IHostedService
{
    private static readonly TimeSpan _Retention = TimeSpan.FromHours(24);

    /// <summary>启动时清扫一次;异常仅记警告。</summary>
    /// <param name="cancellationToken">启动取消令牌。</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var removed = HelmTempDirectory.CleanupStale(HelmTempDirectory.Root, _Retention, DateTime.UtcNow);
            if (removed > 0)
            {
                logger.LogInformation("Cleaned {Count} stale helm temp directories", removed);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Helm temp directory cleanup failed");
        }
        return Task.CompletedTask;
    }

    /// <summary>无停机行为。</summary>
    /// <param name="cancellationToken">停机取消令牌。</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
