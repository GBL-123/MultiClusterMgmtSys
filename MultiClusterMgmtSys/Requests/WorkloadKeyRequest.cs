namespace MultiClusterMgmtSys.Requests;

public record WorkloadKeyRequest(int ClusterId, string Name, string Namespace);
