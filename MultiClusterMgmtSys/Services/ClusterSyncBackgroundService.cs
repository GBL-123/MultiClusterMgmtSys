namespace MultiClusterMgmtSys.Services;

public class ClusterSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ClusterSyncBackgroundService> logger) : BackgroundService
{
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
