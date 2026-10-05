using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MultiClusterMgmtSys.Application.Abstractions;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 节点健康快照保留清理服务(契约 cluster-scheduled-sync「快照保留清理」):
/// 在全量刷新收尾时删除采集时间早于保留窗口起点的快照。
/// 保留天数取 appsettings `ClusterSync:SnapshotRetentionDays`(代码默认 90,配置缺失或非正整数回退默认);
/// 进程内限频每小时至多一次,限频判定基于注入的 <see cref="TimeProvider"/>;
/// 删除失败仅记录告警,不影响当轮刷新结果;本服务属于自动维护,不写审计。
/// </summary>
/// <remarks>注入作用域工厂(每次清理创建独立作用域解析仓储)、配置与时间源;由宿主 DI 注册为 singleton。</remarks>
/// <param name="scopeFactory">作用域工厂,清理时解析节点健康快照仓储。</param>
/// <param name="configuration">宿主配置,读取保留天数。</param>
/// <param name="timeProvider">时间源(限频判定),测试可注入假时钟。</param>
/// <param name="logger">日志。</param>
public class SnapshotRetentionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<SnapshotRetentionService> logger)
{
    private const string RetentionDaysKey = "ClusterSync:SnapshotRetentionDays";

    private const int DefaultRetentionDays = 90;

    internal const int ThrottleIntervalMinutes = 60;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private readonly TimeProvider _timeProvider = timeProvider;

    private readonly IConfiguration _configuration = configuration;

    private readonly ILogger<SnapshotRetentionService> _logger = logger;

    private DateTime? _lastCleanupUtc;

    /// <summary>解析保留天数:配置缺失、非法或非正整数时回退默认 90。</summary>
    /// <returns>保留天数;至少为 1。</returns>
    internal int ResolveRetentionDays()
    {
        try
        {
            var value = _configuration.GetValue<int?>(RetentionDaysKey);
            return value is >= 1 ? value.Value : DefaultRetentionDays;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClusterSync:SnapshotRetentionDays 配置非法,回退默认值 {Default} 天", DefaultRetentionDays);
            return DefaultRetentionDays;
        }
    }

    /// <summary>
    /// 限频执行过期清理:距上次实际执行不足一个限频间隔时直接跳过(不视为执行);
    /// 执行时删除 `now(UTC) - 保留天数` 之前的快照,任何异常仅记告警。
    /// </summary>
    /// <returns>本次实际执行的删除行数(null=因限频跳过;0=异常或无需删除)。</returns>
    public async Task<int?> CleanupIfDueAsync()
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (_lastCleanupUtc is { } last && nowUtc - last < TimeSpan.FromMinutes(ThrottleIntervalMinutes))
        {
            _logger.LogDebug("SnapshotRetention skipped: throttled until {NextUtc}", last.AddMinutes(ThrottleIntervalMinutes));
            return null;
        }

        try
        {
            var cutoff = nowUtc.AddDays(-ResolveRetentionDays());
            await using var scope = _scopeFactory.CreateAsyncScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<IClusterHealthRepository>().DeleteCapturedBeforeAsync(cutoff);
            _lastCleanupUtc = nowUtc;

            if (deleted > 0)
            {
                _logger.LogInformation("SnapshotRetention deleted {Deleted} snapshots before {CutoffUtc}", deleted, cutoff);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SnapshotRetention cleanup failed; round results unaffected");
            return 0;
        }
    }
}
