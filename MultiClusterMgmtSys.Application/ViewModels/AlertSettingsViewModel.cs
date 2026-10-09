namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 告警设置展示数据。
/// </summary>
/// <param name="OfflineThresholdMinutes">集群离线持续时间阈值(分钟):离线超过该时长即开立「集群离线」告警。</param>
public record AlertSettingsViewModel(int OfflineThresholdMinutes);
