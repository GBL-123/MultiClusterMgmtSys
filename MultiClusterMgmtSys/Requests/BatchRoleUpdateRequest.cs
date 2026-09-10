namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 批量调整账号角色的入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的批量改角色方法(BatchUpdateRoleAsync)消费;
/// 角色不存在时抛业务异常,当前登录账号与内置管理员会被跳过,并保护最后一名 Admin。
/// </summary>
/// <param name="Ids">目标账号 Id 列表(空列表时直接返回零处理)。</param>
/// <param name="RoleName">目标角色名(Admin / Member,必须已存在)。</param>
public record BatchRoleUpdateRequest(IReadOnlyList<int> Ids, string RoleName);