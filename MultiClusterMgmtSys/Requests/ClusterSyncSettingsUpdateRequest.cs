namespace MultiClusterMgmtSys.Requests;

public record ClusterSyncSettingsUpdateRequest(bool Enabled, int IntervalMinutes);
