using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 告警设置服务:管理集群离线持续时间阈值。
/// 取值优先级为数据库 AppSetting,其次配置文件,非法或缺失回退默认(10 分钟)。
/// </summary>
public class AlertSettingService(
    IAppSettingRepository repo,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    AuditService auditService,
    ILogger<AlertSettingService> logger)
{
    private const string AdminRole = "Admin";

    private const string ThresholdKey = "Alert:OfflineThresholdMinutes";

    private const int DefaultThresholdMinutes = 10;

    /// <summary>允许设置的最小离线持续阈值(分钟)。</summary>
    public const int MinOfflineThresholdMinutes = 1;

    /// <summary>允许设置的最大离线持续阈值(分钟)。</summary>
    public const int MaxOfflineThresholdMinutes = 1440;

    /// <summary>读取告警设置:数据库未配置或非法时按配置文件解析,仍非法则回退默认值。</summary>
    public async Task<AlertSettingsViewModel> GetAlertSettingsAsync()
    {
        var rows = await repo.GetByKeysAsync([ThresholdKey]);
        rows.TryGetValue(ThresholdKey, out var thresholdRaw);

        var settings = new AlertSettingsViewModel(ResolveThreshold(thresholdRaw));
        logger.LogInformation("GetAlertSettings offlineThresholdMinutes={OfflineThresholdMinutes}", settings.OfflineThresholdMinutes);
        return settings;
    }

    /// <summary>更新告警设置并落库;仅管理员可操作(否则抛 <see cref="PermissionException"/>),阈值超出允许范围抛 <see cref="ValidationException"/>,成功后写审计。</summary>
    public async Task UpdateAlertSettingsAsync(AlertSettingsUpdateRequest request)
    {
        logger.LogInformation("UpdateAlertSettings offlineThresholdMinutes={OfflineThresholdMinutes}",
            request.OfflineThresholdMinutes);

        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole(AdminRole))
        {
            logger.LogWarning("UpdateAlertSettings denied: caller is not admin");
            throw new PermissionException("仅管理员可修改告警设置");
        }

        if (request.OfflineThresholdMinutes is < MinOfflineThresholdMinutes or > MaxOfflineThresholdMinutes)
        {
            throw new ValidationException($"离线持续时间阈值必须为 {MinOfflineThresholdMinutes}~{MaxOfflineThresholdMinutes} 分钟");
        }

        await repo.SetAsync(ThresholdKey, request.OfflineThresholdMinutes.ToString());

        logger.LogInformation("UpdateAlertSettings done");
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update,
            $"告警设置: 离线持续阈值 {request.OfflineThresholdMinutes} 分钟");
    }

    private int ResolveThreshold(string? dbValue)
    {
        if (dbValue is not null
            && int.TryParse(dbValue, out var fromDb)
            && fromDb is >= MinOfflineThresholdMinutes and <= MaxOfflineThresholdMinutes)
        {
            return fromDb;
        }

        try
        {
            var fromConfig = configuration.GetValue(ThresholdKey, DefaultThresholdMinutes);
            return fromConfig is >= MinOfflineThresholdMinutes and <= MaxOfflineThresholdMinutes
                ? fromConfig
                : DefaultThresholdMinutes;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Alert:OfflineThresholdMinutes 配置非法,回退默认值 {Default} 分钟", DefaultThresholdMinutes);
            return DefaultThresholdMinutes;
        }
    }
}
