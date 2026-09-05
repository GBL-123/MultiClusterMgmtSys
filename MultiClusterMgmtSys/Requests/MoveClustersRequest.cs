namespace MultiClusterMgmtSys.Requests;

public record MoveClustersRequest(IReadOnlyList<int> ClusterIds, int? TargetGroupId);