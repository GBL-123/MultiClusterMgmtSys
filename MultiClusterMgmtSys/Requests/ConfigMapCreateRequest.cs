namespace MultiClusterMgmtSys.Requests;

public record ConfigMapCreateRequest(int ClusterId, string Yaml);