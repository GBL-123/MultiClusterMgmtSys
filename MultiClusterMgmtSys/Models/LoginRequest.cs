namespace MultiClusterMgmtSys.Models;

public record LoginRequest(string Username, string Password, bool RememberMe, string? ReturnUrl);
