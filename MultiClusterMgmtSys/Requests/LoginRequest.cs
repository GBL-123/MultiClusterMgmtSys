namespace MultiClusterMgmtSys.Requests;

public record LoginRequest(string UserName, string Password, bool RememberMe, string ReturnUrl);
