namespace MultiClusterMgmtSys.Requests;

public record SvcQueryRequest(int ClusterId, string? Namespace);
