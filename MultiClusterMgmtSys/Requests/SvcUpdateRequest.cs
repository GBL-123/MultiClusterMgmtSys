namespace MultiClusterMgmtSys.Requests;

public record SvcUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);
