namespace MultiClusterMgmtSys.Requests;

public record SvcKeyRequest(int ClusterId, string Name, string Namespace);
