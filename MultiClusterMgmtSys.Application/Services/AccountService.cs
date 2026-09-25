using Microsoft.AspNetCore.Identity;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using System.Security.Claims;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 账号管理服务：面向管理员的账号增删改查、批量删除/批量改角色、密码重置与自助改密。
/// 操作者身份经 IHttpContextAccessor 从当前登录上下文解析,变更类操作成功后写审计日志。
/// </summary>
public class AccountService(
    UserManager<ApplicationUser> userManger,
    RoleManager<IdentityRole<int>> roleManager,
    IAccountQueryRepository accountQueryRepository,
    AuditService auditService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AccountService> logger)
{
    private const string AdminRole = "Admin";

    private const string MemberRole = "Member";

    private const string BuiltInAdminName = "admin";

    private const string DefaultPassword = "Changeme_123";

    private readonly UserManager<ApplicationUser> _userManager = userManger;

    private readonly RoleManager<IdentityRole<int>> _roleManager = roleManager;

    private readonly IAccountQueryRepository _accountQueryRepository = accountQueryRepository;

    private readonly AuditService _auditService = auditService;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private readonly ILogger<AccountService> _logger = logger;

    /// <summary>种子内置角色 Admin/Member 与内置管理员账号 admin(初始密码 Changeme_123,已存在则跳过),应用每次启动都会调用。</summary>
    public async Task CreateAdminAsync()
    {
        // Ensure roles
        if (!await _roleManager.RoleExistsAsync(AdminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole<int> { Name = AdminRole, NormalizedName = AdminRole.ToUpperInvariant() });
        }
        if (!await _roleManager.RoleExistsAsync(MemberRole))
        {
            await _roleManager.CreateAsync(new IdentityRole<int> { Name = MemberRole, NormalizedName = MemberRole.ToUpperInvariant() });
        }

        // Ensure admin
        if (await _userManager.FindByNameAsync("admin") is null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                CreatedAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(admin, DefaultPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, AdminRole);
            }
            else
            {
                _logger.LogError("Failed to create admin account: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        _logger.LogInformation("Create admin account succeeded");
    }

    /// <summary>分页查询账号列表,支持按用户名模糊搜索、按角色过滤,并按创建时间/用户名/最后登录时间排序(均以 Id 作次级稳定排序)。</summary>
    /// <param name="query">分页、搜索、过滤与排序参数,页码/页大小不合法时按最小值处理。</param>
    /// <returns>账号视图分页结果,每项附带该用户的首个角色名。</returns>
    public async Task<PagedResult<AccountViewModel>> GetPagedAccountsAsync(AccountQueryRequest query)
    {
        _logger.LogInformation("Querying accounts: search={SearchName}, role={RoleFilter}", query.SearchName, query.RoleFilter);
        var (users, total) = await _accountQueryRepository.GetPagedAsync(query);

        var vms = new List<AccountViewModel>(users.Count);
        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            vms.Add(user.ToAccountViewModel(userRoles.FirstOrDefault() ?? ""));
        }
        _logger.LogInformation("Account query done: total={Total}", total);
        return new PagedResult<AccountViewModel>(vms, total);
    }

    /// <summary>批量删除账号:跳过当前登录账号与内置管理员;删除 Admin 将导致系统无剩余管理员时,该批 Admin 全部跳过。成功删除数大于 0 时写审计。</summary>
    /// <param name="ids">待删除账号 ID 列表,为空直接返回零结果。</param>
    /// <returns>实际删除数与跳过数。</returns>
    public async Task<AccountBatchResult> BatchDeleteAsync(IReadOnlyList<int> ids)
    {
        _logger.LogInformation("Batch deleting accounts: count={Count}", ids.Count);
        if (ids.Count == 0) return new AccountBatchResult(0, 0);

        var skipSet = new HashSet<int> { GetCurrentUserId() };
        var builtIn = await _userManager.FindByNameAsync(BuiltInAdminName);
        if (builtIn is not null) skipSet.Add(builtIn.Id);

        var users = await _accountQueryRepository.GetByIdsAsync(ids);

        var adminUsers = await _userManager.GetUsersInRoleAsync(AdminRole);
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
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                processed++;
            }
            else
            {
                _logger.LogWarning("Batch delete failed for user {UserId}: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        _logger.LogInformation("Batch delete done: processed={Processed}, skipped={Skipped}", processed, users.Count - processed);
        if (processed > 0)
        {
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Delete, $"账号 {processed} 个");
        }
        return new AccountBatchResult(processed, users.Count - processed);
    }

    /// <summary>批量修改账号角色:先移除现有全部角色再赋予目标角色。角色不存在抛 <see cref="NotFoundException"/>;跳过当前登录账号与内置管理员,降级为 Member 时保证至少保留一名管理员。成功数大于 0 时写审计。</summary>
    public async Task<AccountBatchResult> BatchUpdateRoleAsync(BatchRoleUpdateRequest request)
    {
        var ids = request.Ids;
        var roleName = request.RoleName;
        _logger.LogInformation("Batch updating role: count={Count}, role={Role}", ids.Count, roleName);
        if (ids.Count == 0) return new AccountBatchResult(0, 0);
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            throw new NotFoundException($"角色 {roleName} 不存在");
        }

        var skipSet = new HashSet<int> { GetCurrentUserId() };
        var builtIn = await _userManager.FindByNameAsync(BuiltInAdminName);
        if (builtIn is not null) skipSet.Add(builtIn.Id);

        var users = await _accountQueryRepository.GetByIdsAsync(ids);

        if (roleName != AdminRole)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync(AdminRole);
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
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogWarning("Batch role change: failed to remove roles for user {UserId}: {Errors}",
                        user.Id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    continue;
                }
            }
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (addResult.Succeeded)
            {
                processed++;
            }
            else
            {
                _logger.LogWarning("Batch role change: failed to add role for user {UserId}: {Errors}",
                    user.Id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }
        _logger.LogInformation("Batch role change done: processed={Processed}, skipped={Skipped}", processed, users.Count - processed);
        if (processed > 0)
        {
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号 {processed} 个 → 角色 {roleName}");
        }
        return new AccountBatchResult(processed, users.Count - processed);
    }

    /// <summary>创建新账号并赋予指定角色;角色不存在时返回 InvalidRole 失败结果,成功后写创建审计。</summary>
    /// <param name="request">用户名、密码与角色名。</param>
    /// <returns>Identity 结果(错误文案为中文)。</returns>
    public async Task<IdentityResult> CreateAccountAsync(AccountCreateRequest request)
    {
        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRole",
                Description = $"角色 {request.RoleName} 不存在"
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            NormalizedUserName = request.UserName.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, request.RoleName);
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Create, $"账号: {request.UserName}");
        }
        return result;
    }

    /// <summary>更新账号信息并按需切换角色(先移除旧角色再赋予新角色);账号不存在或目标是内置管理员时返回失败结果,无论角色是否变化都写更新审计。</summary>
    public async Task<IdentityResult> UpdateAccountAsync(AccountUpdateRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
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
            _logger.LogWarning("Rejected update of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotModifyBuiltInAdmin",
                Description = "内置管理员不可修改"
            });
        }

        if (!string.IsNullOrEmpty(request.RoleName) && await _roleManager.RoleExistsAsync(request.RoleName))
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(request.RoleName))
            {
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, request.RoleName);
            }
        }

        await _auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {user.UserName}");
        return IdentityResult.Success;
    }

    /// <summary>删除单个账号。禁止删除内置管理员与当前登录账号,并保证系统至少保留一个 Admin;以上保护以失败结果返回,正常删除成功后写审计。</summary>
    public async Task<IdentityResult> DeleteAccountAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
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
            _logger.LogWarning("Rejected deletion of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteBuiltInAdmin",
                Description = "内置管理员不可删除"
            });
        }

        if (id == GetCurrentUserId())
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteSelf",
                Description = "不能删除当前登录账号"
            });
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains(AdminRole))
        {
            var adminRole = await _roleManager.FindByNameAsync(AdminRole);
            if (adminRole is not null)
            {
                var adminCount = (await _userManager.GetUsersInRoleAsync(AdminRole)).Count;
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

        var deleteResult = await _userManager.DeleteAsync(user);
        if (deleteResult.Succeeded)
        {
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Delete, $"账号: {user.UserName}");
        }
        return deleteResult;
    }

    /// <summary>管理员重置指定账号密码,经 Identity 重置令牌与密码策略校验;内置管理员不可重置,成功后写审计。</summary>
    public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
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
            _logger.LogWarning("Rejected password reset of built-in admin account");
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotModifyBuiltInAdmin",
                Description = "内置管理员不可修改"
            });
        }

        // Validate via Create + Remove approach to leverage IPasswordValidator pipeline
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {user.UserName} 重置密码");
        }
        return result;
    }

    /// <summary>当前登录用户自助修改密码;新旧密码相同抛 <see cref="ValidationException"/>,成功后更新用户 UpdatedAt 并写审计。</summary>
    public async Task<IdentityResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var username = GetCurrentUserName();
        var user = await _userManager.FindByNameAsync(username);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "账号不存在"
            });
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new ValidationException("新密码不能与当前密码相同");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Succeeded)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _auditService.LogAsync(AuditCategory.Account, AuditAction.Update, $"账号: {username} 修改密码");
        }
        return result;
    }

    /// <summary>按用户名查询单个账号(含其首个角色名);用户不存在返回 null。</summary>
    public async Task<AccountViewModel?> GetUserByNameAsync(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return user.ToAccountViewModel(roles.FirstOrDefault() ?? "");
    }

    private int GetCurrentUserId()
    {
        var idStr = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var id))
        {
            throw new PermissionException("无法获取当前登录账号信息");
        }
        return id;
    }

    private string GetCurrentUserName()
    {
        var name = _httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (string.IsNullOrEmpty(name))
        {
            throw new PermissionException("无法获取当前登录账号信息");
        }
        return name;
    }
}
