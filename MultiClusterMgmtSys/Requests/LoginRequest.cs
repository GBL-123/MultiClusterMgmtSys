namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 登录入参,由 <see cref="MultiClusterMgmtSys.Services.AuthService"/> 的登录方法(LoginAsync)消费。
/// </summary>
/// <param name="UserName">用户名。</param>
/// <param name="Password">密码。</param>
/// <param name="AutoLogin">是否记住登录(true = 签发持久化 Cookie,false = 仅会话 Cookie)。</param>
public record LoginRequest(string UserName, string Password, bool AutoLogin);
