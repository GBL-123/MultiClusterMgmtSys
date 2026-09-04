using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests;

public class SmokeTests
{
    [Fact]
    public async Task SqliteFactory_CreatesSchema()
    {
        using var db = SqliteDbFactory.CreateContext();

        var id = db.Clusters.Count();
        db.Clusters.Add(TestData.NewCluster("smoke"));
        await db.SaveChangesAsync();

        Assert.Equal(1, db.Clusters.Count(c => c.Name == "smoke"));
        Assert.True(db.Database.IsSqlite());
    }
}