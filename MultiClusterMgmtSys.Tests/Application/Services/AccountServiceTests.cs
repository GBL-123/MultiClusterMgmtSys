using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class AccountServiceTests : IDisposable
{
    private readonly TestIdentity _identity = TestIdentity.Create("admin", "Admin");
    private readonly AuditService _audit;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _audit = new AuditService(
            new AuditLogRepository(_identity.Db),
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuditService>.Instance);
        _service = new AccountService(
            _identity.Users,
            _identity.Roles,
            new AccountQueryRepository(_identity.Db),
            _audit,
            TestHttpContext.ForIdentity("admin", userId: 9999, "Admin").Object,
            NullLogger<AccountService>.Instance);
    }

    public void Dispose() => _identity.Dispose();

    private ApplicationDbContext Db => _identity.Db;

    [Fact]
    public async Task CreateAdminAsync_seeds_roles_and_builtin_admin()
    {
        await _service.CreateAdminAsync();

        Assert.True(await _identity.Roles.RoleExistsAsync("Admin"));
        Assert.True(await _identity.Roles.RoleExistsAsync("Member"));
        var admin = await _identity.Users.FindByNameAsync("admin");
        Assert.NotNull(admin);
        Assert.Equal(["Admin"], await _identity.Users.GetRolesAsync(admin!));
    }

    [Fact]
    public async Task CreateAdminAsync_is_idempotent()
    {
        await _service.CreateAdminAsync();
        await _service.CreateAdminAsync();

        Assert.Equal(1, await Db.Users.CountAsync(u => u.UserName == "admin", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetPagedAccountsAsync_returns_roles_and_paging()
    {
        await _service.CreateAdminAsync();
        await _identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" });
        var member = new ApplicationUser { UserName = "beta", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");
        await _identity.Users.AddToRoleAsync(member, "Member");

        var result = await _service.GetPagedAccountsAsync(new AccountQueryRequest { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.Total);
        var roles = result.Items.ToDictionary(v => v.UserName, v => v.RoleName);
        Assert.Equal("Admin", roles["admin"]);
        Assert.Equal("Member", roles["beta"]);
    }

    [Fact]
    public async Task GetPagedAccountsAsync_role_filter_and_search()
    {
        await _service.CreateAdminAsync();
        await _identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" });
        var member = new ApplicationUser { UserName = "member-1", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");
        await _identity.Users.AddToRoleAsync(member, "Member");

        var byRole = await _service.GetPagedAccountsAsync(new AccountQueryRequest { RoleFilter = "Member" });
        Assert.Equal(1, byRole.Total);

        var bySearch = await _service.GetPagedAccountsAsync(new AccountQueryRequest { SearchName = "member" });
        Assert.Equal(1, bySearch.Total);
    }

    [Fact]
    public async Task BatchDeleteAsync_skips_builtin_admin_and_current_user()
    {
        await _service.CreateAdminAsync();
        var admin = await _identity.Users.FindByNameAsync("admin");
        var member = new ApplicationUser { UserName = "victim", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");

        var accessor = TestHttpContext.ForIdentity("admin", userId: admin!.Id, "Admin").Object;
        var service2 = new AccountService(
            _identity.Users, _identity.Roles, new AccountQueryRepository(_identity.Db), _audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.BatchDeleteAsync([admin.Id, member.Id]);

        Assert.Equal(1, result.Processed);
        Assert.Equal(1, result.Skipped);
        Assert.Null(await _identity.Users.FindByIdAsync(member.Id.ToString()));
        var auditLog = Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Delete, auditLog.Action);
    }

    [Fact]
    public async Task BatchDeleteAsync_never_removes_last_admin()
    {
        await _service.CreateAdminAsync();
        var admin = await _identity.Users.FindByNameAsync("admin");

        var result = await _service.BatchDeleteAsync([admin!.Id]);

        Assert.Equal(0, result.Processed);
        Assert.Equal(1, result.Skipped);
        Assert.NotNull(await _identity.Users.FindByIdAsync(admin.Id.ToString()));
    }

    [Fact]
    public async Task BatchUpdateRoleAsync_unknown_role_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.BatchUpdateRoleAsync(new BatchRoleUpdateRequest([1], "Ghost")));
    }

    [Fact]
    public async Task BatchUpdateRoleAsync_changes_roles_and_audits()
    {
        await _service.CreateAdminAsync();
        await _identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" });
        var member = new ApplicationUser { UserName = "promotee", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");

        var accessor = TestHttpContext.ForIdentity("someone", userId: 7777).Object;
        var service2 = new AccountService(
            _identity.Users, _identity.Roles, new AccountQueryRepository(_identity.Db), _audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.BatchUpdateRoleAsync(new BatchRoleUpdateRequest([member.Id], "Admin"));

        Assert.Equal(1, result.Processed);
        Assert.Equal(["Admin"], await _identity.Users.GetRolesAsync(member));
        Assert.Equal(AuditAction.Update, Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task CreateAccountAsync_unknown_role_returns_failed_result()
    {
        var result = await _service.CreateAccountAsync(new AccountCreateRequest("ghost-user", "Passw0rd1", "Ghost"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description!.Contains("角色"));
    }

    [Fact]
    public async Task CreateAccountAsync_success_creates_with_role_and_audits()
    {
        await _service.CreateAdminAsync();

        var result = await _service.CreateAccountAsync(new AccountCreateRequest("new-admin", "Passw0rd1", "Admin"));

        Assert.True(result.Succeeded);
        var user = await _identity.Users.FindByNameAsync("new-admin");
        Assert.Equal(["Admin"], await _identity.Users.GetRolesAsync(user!));
        Assert.Equal(AuditAction.Create, Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task UpdateAccountAsync_builtin_admin_rejected()
    {
        await _service.CreateAdminAsync();
        var admin = await _identity.Users.FindByNameAsync("admin");

        var result = await _service.UpdateAccountAsync(new AccountUpdateRequest(admin!.Id, "Member"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description!.Contains("内置管理员"));
    }

    [Fact]
    public async Task UpdateAccountAsync_missing_user_fails()
    {
        var result = await _service.UpdateAccountAsync(new AccountUpdateRequest(999, "Member"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UpdateAccountAsync_changes_role_and_audits()
    {
        await _service.CreateAdminAsync();
        await _identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" });
        var member = new ApplicationUser { UserName = "up-target", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");
        await _identity.Users.AddToRoleAsync(member, "Member");

        var result = await _service.UpdateAccountAsync(new AccountUpdateRequest(member.Id, "Admin"));

        Assert.True(result.Succeeded);
        Assert.Equal(["Admin"], await _identity.Users.GetRolesAsync(member));
        Assert.Equal(AuditAction.Update, Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task DeleteAccountAsync_builtin_admin_rejected()
    {
        await _service.CreateAdminAsync();
        var admin = await _identity.Users.FindByNameAsync("admin");

        var result = await _service.DeleteAccountAsync(admin!.Id);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description!.Contains("内置管理员"));
    }

    [Fact]
    public async Task DeleteAccountAsync_self_rejected()
    {
        var member = new ApplicationUser { UserName = "self-user", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");
        var accessor = TestHttpContext.ForIdentity("self-user", userId: member.Id).Object;
        var service2 = new AccountService(
            _identity.Users, _identity.Roles, new AccountQueryRepository(_identity.Db), _audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.DeleteAccountAsync(member.Id);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CannotDeleteSelf");
    }

    [Fact]
    public async Task DeleteAccountAsync_removes_member_and_audits()
    {
        await _service.CreateAdminAsync();
        var member = new ApplicationUser { UserName = "doomed", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(member, "Passw0rd1");

        var result = await _service.DeleteAccountAsync(member.Id);

        Assert.True(result.Succeeded);
        Assert.Equal(AuditAction.Delete, Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task ResetPasswordAsync_builtin_admin_rejected()
    {
        await _service.CreateAdminAsync();
        var admin = await _identity.Users.FindByNameAsync("admin");

        var result = await _service.ResetPasswordAsync(new ResetPasswordRequest(admin!.Id, "NewPass123"));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description!.Contains("内置管理员"));
    }

    [Fact]
    public async Task ResetPasswordAsync_resets_password_for_regular_user()
    {
        await _service.CreateAdminAsync();
        var user = new ApplicationUser { UserName = "reset-target", CreatedAt = DateTime.UtcNow };
        await _identity.Users.CreateAsync(user, "Passw0rd1");

        var result = await _service.ResetPasswordAsync(new ResetPasswordRequest(user.Id, "NewPass9"));

        Assert.True(result.Succeeded);
        Assert.True(await _identity.Users.CheckPasswordAsync(user, "NewPass9"));
    }

    [Fact]
    public async Task ChangePasswordAsync_same_passwords_throws_validation()
    {
        await _service.CreateAdminAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.ChangePasswordAsync(new ChangePasswordRequest("same", "same")));

        Assert.Contains("相同", ex.UserMessage);
    }

    [Fact]
    public async Task ChangePasswordAsync_success_changes_and_audits()
    {
        await _service.CreateAdminAsync();
        var accessor = TestHttpContext.For("admin", "Admin").Object;
        var service2 = new AccountService(
            _identity.Users, _identity.Roles, new AccountQueryRepository(_identity.Db), _audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.ChangePasswordAsync(new ChangePasswordRequest("Changeme_123", "Changed_9x"));

        Assert.True(result.Succeeded);
        var admin = await _identity.Users.FindByNameAsync("admin");
        Assert.True(await _identity.Users.CheckPasswordAsync(admin!, "Changed_9x"));
        Assert.Equal(AuditAction.Update, Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task ChangePasswordAsync_wrong_current_password_fails()
    {
        await _service.CreateAdminAsync();
        var accessor = TestHttpContext.For("admin", "Admin").Object;
        var service2 = new AccountService(
            _identity.Users, _identity.Roles, new AccountQueryRepository(_identity.Db), _audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.ChangePasswordAsync(new ChangePasswordRequest("wrong-pass", "Changed_9x"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GetUserByNameAsync_returns_null_for_missing()
    {
        Assert.Null(await _service.GetUserByNameAsync("no-such-user"));
    }
}
