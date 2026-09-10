namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 集群后台同步设置展示数据。
/// </summary>
/// <param name="Enabled">是否启用后台自动同步。</param>
/// <param name="IntervalMinutes">同步间隔(分钟)。</param>
public record ClusterSyncSettingsViewModel(bool Enabled, int IntervalMinutes);
