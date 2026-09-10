namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 更新集群定时同步设置的入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterSyncSettingService"/> 的更新方法(UpdateClusterSyncSettingsAsync)消费;仅管理员可提交。
/// </summary>
/// <param name="Enabled">是否启用定时同步(true = 启用,false = 停用)。</param>
/// <param name="IntervalMinutes">同步间隔分钟数,有效范围 1~1440,越界抛校验异常。</param>
public record ClusterSyncSettingsUpdateRequest(bool Enabled, int IntervalMinutes);
