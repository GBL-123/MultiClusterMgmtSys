using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Persistence;

public class AppSettingRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _db = SqliteDbFactory.CreateContext();
    private readonly AppSettingRepository _repo;

    public AppSettingRepositoryTests()
    {
        _repo = new AppSettingRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByKeysAsync_returns_only_requested_keys()
    {
        _db.AppSettings.Add(TestData.NewSetting("sync.enabled", "true"));
        _db.AppSettings.Add(TestData.NewSetting("sync.interval", "5"));
        _db.AppSettings.Add(TestData.NewSetting("other.key", "x"));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var values = await _repo.GetByKeysAsync(["sync.enabled", "sync.interval"]);

        Assert.Equal(2, values.Count);
        Assert.Equal("true", values["sync.enabled"]);
        Assert.Equal("5", values["sync.interval"]);
        Assert.False(values.ContainsKey("other.key"));
    }

    [Fact]
    public async Task GetByKeysAsync_returns_empty_for_missing_keys()
    {
        var values = await _repo.GetByKeysAsync(["missing.key"]);

        Assert.Empty(values);
    }

    [Fact]
    public async Task SetAsync_inserts_new_key()
    {
        await _repo.SetAsync("sync.enabled", "true");

        var values = await _repo.GetByKeysAsync(["sync.enabled"]);
        Assert.Equal("true", values["sync.enabled"]);
    }

    [Fact]
    public async Task SetAsync_updates_existing_key()
    {
        await _repo.SetAsync("sync.enabled", "true");
        await _repo.SetAsync("sync.enabled", "false");

        var values = await _repo.GetByKeysAsync(["sync.enabled"]);
        Assert.Equal("false", values["sync.enabled"]);

        var stored = _db.AppSettings.Single(s => s.Key == "sync.enabled");
        Assert.Single(_db.AppSettings);
        Assert.Equal("false", stored.Value);
    }
}
