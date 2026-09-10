namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群状态定时同步后台服务:按 AppSetting 中的启用开关与间隔周期轮询刷新全部集群状态。
/// 每轮通过新 DI 作用域读取设置;整轮失败仅记录错误,不影响下一轮调度。
/// </summary>
public class ClusterSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ClusterSyncBackgroundService> logger) : BackgroundService
{
    /// <summary>主循环:读取定时同步设置,启用时执行一轮全量刷新,随后按设置的间隔休眠,直至停机令牌取消。</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ClusterSyncBackgroundService started");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (enabled, intervalMinutes) = await ReadSettingsAsync();
                logger.LogInformation("ClusterSync round settings enabled={Enabled} intervalMinutes={IntervalMinutes}",
                    enabled, intervalMinutes);

                if (enabled)
                {
                    try
                    {
                        await RunOnceAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "定时同步集群状态整轮失败");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ClusterSyncBackgroundService stopped");
        }
    }

    /// <summary>立即执行一轮全部集群状态刷新(来源标记为定时同步),返回成功探测的集群数量;内部通过新作用域解析 <see cref="ClusterService"/>。</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var clusterService = scope.ServiceProvider.GetRequiredService<ClusterService>();
        var succeeded = await clusterService.RefreshAllClustersStatusAsync(source: ClusterSyncSource.Scheduled);
        logger.LogInformation("ClusterSync run once done succeeded={Succeeded}", succeeded);
        return succeeded;
    }

    private async Task<(bool Enabled, int IntervalMinutes)> ReadSettingsAsync()
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var settings = await scope.ServiceProvider
            .GetRequiredService<ClusterSyncSettingService>()
            .GetClusterSyncSettingsAsync();
        return (settings.Enabled, settings.IntervalMinutes);
    }
}
