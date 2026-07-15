using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels.Accounts;
using MultiClusterMgmtSys.ViewModels.Accounts.Mappings;

namespace MultiClusterMgmtSys.Services;

public class AccountService(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<int>> roles,
    SignInManager<ApplicationUser> signIn,
    AppDbContext db,
    ILogger<AccountService> logger)
{
    private const string AdminRole = "Admin";
    private const string MemberRole = "Member";
    private const string DefaultPassword = "Changeme_123";

    private readonly UserManager<ApplicationUser> users = users;
    private readonly RoleManager<IdentityRole<int>> roles = roles;
    private readonly SignInManager<ApplicationUser> signIn = signIn;
    private readonly AppDbContext db = db;
    private readonly ILogger<AccountService> logger = logger;

    public async Task SeedAccountsAsync()
    {
        // Ensure roles
        if (!await roles.RoleExistsAsync(AdminRole))
        {
            await roles.CreateAsync(new IdentityRole<int> { Name = AdminRole, NormalizedName = AdminRole.ToUpperInvariant() });
        }
        if (!await roles.RoleExistsAsync(MemberRole))
        {
            await roles.CreateAsync(new IdentityRole<int> { Name = MemberRole, NormalizedName = MemberRole.ToUpperInvariant() });
        }

        // Ensure admin
        if (await users.FindByNameAsync("admin") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                DisplayName = "管理员",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(admin, DefaultPassword);
            if (result.Succeeded)
            {
                await users.AddToRoleAsync(admin, AdminRole);
            }
            else
            {
                logger.LogError("Failed to create admin seed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Ensure member
        if (await users.FindByNameAsync("member") is null)
        {
            var member = new ApplicationUser
            {
                UserName = "member",
                NormalizedUserName = "MEMBER",
                DisplayName = "访客成员",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(member, DefaultPassword);
            if (result.Succeeded)
            {
                await users.AddToRoleAsync(member, MemberRole);
            }
            else
            {
                logger.LogError("Failed to create member seed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        logger.LogInformation("Seeded admin and member accounts (idempotent)");
    }

    public async Task<IdentityResult> RegisterAsync(string username, string password, string? displayName)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await users.AddToRoleAsync(user, MemberRole);
        }
        return result;
    }

    public async Task<AccountViewModel[]> GetAllAccountsAsync()
    {
        var list = await users.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var vms = new List<AccountViewModel>(list.Count);
        foreach (var user in list)
        {
            var userRoles = await users.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault() ?? "";
            vms.Add(user.ToAccountViewModel(roleName));
        }
        return vms.ToArray();
    }

    public async Task<AccountViewModel?> GetAccountByIdAsync(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return null;
        var userRoles = await users.GetRolesAsync(user);
        return user.ToAccountViewModel(userRoles.FirstOrDefault() ?? "");
    }

    public async Task<IdentityResult> CreateAccountAsync(string username, string password, string? displayName, string roleName)
    {
        if (!await roles.RoleExistsAsync(roleName))
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
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await users.AddToRoleAsync(user, roleName);
        }
        return result;
    }

    public async Task<IdentityResult> UpdateAccountAsync(int id, string? displayName, string? roleName)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var changed = false;
        if (displayName is not null && displayName != user.DisplayName)
        {
            user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
            user.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (!string.IsNullOrEmpty(roleName) && await roles.RoleExistsAsync(roleName))
        {
            var currentRoles = await users.GetRolesAsync(user);
            if (!currentRoles.Contains(roleName))
            {
                if (currentRoles.Any())
                {
                    await users.RemoveFromRolesAsync(user, currentRoles);
                }
                await users.AddToRoleAsync(user, roleName);
            }
        }

        if (!changed)
        {
            return IdentityResult.Success;
        }
        return await users.UpdateAsync(user);
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

        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var userRoles = await users.GetRolesAsync(user);
        if (userRoles.Contains(AdminRole))
        {
            var adminRole = await roles.FindByNameAsync(AdminRole);
            if (adminRole is not null)
            {
                var adminCount = (await users.GetUsersInRoleAsync(AdminRole)).Count;
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

        return await users.DeleteAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        // Validate via Create + Remove approach to leverage IPasswordValidator pipeline
        var token = await users.GeneratePasswordResetTokenAsync(user);
        return await users.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<IdentityResult> UpdateProfileAsync(string username, string? displayName)
    {
        var user = await users.FindByNameAsync(username);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        return await users.UpdateAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(string username, string currentPassword, string newPassword)
    {
        var user = await users.FindByNameAsync(username);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await users.UpdateAsync(user);
        }
        return result;
    }

    public Task<SignInResult> PasswordSignInAsync(string username, string password, bool isPersistent)
    {
        return signIn.PasswordSignInAsync(username, password, isPersistent, lockoutOnFailure: false);
    }

    public async Task SignOutAsync()
    {
        await signIn.SignOutAsync();
    }

    public async Task<ApplicationUser?> GetUserByNameAsync(string username)
    {
        return await users.FindByNameAsync(username);
    }
}
