namespace MultiClusterMgmtSys.Requests;

public record NodeDetailQueryRequest(int ClusterId, string NodeName);