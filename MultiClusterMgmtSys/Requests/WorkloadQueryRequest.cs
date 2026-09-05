namespace MultiClusterMgmtSys.Requests;

public record WorkloadQueryRequest(int ClusterId, string? Namespace);
