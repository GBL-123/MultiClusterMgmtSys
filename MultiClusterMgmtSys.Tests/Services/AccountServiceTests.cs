using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class AccountServiceTests
{
    private const string Password = "P@ssw0rd1";

    private async Task<(AccountService Service, UserManager<ApplicationUser> UserManager)> CreateAsync(MultiClusterMgmtSys.Data.ApplicationDbContext db, string? userName = "tester", int? userId = null)
    {
        var (userManager, roleManager, _) = await SeedUser.CreateIdentityAsync(db);
        var http = new TestServices.FakeHttpContextAccessor(userName, userId);
        var audit = new AuditService(new AuditLogRepository(db), http, NullLogger<AuditService>.Instance);
        var svc = new AccountService(userManager, roleManager, db, audit, http, NullLogger<AccountService>.Instance);
        return (svc, userManager);
    }

    [Fact]
    public async Task ChangePassword_NewSameAsCurrent_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.ChangePasswordAsync(new ChangePasswordRequest(Password, Password)));

        Assert.Equal("新密码不能与当前密码相同", ex.UserMessage);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_FailsWithMismatch()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var result = await svc.ChangePasswordAsync(new ChangePasswordRequest("WrongPass_1", "NewPass_123"));

        Assert.False(result.Succeeded);
        Assert.Equal("PasswordMismatch", result.Errors.First().Code);
    }

    [Fact]
    public async Task ChangePassword_Success_UpdatesPassword()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var result = await svc.ChangePasswordAsync(new ChangePasswordRequest(Password, "NewPass_123"));

        Assert.True(result.Succeeded);
        var check = await userManager.CheckPasswordAsync(await userManager.FindByNameAsync("tester"), "NewPass_123");
        Assert.True(check);
    }

    [Fact]
    public async Task ChangePassword_UnknownUser_ReturnsFailedResult()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, _) = await CreateAsync(db);

        var result = await svc.ChangePasswordAsync(new ChangePasswordRequest(Password, "NewPass_123"));

        Assert.False(result.Succeeded);
        Assert.Equal("UserNotFound", result.Errors.First().Code);
    }

    [Fact]
    public async Task ChangePassword_NoIdentityInContext_ThrowsPermission()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, _) = await CreateAsync(db, userName: null);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => svc.ChangePasswordAsync(new ChangePasswordRequest(Password, "NewPass_123")));

        Assert.Contains("当前登录", ex.UserMessage);
    }

    [Fact]
    public async Task DeleteAccount_Self_ReturnsCannotDeleteSelf()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (userManager, _, _) = await SeedUser.CreateIdentityAsync(db);
        var user = await SeedUser.CreateUserAsync(userManager, "tester", Password);
        var (svc, _) = await CreateAsync(db, userName: "tester", userId: user.Id);

        var result = await svc.DeleteAccountAsync(user.Id);

        Assert.False(result.Succeeded);
        Assert.Equal("CannotDeleteSelf", result.Errors.First().Code);
    }

    [Fact]
    public async Task GetUserByName_ReturnsViewModel_NotEntity()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password, role: "Member");

        var vm = await svc.GetUserByNameAsync("tester");

        Assert.NotNull(vm);
        Assert.Equal("tester", vm.UserName);
        Assert.Equal("Member", vm.RoleName);
    }
}