using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterSyncSettingServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly AppSettingRepository repo;
    private readonly ClusterSyncSettingService service;

    public ClusterSyncSettingServiceTests()
    {
        repo = new AppSettingRepository(harness.Db);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        service = new ClusterSyncSettingService(
            repo,
            configuration,
            TestHttpContext.For("admin", "Admin").Object,
            harness.Audit,
            NullLogger<ClusterSyncSettingService>.Instance);
    }

    public void Dispose() => harness.Dispose();

    [Fact]
    public async Task GetSettings_returns_defaults_when_nothing_persisted()
    {
        var settings = await service.GetClusterSyncSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.Equal(5, settings.IntervalMinutes);
    }

    [Fact]
    public async Task GetSettings_persisted_values_win()
    {
        await repo.SetAsync("ClusterSync:Enabled", "false");
        await repo.SetAsync("ClusterSync:IntervalMinutes", "30");

        var settings = await service.GetClusterSyncSettingsAsync();

        Assert.False(settings.Enabled);
        Assert.Equal(30, settings.IntervalMinutes);
    }

    [Fact]
    public async Task GetSettings_invalid_db_interval_falls_back()
    {
        await repo.SetAsync("ClusterSync:Enabled", "true");
        await repo.SetAsync("ClusterSync:IntervalMinutes", "not-a-number");

        var settings = await service.GetClusterSyncSettingsAsync();

        Assert.Equal(5, settings.IntervalMinutes);
    }

    [Fact]
    public async Task UpdateSettings_non_admin_throws_permission()
    {
        var service = new ClusterSyncSettingService(
            repo,
            new ConfigurationBuilder().Build(),
            TestHttpContext.For("member").Object,
            harness.Audit,
            NullLogger<ClusterSyncSettingService>.Instance);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => service.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(true, 10)));

        Assert.Contains("管理员", ex.UserMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public async Task UpdateSettings_out_of_range_interval_throws_validation(int minutes)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(true, minutes)));

        Assert.Contains("同步间隔", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateSettings_persists_and_audits()
    {
        await service.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(false, 15));

        var values = await repo.GetByKeysAsync(["ClusterSync:Enabled", "ClusterSync:IntervalMinutes"]);
        Assert.Equal("False", values["ClusterSync:Enabled"]);
        Assert.Equal("15", values["ClusterSync:IntervalMinutes"]);

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Cluster, audit.Category);
        Assert.Equal(AuditAction.Update, audit.Action);
        Assert.Contains("15", audit.Target);
    }
}
