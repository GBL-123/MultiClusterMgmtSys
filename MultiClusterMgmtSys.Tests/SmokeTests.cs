using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests;

public class SmokeTests
{
    [Fact]
    public async Task SqliteDbFactory_creates_schema()
    {
        using var db = SqliteDbFactory.CreateContext();

        Assert.True(await db.Database.CanConnectAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(db.Clusters);
    }

    [Fact]
    public async Task SeedUser_creates_user_with_role()
    {
        using var db = SqliteDbFactory.CreateContext();

        var user = await SeedUser.AddUserAsync(db, "smoke-user", roles: "Admin");

        Assert.NotNull(await db.Users.FindAsync(user.Id, TestContext.Current.CancellationToken));
        var roles = await db.UserRoles
            .Where(r => r.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(["Admin"], roles);
    }
}
