namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 更新告警设置的入参,由 <see cref="MultiClusterMgmtSys.Application.Services.AlertSettingService"/> 的更新方法(UpdateAlertSettingsAsync)消费;仅管理员可提交。
/// </summary>
/// <param name="OfflineThresholdMinutes">集群离线持续时间阈值(分钟),有效范围 1~1440,越界抛校验异常。</param>
public record AlertSettingsUpdateRequest(int OfflineThresholdMinutes);
