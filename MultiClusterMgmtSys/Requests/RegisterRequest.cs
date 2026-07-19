namespace MultiClusterMgmtSys.Requests;

public record RegisterRequest(string UserName, string Password, string ConfirmPassword);
