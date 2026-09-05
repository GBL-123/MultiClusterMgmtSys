namespace MultiClusterMgmtSys.Requests;

public record ConfigMapUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);