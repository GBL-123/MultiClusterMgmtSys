using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群定时同步设置服务:管理启用开关与同步间隔。
/// 取值优先级为数据库 AppSetting,其次配置文件,非法或缺失回退默认(启用、5 分钟)。
/// </summary>
public class ClusterSyncSettingService(
    AppSettingRepository repo,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    AuditService auditService,
    ILogger<ClusterSyncSettingService> logger)
{
    private const string AdminRole = "Admin";

    private const string EnabledKey = "ClusterSync:Enabled";

    private const string IntervalKey = "ClusterSync:IntervalMinutes";

    private const bool DefaultEnabled = true;

    private const int DefaultIntervalMinutes = 5;

    /// <summary>允许设置的最小同步间隔(分钟)。</summary>
    public const int MinIntervalMinutes = 1;

    /// <summary>允许设置的最大同步间隔(分钟)。</summary>
    public const int MaxIntervalMinutes = 1440;

    /// <summary>读取定时同步设置(是否启用与间隔分钟数);数据库未配置或非法时按配置文件解析,仍非法则回退默认值。</summary>
    public async Task<ClusterSyncSettingsViewModel> GetClusterSyncSettingsAsync()
    {
        var rows = await repo.GetByKeysAsync([EnabledKey, IntervalKey]);
        rows.TryGetValue(EnabledKey, out var enabledRaw);
        rows.TryGetValue(IntervalKey, out var intervalRaw);

        var settings = new ClusterSyncSettingsViewModel(
            ResolveEnabled(enabledRaw),
            ResolveInterval(intervalRaw));
        logger.LogInformation("GetClusterSyncSettings enabled={Enabled} intervalMinutes={IntervalMinutes}",
            settings.Enabled, settings.IntervalMinutes);
        return settings;
    }

    /// <summary>更新定时同步设置并落库;仅管理员可操作(否则抛 <see cref="PermissionException"/>),间隔超出允许范围抛 <see cref="ValidationException"/>,成功后写审计。</summary>
    public async Task UpdateClusterSyncSettingsAsync(ClusterSyncSettingsUpdateRequest request)
    {
        logger.LogInformation("UpdateClusterSyncSettings enabled={Enabled} intervalMinutes={IntervalMinutes}",
            request.Enabled, request.IntervalMinutes);

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole(AdminRole))
        {
            logger.LogWarning("UpdateClusterSyncSettings denied: caller is not admin");
            throw new PermissionException("仅管理员可修改定时同步设置");
        }

        if (request.IntervalMinutes is < MinIntervalMinutes or > MaxIntervalMinutes)
        {
            throw new ValidationException($"同步间隔必须为 {MinIntervalMinutes}~{MaxIntervalMinutes} 分钟");
        }

        await repo.SetAsync(EnabledKey, request.Enabled.ToString());
        await repo.SetAsync(IntervalKey, request.IntervalMinutes.ToString());

        logger.LogInformation("UpdateClusterSyncSettings done");
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update,
            $"定时同步设置: 间隔 {request.IntervalMinutes} 分钟,{(request.Enabled ? "启用" : "停用")}");
    }

    private bool ResolveEnabled(string? dbValue)
    {
        if (dbValue is not null && bool.TryParse(dbValue, out var fromDb))
        {
            return fromDb;
        }

        try
        {
            return configuration.GetValue(EnabledKey, DefaultEnabled);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ClusterSync:Enabled 配置非法,回退默认值 true");
            return DefaultEnabled;
        }
    }

    private int ResolveInterval(string? dbValue)
    {
        if (dbValue is not null
            && int.TryParse(dbValue, out var fromDb)
            && fromDb is >= MinIntervalMinutes and <= MaxIntervalMinutes)
        {
            return fromDb;
        }

        try
        {
            var fromConfig = configuration.GetValue(IntervalKey, DefaultIntervalMinutes);
            return fromConfig is >= MinIntervalMinutes and <= MaxIntervalMinutes
                ? fromConfig
                : DefaultIntervalMinutes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ClusterSync:IntervalMinutes 配置非法,回退默认值 {Default} 分钟", DefaultIntervalMinutes);
            return DefaultIntervalMinutes;
        }
    }
}
