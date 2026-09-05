namespace MultiClusterMgmtSys.Requests;

public record AccountCreateRequest(string UserName, string Password, string RoleName);