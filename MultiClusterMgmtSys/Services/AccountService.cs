using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Services;

public class AccountService(AccountRepository repo, PasswordHasher<string> hasher, ILogger<AccountService> logger)
{
    private readonly AccountRepository repo = repo;
    private readonly PasswordHasher<string> hasher = hasher;
    private readonly ILogger<AccountService> logger = logger;

    public async Task<Account?> ValidateCredentialsAsync(string username, string password)
    {
        var account = await repo.GetByUsernameAsync(username);
        if (account is null)
            return null;

        var result = hasher.VerifyHashedPassword("", account.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return account;
    }

    public ClaimsPrincipal CreateClaimsPrincipal(Account account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Role, account.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public async Task SeedAccountsAsync()
    {
        var count = await repo.CountAsync();
        if (count > 0)
            return;

        var hash = hasher.HashPassword("", "Changeme_123");

        var admin = new Account
        {
            Username = "admin",
            PasswordHash = hash,
            Role = AppRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(admin);

        var guest = new Account
        {
            Username = "guest",
            PasswordHash = hash,
            Role = AppRole.Guest,
            CreatedAt = DateTime.UtcNow
        };
        await repo.AddAsync(guest);

        logger.LogInformation("Seeded admin and guest accounts");
    }
}
