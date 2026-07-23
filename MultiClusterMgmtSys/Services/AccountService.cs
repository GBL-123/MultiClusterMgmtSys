using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Services;

public class AccountService(
    UserManager<ApplicationUser> userManger,
    RoleManager<IdentityRole<int>> roleManager,
    SignInManager<ApplicationUser> signInManager,
    AppDbContext db,
    ILogger<AccountService> logger)
{
    private const string AdminRole = "Admin";
    private const string MemberRole = "Member";
    private const string DefaultPassword = "Changeme_123";

    private readonly UserManager<ApplicationUser> userManager = userManger;
    private readonly RoleManager<IdentityRole<int>> roleManager = roleManager;
    private readonly SignInManager<ApplicationUser> signInManager = signInManager;
    private readonly AppDbContext db = db;
    private readonly ILogger<AccountService> logger = logger;

    public async Task CreateAdminAsync()
    {
        // Ensure roles
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = AdminRole, NormalizedName = AdminRole.ToUpperInvariant() });
        }
        if (!await roleManager.RoleExistsAsync(MemberRole))
        {
            await roleManager.CreateAsync(new IdentityRole<int> { Name = MemberRole, NormalizedName = MemberRole.ToUpperInvariant() });
        }

        // Ensure admin
        if (await userManager.FindByNameAsync("admin") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(admin, DefaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
            }
            else
            {
                logger.LogError("Failed to create admin account: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        logger.LogInformation("Create admin account succeeded");
    }

    public async Task<AccountViewModel[]> GetAllAccountsAsync()
    {
        var list = await userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var vms = new List<AccountViewModel>(list.Count);
        foreach (var user in list)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault() ?? "";
            vms.Add(user.ToAccountViewModel(roleName));
        }
        return vms.ToArray();
    }

    public async Task<AccountViewModel?> GetAccountByIdAsync(int id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;
        var userRoles = await userManager.GetRolesAsync(user);
        return user.ToAccountViewModel(userRoles.FirstOrDefault() ?? "");
    }

    public async Task<IdentityResult> CreateAccountAsync(string username, string password, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRole",
                Description = $"角色 {roleName} 不存在"
            });
        }

        var user = new ApplicationUser
        {
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
        return result;
    }

    public async Task<IdentityResult> UpdateAccountAsync(int id, string? roleName)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var changed = false;
        if (!string.IsNullOrEmpty(roleName) && await roleManager.RoleExistsAsync(roleName))
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(roleName))
            {
                if (currentRoles.Any())
                {
                    await userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await userManager.AddToRoleAsync(user, roleName);
            }
        }

        if (!changed)
        {
            return IdentityResult.Success;
        }
        return await userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> DeleteAccountAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteSelf",
                Description = "不能删除当前登录账号"
            });
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Contains(AdminRole))
        {
            var adminRole = await roleManager.FindByNameAsync(AdminRole);
            if (adminRole is not null)
            {
                var adminCount = (await userManager.GetUsersInRoleAsync(AdminRole)).Count;
                if (adminCount <= 1)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "CannotDeleteLastAdmin",
                        Description = "系统中必须至少保留一个 Admin 账号"
                    });
                }
            }
        }

        return await userManager.DeleteAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        // Validate via Create + Remove approach to leverage IPasswordValidator pipeline
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        return await userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<IdentityResult> UpdateProfileAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }
        user.UpdatedAt = DateTime.UtcNow;
        return await userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }
        return result;
    }


    

    public async Task<ApplicationUser?> GetUserByNameAsync(string username)
    {
        return await userManager.FindByNameAsync(username);
    }
}
