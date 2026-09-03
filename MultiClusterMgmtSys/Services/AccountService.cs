using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Services;

public class AccountService(
    UserManager<ApplicationUser> userManger,
    RoleManager<IdentityRole<int>> roleManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext db,
    AuditService auditService,
    ILogger<AccountService> logger)
{
    private const string AdminRole = "Admin";
    private const string MemberRole = "Member";
    private const string BuiltInAdminName = "admin";
    private const string DefaultPassword = "Changeme_123";

    private readonly UserManager<ApplicationUser> userManager = userManger;
    private readonly RoleManager<IdentityRole<int>> roleManager = roleManager;
    private readonly SignInManager<ApplicationUser> signInManager = signInManager;
    private readonly ApplicationDbContext db = db;
    private readonly AuditService auditService = auditService;
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

    public async Task<PagedResult<AccountViewModel>> GetPagedAccountsAsync(TableState state, AccountQueryRequest query)
    {
        logger.LogInformation("Querying accounts: search={SearchName}, role={RoleFilter}", query.SearchName, query.RoleFilter);
        var page = state.Page > 0 ? state.Page + 1 : Math.Max(query.Page, 1);
        var pageSize = Math.Max(state.PageSize > 0 ? state.PageSize : query.PageSize, 1);
        var sortDescending = state.SortDirection != SortDirection.Ascending;

        IQueryable<ApplicationUser> q = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchName))
        {
            var search = query.SearchName.Trim();
            q = q.Where(u => u.UserName != null && u.UserName.Contains(search));
        }

        if (!string.IsNullOrEmpty(query.RoleFilter))
        {
            var roleId = await roleManager.Roles
                .Where(r => r.NormalizedName == query.RoleFilter.ToUpperInvariant())
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();
            if (roleId is null)
            {
                return new PagedResult<AccountViewModel>([], 0);
            }
            var userIdsInRole = await db.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();
            q = q.Where(u => userIdsInRole.Contains(u.Id));
        }

        var total = await q.CountAsync();
        var users = await (state.SortLabel switch
        {
            "UserName" => sortDescending
                ? q.OrderByDescending(u => u.UserName)
                : q.OrderBy(u => u.UserName),
            "LastLoginAt" => sortDescending
                ? q.OrderByDescending(u => u.LastLoginAt)
                : q.OrderBy(u => u.LastLoginAt),
            _ => sortDescending
                ? q.OrderByDescending(u => u.CreatedAt)
                : q.OrderBy(u => u.CreatedAt)
        })
        .ThenBy(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        var vms = new List<AccountViewModel>(users.Count);
        foreach (var user in users)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            vms.Add(user.ToAccountViewModel(userRoles.FirstOrDefault() ?? ""));
        }
        logger.LogInformation("Account query done: total={Total}", total);
        return new PagedResult<AccountViewModel>(vms, total);
    }

    public async Task<AccountBatchResult> BatchDeleteAsync(IReadOnlyList<int> ids, int currentUserId)
    {
        logger.LogInformation("Batch deleting accounts: count={Count}", ids.Count);
        if (ids.Count == 0) return new AccountBatchResult(0, 0);

        var skipSet = new HashSet<int> { currentUserId };
        var builtIn = await userManager.FindByNameAsync(BuiltInAdminName);
        if (builtIn is not null) skipSet.Add(builtIn.Id);

        var users = await userManager.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

        var adminUsers = await userManager.GetUsersInRoleAsync(AdminRole);
        var adminIds = adminUsers.Select(u => u.Id).ToHashSet();
        var adminCandidates = users.Where(u => adminIds.Contains(u.Id)).Select(u => u.Id).ToList();
        if (adminCandidates.Count > 0 && adminUsers.Count - adminCandidates.Count < 1)
        {
            foreach (var id in adminCandidates)
            {
                skipSet.Add(id);
            }
        }

        var processed = 0;
        foreach (var user in users)
        {
            if (skipSet.Contains(user.Id)) continue;
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                processed++;
            }
            else
            {
                logger.LogWarning("Batch delete failed for user {UserId}: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        logger.LogInformation("Batch delete done: processed={Processed}, skipped={Skipped}", processed, users.Count - processed);
        if (processed > 0)
        {
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Delete, $"账号 {processed} 个");
        }
        return new AccountBatchResult(processed, users.Count - processed);
    }

    public async Task<AccountBatchResult> BatchUpdateRoleAsync(IReadOnlyList<int> ids, int currentUserId, string roleName)
    {
        logger.LogInformation("Batch updating role: count={Count}, role={Role}", ids.Count, roleName);
        if (ids.Count == 0) return new AccountBatchResult(0, 0);
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            throw new NotFoundException($"角色 {roleName} 不存在");
        }

        var skipSet = new HashSet<int> { currentUserId };
        var builtIn = await userManager.FindByNameAsync(BuiltInAdminName);
        if (builtIn is not null) skipSet.Add(builtIn.Id);

        var users = await userManager.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

        if (roleName != AdminRole)
        {
            var adminUsers = await userManager.GetUsersInRoleAsync(AdminRole);
            var adminIds = adminUsers.Select(u => u.Id).ToHashSet();
            var adminCandidates = users.Where(u => adminIds.Contains(u.Id)).Select(u => u.Id).ToList();
            if (adminCandidates.Count > 0 && adminUsers.Count - adminCandidates.Count < 1)
            {
                foreach (var id in adminCandidates)
                {
                    skipSet.Add(id);
                }
            }
        }

        var processed = 0;
        foreach (var user in users)
        {
            if (skipSet.Contains(user.Id)) continue;
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    logger.LogWarning("Batch role change: failed to remove roles for user {UserId}: {Errors}",
                        user.Id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            var addResult = await userManager.AddToRoleAsync(user, roleName);
            if (addResult.Succeeded)
            {
                processed++;
            }
            else
            {
                logger.LogWarning("Batch role change: failed to add role for user {UserId}: {Errors}",
                    user.Id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }
        logger.LogInformation("Batch role change done: processed={Processed}, skipped={Skipped}", processed, users.Count - processed);
        if (processed > 0)
        {
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号 {processed} 个 → 角色 {roleName}");
        }
        return new AccountBatchResult(processed, users.Count - processed);
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
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Create, $"账号: {username}");
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

        if (user.UserName == BuiltInAdminName)
        {
            logger.LogWarning("Rejected update of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotModifyBuiltInAdmin",
                Description = "内置管理员不可修改"
            });
        }

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

        await auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {user.UserName}");
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAccountAsync(int id, int currentUserId)
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

        if (user.UserName == BuiltInAdminName)
        {
            logger.LogWarning("Rejected deletion of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteBuiltInAdmin",
                Description = "内置管理员不可删除"
            });
        }

        if (id == currentUserId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteSelf",
                Description = "不能删除当前登录账号"
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

        var deleteResult = await userManager.DeleteAsync(user);
        if (deleteResult.Succeeded)
        {
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Delete, $"账号: {user.UserName}");
        }
        return deleteResult;
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

        if (user.UserName == BuiltInAdminName)
        {
            logger.LogWarning("Rejected password reset of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotModifyBuiltInAdmin",
                Description = "内置管理员不可修改"
            });
        }

        // Validate via Create + Remove approach to leverage IPasswordValidator pipeline
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (result.Succeeded)
        {
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {user.UserName} 重置密码");
        }
        return result;
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

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            throw new ValidationException("新密码不能与当前密码相同");
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            await auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {username} 修改密码");
        }
        return result;
    }


    

    public async Task<ApplicationUser?> GetUserByNameAsync(string username)
    {
        return await userManager.FindByNameAsync(username);
    }
}
