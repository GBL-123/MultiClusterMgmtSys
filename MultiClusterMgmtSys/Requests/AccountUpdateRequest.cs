namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 管理员编辑账号的入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的编辑账号方法(UpdateAccountAsync)消费;内置管理员账号不可修改。
/// </summary>
/// <param name="Id">目标账号 Id(数据库主键)。</param>
/// <param name="RoleName">要调整到的角色名(Admin / Member);null 或空 = 不变更角色。</param>
public record AccountUpdateRequest(int Id, string? RoleName);