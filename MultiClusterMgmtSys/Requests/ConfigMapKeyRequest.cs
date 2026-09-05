namespace MultiClusterMgmtSys.Requests;

public record ConfigMapKeyRequest(int ClusterId, string Name, string Namespace);