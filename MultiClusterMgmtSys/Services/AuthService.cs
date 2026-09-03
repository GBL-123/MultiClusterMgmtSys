using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;

namespace MultiClusterMgmtSys.Services;

public class AuthService(
    UserManager<ApplicationUser> userManger,
    SignInManager<ApplicationUser> signInManager,
    AuditService auditService,
    ILogger<AuthService> logger)
{
    private const string MemberRole = "Member";

    private readonly UserManager<ApplicationUser> userManager = userManger;

    private readonly SignInManager<ApplicationUser> signInManager = signInManager;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<AuthService> logger = logger;

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

    public async Task LogoutAsync(string userName)
    {
        logger.LogInformation("{userName} logging out", userName);
        await signInManager.SignOutAsync();
        await auditService.LogAsync(AuditCategory.Authentication, AuditAction.Logout, $"账号: {userName}", userName);
    }
}
