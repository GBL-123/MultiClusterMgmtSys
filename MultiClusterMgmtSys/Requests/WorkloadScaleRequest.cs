namespace MultiClusterMgmtSys.Requests;

public record WorkloadScaleRequest(int ClusterId, string Name, string Namespace, int Replicas);
