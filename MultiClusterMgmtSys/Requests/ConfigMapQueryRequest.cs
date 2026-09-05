namespace MultiClusterMgmtSys.Requests;

public record ConfigMapQueryRequest(int ClusterId, string? Namespace);