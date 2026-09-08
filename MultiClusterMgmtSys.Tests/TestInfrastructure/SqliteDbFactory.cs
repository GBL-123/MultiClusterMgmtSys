using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class SqliteDbFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
