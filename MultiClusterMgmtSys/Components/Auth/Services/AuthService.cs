using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Components.Auth.Requests;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Components.Auth.Services;

public class AuthService(
    UserManager<ApplicationUser> userManger,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthService> logger)
{
    private const string MemberRole = "Member";

    private readonly UserManager<ApplicationUser> userManager = userManger;
    private readonly SignInManager<ApplicationUser> signInManager = signInManager;
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
        return await signInManager.PasswordSignInAsync(
            request.UserName,
            request.Password,
            request.AutoLogin,
            lockoutOnFailure: false);
    }

    public async Task LogoutAsync(string userName)
    {
        logger.LogInformation("{userName} logging out", userName);
        await signInManager.SignOutAsync();
    }
}
