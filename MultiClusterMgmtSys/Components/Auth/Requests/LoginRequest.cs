namespace MultiClusterMgmtSys.Components.Auth.Requests;

public record LoginRequest(string UserName, string Password, bool AutoLogin);
