namespace MultiClusterMgmtSys.Requests;

public record WorkloadCreateRequest(int ClusterId, string Yaml);
