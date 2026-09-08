using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Data;

public class AppSettingRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext db = SqliteDbFactory.CreateContext();
    private readonly AppSettingRepository repo;

    public AppSettingRepositoryTests()
    {
        repo = new AppSettingRepository(db);
    }

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task GetByKeysAsync_returns_only_requested_keys()
    {
        db.AppSettings.Add(TestData.NewSetting("sync.enabled", "true"));
        db.AppSettings.Add(TestData.NewSetting("sync.interval", "5"));
        db.AppSettings.Add(TestData.NewSetting("other.key", "x"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var values = await repo.GetByKeysAsync(["sync.enabled", "sync.interval"]);

        Assert.Equal(2, values.Count);
        Assert.Equal("true", values["sync.enabled"]);
        Assert.Equal("5", values["sync.interval"]);
        Assert.False(values.ContainsKey("other.key"));
    }

    [Fact]
    public async Task GetByKeysAsync_returns_empty_for_missing_keys()
    {
        var values = await repo.GetByKeysAsync(["missing.key"]);

        Assert.Empty(values);
    }

    [Fact]
    public async Task SetAsync_inserts_new_key()
    {
        await repo.SetAsync("sync.enabled", "true");

        var values = await repo.GetByKeysAsync(["sync.enabled"]);
        Assert.Equal("true", values["sync.enabled"]);
    }

    [Fact]
    public async Task SetAsync_updates_existing_key()
    {
        await repo.SetAsync("sync.enabled", "true");
        await repo.SetAsync("sync.enabled", "false");

        var values = await repo.GetByKeysAsync(["sync.enabled"]);
        Assert.Equal("false", values["sync.enabled"]);

        var stored = db.AppSettings.Single(s => s.Key == "sync.enabled");
        Assert.Single(db.AppSettings);
        Assert.Equal("false", stored.Value);
    }
}
