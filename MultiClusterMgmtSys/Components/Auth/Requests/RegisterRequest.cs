namespace MultiClusterMgmtSys.Components.Auth.Requests;

public record RegisterRequest(string UserName, string Password, string ConfirmPassword);
