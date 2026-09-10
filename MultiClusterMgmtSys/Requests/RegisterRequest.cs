namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 自助注册入参,由 <see cref="MultiClusterMgmtSys.Services.AuthService"/> 的注册方法(RegisterAsync)消费;注册成功自动授予 Member 角色。
/// </summary>
/// <param name="UserName">用户名(唯一)。</param>
/// <param name="Password">密码(需满足密码策略:至少 8 位且含数字)。</param>
/// <param name="ConfirmPassword">确认密码(两次输入须一致,由前端校验)。</param>
public record RegisterRequest(string UserName, string Password, string ConfirmPassword);
