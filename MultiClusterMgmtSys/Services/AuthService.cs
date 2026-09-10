using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 认证服务:自助注册(默认 Member 角色)、密码登录与登出。
/// 登录/注册/登出成功后写认证类审计日志。
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManger,
    SignInManager<ApplicationUser> signInManager,
    AuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger)
{
    private const string MemberRole = "Member";

    private readonly UserManager<ApplicationUser> userManager = userManger;

    private readonly SignInManager<ApplicationUser> signInManager = signInManager;

    private readonly AuditService auditService = auditService;

    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    private readonly ILogger<AuthService> logger = logger;

    /// <summary>注册新用户并默认赋予 Member 角色,成功后写注册审计;失败(如用户名重复)不抛异常,以 Identity 结果返回中文错误文案。</summary>
    /// <param name="request">用户名与密码。</param>
    /// <returns>Identity 注册结果。</returns>
    public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
    {
        logger.LogInformation("Registering user: {UserName}", request.UserName);
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            NormalizedUserName = request.UserName.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, MemberRole);
            logger.LogInformation("User registered successfully: {UserName}", request.UserName);
            await auditService.LogAsync(AuditCategory.Authentication, AuditAction.Register, $"账号: {request.UserName}", request.UserName);
        }
        else
        {
            logger.LogWarning("Failed to register user: {UserName}", request.UserName);
        }
        return result;
    }

    /// <summary>密码登录;成功后更新用户最后登录时间并写登录审计,失败不抛异常、由调用方处理结果。</summary>
    /// <param name="request">用户名、密码与是否自动登录(记住会话)。</param>
    /// <returns>登录结果(Succeeded 表示成功)。</returns>
    public async Task<SignInResult> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("{UserName} login", request.UserName);
        var result = await signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            request.AutoLogin,
            lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
            }
            await auditService.LogAsync(AuditCategory.Authentication, AuditAction.Login, $"账号: {request.UserName}", request.UserName);
        }
        return result;
    }

    /// <summary>登出当前会话并写登出审计;操作者用户名从当前 HTTP 上下文解析。</summary>
    public async Task LogoutAsync()
    {
        var userName = httpContextAccessor.HttpContext?.User.Identity?.Name;
        logger.LogInformation("{userName} logging out", userName);
        await signInManager.SignOutAsync();
        await auditService.LogAsync(AuditCategory.Authentication, AuditAction.Logout, $"账号: {userName}", userName);
    }
}