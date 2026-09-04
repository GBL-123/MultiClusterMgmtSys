using Microsoft.EntityFrameworkCore;
using MultiClusterMgmtSys.Data;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// 每个测试用例独立的 SQLite 内存库(与生产同 provider)。
/// </summary>
public static class SqliteDbFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}