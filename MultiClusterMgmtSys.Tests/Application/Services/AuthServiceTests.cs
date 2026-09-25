using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class AuthServiceTests : IDisposable
{
    private readonly TestIdentity _identity = TestIdentity.Create("admin", "Admin");
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member" }).Wait();
        var audit = new AuditService(
            new AuditLogRepository(_identity.Db),
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuditService>.Instance);
        _service = new AuthService(
            _identity.Users,
            _identity.SignIn,
            audit,
            TestHttpContext.For("admin", "Admin").Object,
            NullLogger<AuthService>.Instance);
    }

    public void Dispose() => _identity.Dispose();

    [Fact]
    public async Task RegisterAsync_creates_member_and_audits()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("newuser", "Passw0rd1", "Passw0rd1"));

        Assert.True(result.Succeeded);
        var user = await _identity.Users.FindByNameAsync("newuser");
        Assert.NotNull(user);
        Assert.Equal(["Member"], await _identity.Users.GetRolesAsync(user!));

        var audit = await _identity.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Register, audit.Action);
        Assert.Equal("newuser", audit.UserName);
    }

    [Fact]
    public async Task RegisterAsync_weak_password_fails_without_audit()
    {
        var result = await _service.RegisterAsync(new RegisterRequest("weak", "short", "short"));

        Assert.False(result.Succeeded);
        Assert.Empty(_identity.Db.AuditLogs);
    }

    [Fact]
    public async Task RegisterAsync_duplicate_fails()
    {
        await _service.RegisterAsync(new RegisterRequest("dup", "Passw0rd1", "Passw0rd1"));
        var second = await _service.RegisterAsync(new RegisterRequest("dup", "Passw0rd2", "Passw0rd1"));

        Assert.False(second.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_valid_credentials_succeeds_and_audits()
    {
        await _identity.Users.CreateAsync(
            new ApplicationUser { UserName = "login-user", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
        var request = new LoginRequest("login-user", "Passw0rd1", AutoLogin: false);

        var result = await _service.LoginAsync(request);

        Assert.True(result.Succeeded);
        var user = await _identity.Users.FindByNameAsync("login-user");
        Assert.NotNull(user!.LastLoginAt);
    }

    [Fact]
    public async Task LoginAsync_wrong_password_fails()
    {
        await _identity.Users.CreateAsync(
            new ApplicationUser { UserName = "bad-login", CreatedAt = DateTime.UtcNow }, "Passw0rd1");

        var result = await _service.LoginAsync(new LoginRequest("bad", "WrongPass9", AutoLogin: false));

        Assert.False(result.Succeeded);
        Assert.Empty(_identity.Db.AuditLogs);
    }

    [Fact]
    public async Task LogoutAsync_audits()
    {
        await _service.LogoutAsync();

        var audit = await _identity.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Logout, audit.Action);
        Assert.Equal("admin", audit.UserName);
    }
}
