using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestIdentity identity = TestIdentity.Create("admin", "Admin");
    private readonly AuthService service;

    public AuthServiceTests()
    {
        identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" }).Wait();
        var audit = new AuditService(
            new AuditLogRepository(identity.Db),
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuditService>.Instance);
        service = new AuthService(
            identity.Users,
            identity.SignIn,
            audit,
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuthService>.Instance);
    }

    public void Dispose() => identity.Dispose();

    [Fact]
    public async Task RegisterAsync_creates_member_and_audits()
    {
        var result = await service.RegisterAsync(new RegisterRequest("newuser", "Passw0rd1", "Passw0rd1"));

        Assert.True(result.Succeeded);
        var user = await identity.Users.FindByNameAsync("newuser");
        Assert.NotNull(user);
        Assert.Equal(["Member"], await identity.Users.GetRolesAsync(user!));

        var audit = await identity.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Register, audit.Action);
        Assert.Equal("newuser", audit.UserName);
    }

    [Fact]
    public async Task RegisterAsync_weak_password_fails_without_audit()
    {
        var result = await service.RegisterAsync(new RegisterRequest("weak", "short", "short"));

        Assert.False(result.Succeeded);
        Assert.Empty(identity.Db.AuditLogs);
    }

    [Fact]
    public async Task RegisterAsync_duplicate_fails()
    {
        await service.RegisterAsync(new RegisterRequest("dup", "Passw0rd1", "Passw0rd1"));
        var second = await service.RegisterAsync(new RegisterRequest("dup", "Passw0rd2", "Passw0rd1"));

        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_valid_credentials_succeeds_and_audits()
    {
        await identity.Users.CreateAsync(
            new ApplicationUser { UserName = "login-user", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
        var request = new LoginRequest("login-user", "Passw0rd1", AutoLogin: false);

        var result = await service.LoginAsync(request);

        Assert.True(result.Succeeded);
        var user = await identity.Users.FindByNameAsync("login-user");
        Assert.NotNull(user!.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_wrong_password_fails()
    {
        await identity.Users.CreateAsync(
            new ApplicationUser { UserName = "bad-login", CreatedAt = DateTime.UtcNow }, "Passw0rd1");

        var result = await service.LoginAsync(new LoginRequest("bad", "WrongPass9", AutoLogin: false));

        Assert.False(result.Succeeded);
        Assert.Empty(identity.Db.AuditLogs);
    }

    [Fact]
    public async Task LogoutAsync_audits()
    {
        await service.LogoutAsync();

        var audit = await identity.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Logout, audit.Action);
        Assert.Equal("admin", audit.UserName);
    }
}
