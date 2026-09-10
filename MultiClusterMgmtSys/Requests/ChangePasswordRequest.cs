namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 当前登录用户修改自己密码的入参,由 <see cref="MultiClusterMgmtSys.Services.AccountService"/> 的改密方法(ChangePasswordAsync)消费。
/// </summary>
/// <param name="CurrentPassword">当前密码(校验用)。</param>
/// <param name="NewPassword">新密码(不得与当前密码相同,需满足密码策略:至少 8 位且含数字)。</param>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);