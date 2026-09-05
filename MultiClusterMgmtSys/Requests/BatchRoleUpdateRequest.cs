namespace MultiClusterMgmtSys.Requests;

public record BatchRoleUpdateRequest(IReadOnlyList<int> Ids, string RoleName);