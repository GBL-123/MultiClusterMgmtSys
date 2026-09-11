using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class AccountServiceBranchTests : IDisposable
{
    private readonly TestIdentity identity = TestIdentity.Create("admin", "Admin");
    private readonly AuditService audit;
    private readonly AccountService service;

    public AccountServiceBranchTests()
    {
        audit = new AuditService(
            new AuditLogRepository(identity.Db),
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuditService>.Instance);
        service = new AccountService(
            identity.Users,
            identity.Roles,
            identity.Db,
            audit,
            TestHttpContext.ForIdentity("admin", userId: 9999, "Admin").Object,
            NullLogger<AccountService>.Instance);
    }

    public void Dispose() => identity.Dispose();

    private async Task SeedUsersAsync()
    {
        await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" });
        var alpha = new ApplicationUser
        {
            UserName = "alpha",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var beta = new ApplicationUser
        {
            UserName = "beta",
            CreatedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await identity.Users.CreateAsync(alpha, "Passw0rd1");
        await identity.Users.CreateAsync(beta, "Passw0rd1");
        await identity.Users.AddToRoleAsync(alpha, "Member");
        await identity.Users.AddToRoleAsync(beta, "Member");
    }

    [Fact]
    public async Task GetPaged_sorts_by_user_name_ascending()
    {
        await SeedUsersAsync();

        var result = await service.GetPagedAccountsAsync(new AccountQueryRequest
        {
            SortBy = "UserName",
            SortDescending = false
        });

        Assert.Equal(["alpha", "beta"], result.Items.Select(i => i.UserName));
    }

    [Fact]
    public async Task GetPaged_sorts_by_last_login_descending()
    {
        await SeedUsersAsync();

        var result = await service.GetPagedAccountsAsync(new AccountQueryRequest
        {
            SortBy = "LastLoginAt",
            SortDescending = true
        });

        Assert.Equal("alpha", result.Items[0].UserName);
    }

    [Fact]
    public async Task GetPaged_unknown_role_filter_returns_empty()
    {
        await SeedUsersAsync();

        var result = await service.GetPagedAccountsAsync(new AccountQueryRequest { RoleFilter = "Ghost" });

        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Update_account_with_empty_role_keeps_roles()
    {
        await SeedUsersAsync();
        var alpha = await identity.Users.FindByNameAsync("alpha");

        var result = await service.UpdateAccountAsync(new AccountUpdateRequest(alpha!.Id, RoleName: ""));

        Assert.True(result.Succeeded);
        Assert.Equal(["Member"], await identity.Users.GetRolesAsync(alpha));
    }

    [Fact]
    public async Task Update_account_with_unknown_role_keeps_roles()
    {
        await SeedUsersAsync();
        var alpha = await identity.Users.FindByNameAsync("alpha");

        var result = await service.UpdateAccountAsync(new AccountUpdateRequest(alpha!.Id, "Ghost"));

        Assert.True(result.Succeeded);
        Assert.Equal(["Member"], await identity.Users.GetRolesAsync(alpha));
    }

    [Fact]
    public async Task Reset_password_weak_password_fails()
    {
        await SeedUsersAsync();
        var alpha = await identity.Users.FindByNameAsync("alpha");

        var result = await service.ResetPasswordAsync(new ResetPasswordRequest(alpha!.Id, "short"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Reset_password_missing_user_fails()
    {
        var result = await service.ResetPasswordAsync(new ResetPasswordRequest(9999, "NewPass12"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Batch_update_role_admin_to_member_removes_admin_role()
    {
        await service.CreateAdminAsync();
        await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" });
        var secondAdmin = new ApplicationUser { UserName = "admin2", CreatedAt = DateTime.UtcNow };
        await identity.Users.CreateAsync(secondAdmin, "Passw0rd1");
        await identity.Users.AddToRoleAsync(secondAdmin, "Admin");

        var result = await service.BatchUpdateRoleAsync(new BatchRoleUpdateRequest([secondAdmin.Id], "Member"));

        Assert.Equal(1, result.Processed);
        Assert.Equal(["Member"], await identity.Users.GetRolesAsync(secondAdmin));
    }

    [Fact]
    public async Task Batch_update_role_keeps_last_admin()
    {
        await service.CreateAdminAsync();
        var admin = await identity.Users.FindByNameAsync("admin");

        var result = await service.BatchUpdateRoleAsync(new BatchRoleUpdateRequest([admin!.Id], "Member"));

        Assert.Equal(0, result.Processed);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public async Task Batch_update_role_empty_ids_returns_zero()
    {
        var result = await service.BatchUpdateRoleAsync(new BatchRoleUpdateRequest([], "Admin"));

        Assert.Equal(0, result.Processed);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public async Task Batch_delete_empty_ids_returns_zero()
    {
        var result = await service.BatchDeleteAsync([]);

        Assert.Equal(0, result.Processed);
        Assert.Equal(0, result.Skipped);
    }

    [Fact]
    public async Task GetUserByName_returns_view_model_with_role()
    {
        await SeedUsersAsync();

        var vm = await service.GetUserByNameAsync("alpha");

        Assert.NotNull(vm);
        Assert.Equal("alpha", vm!.UserName);
        Assert.Equal("Member", vm.RoleName);
    }

    [Fact]
    public async Task Change_password_missing_user_fails()
    {
        var accessor = TestHttpContext.For("ghost-user").Object;
        var service2 = new AccountService(
            identity.Users, identity.Roles, identity.Db, audit, accessor, NullLogger<AccountService>.Instance);

        var result = await service2.ChangePasswordAsync(new ChangePasswordRequest("a", "b"));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Create_account_failure_returns_errors()
    {
        await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" });

        var result = await service.CreateAccountAsync(new AccountCreateRequest("weak-user", "short", "Member"));

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
    }
}
