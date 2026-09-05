namespace MultiClusterMgmtSys.Requests;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);