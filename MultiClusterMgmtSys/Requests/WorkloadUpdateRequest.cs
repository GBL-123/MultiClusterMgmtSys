namespace MultiClusterMgmtSys.Requests;

public record WorkloadUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);
