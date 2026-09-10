namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 管理员新建账号的入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的新建账号方法(CreateAccountAsync)消费。
/// </summary>
/// <param name="UserName">新账号的用户名(唯一)。</param>
/// <param name="Password">初始密码(需满足密码策略:至少 8 位且含数字)。</param>
/// <param name="RoleName">初始角色名(Admin / Member,角色不存在时创建失败)。</param>
public record AccountCreateRequest(string UserName, string Password, string RoleName);