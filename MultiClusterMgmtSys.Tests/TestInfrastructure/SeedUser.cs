using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class SeedUser
{
    public const string DefaultPassword = "Test1234";

    public static (UserManager<ApplicationUser> Users, RoleManager<IdentityRole<int>> Roles) CreateManagers(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole<int>>()
            .AddUserStore<UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>()
            .AddRoleStore<RoleStore<IdentityRole<int>, ApplicationDbContext, int>>();

        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<RoleManager<IdentityRole<int>>>()
        );
    }

    public static async Task<ApplicationUser> AddUserAsync(
        ApplicationDbContext db,
        string userName,
        string password = DefaultPassword,
        params string[] roles)
    {
        var (users, roleManager) = CreateManagers(db);

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@test.local",
            CreatedAt = DateTime.UtcNow
        };

        var result = await users.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(';', result.Errors.Select(e => e.Description)));

        foreach (var role in roles)
        {
            await users.AddToRoleAsync(user, role);
        }

        return user;
    }
}
