namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 管理员重置指定账号密码的入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的重置密码方法(ResetPasswordAsync)消费;内置管理员账号不可重置。
/// </summary>
/// <param name="Id">目标账号 Id(数据库主键)。</param>
/// <param name="NewPassword">新密码(需满足密码策略:至少 8 位且含数字)。</param>
public record ResetPasswordRequest(int Id, string NewPassword);