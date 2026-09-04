using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class AccountServiceTests
{
    private const string Password = "P@ssw0rd1";

    private async Task<(AccountService Service, UserManager<ApplicationUser> UserManager)> CreateAsync(MultiClusterMgmtSys.Data.ApplicationDbContext db)
    {
        var (userManager, roleManager, signInManager) = await SeedUser.CreateIdentityAsync(db);
        var audit = new AuditService(new AuditLogRepository(db), new TestServices.NullHttpContextAccessor(), NullLogger<AuditService>.Instance);
        var svc = new AccountService(userManager, roleManager, signInManager, db, audit, NullLogger<AccountService>.Instance);
        return (svc, userManager);
    }

    [Fact]
    public async Task ChangePassword_NewSameAsCurrent_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.ChangePasswordAsync("tester", Password, Password));

        Assert.Equal("新密码不能与当前密码相同", ex.UserMessage);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_FailsWithMismatch()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var result = await svc.ChangePasswordAsync("tester", "WrongPass_1", "NewPass_123");

        Assert.False(result.Succeeded);
        Assert.Equal("PasswordMismatch", result.Errors.First().Code);
    }

    [Fact]
    public async Task ChangePassword_Success_UpdatesPassword()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, userManager) = await CreateAsync(db);
        await SeedUser.CreateUserAsync(userManager, "tester", Password);

        var result = await svc.ChangePasswordAsync("tester", Password, "NewPass_123");

        Assert.True(result.Succeeded);
        var check = await userManager.CheckPasswordAsync(await userManager.FindByNameAsync("tester"), "NewPass_123");
        Assert.True(check);
    }

    [Fact]
    public async Task ChangePassword_UnknownUser_ReturnsFailedResult()
    {
        using var db = SqliteDbFactory.CreateContext();
        var (svc, _) = await CreateAsync(db);

        var result = await svc.ChangePasswordAsync("nobody", Password, "NewPass_123");

        Assert.False(result.Succeeded);
        Assert.Equal("UserNotFound", result.Errors.First().Code);
    }
}