using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class AlertSettingServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");

    private readonly AppSettingRepository _repo;

    private readonly AlertSettingService _service;

    public AlertSettingServiceTests()
    {
        _repo = new AppSettingRepository(_harness.Db);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        _service = new AlertSettingService(
            _repo,
            configuration,
            TestHttpContext.For("admin", "Admin").Object,
            _harness.Audit,
            NullLogger<AlertSettingService>.Instance);
    }

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task GetSettings_returns_default_10_when_nothing_persisted()
    {
        var settings = await _service.GetAlertSettingsAsync();

        Assert.Equal(10, settings.OfflineThresholdMinutes);
    }

    [Fact]
    public async Task GetSettings_persisted_value_wins()
    {
        await _repo.SetAsync("Alert:OfflineThresholdMinutes", "30");

        var settings = await _service.GetAlertSettingsAsync();

        Assert.Equal(30, settings.OfflineThresholdMinutes);
    }

    [Fact]
    public async Task GetSettings_invalid_db_value_falls_back_to_config_then_default()
    {
        // DB 非法 → 配置层有合法值。
        var configWithAlert = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Alert:OfflineThresholdMinutes"] = "15"
            })
            .Build();
        var serviceWithConfig = new AlertSettingService(
            _repo,
            configWithAlert,
            TestHttpContext.For("admin", "Admin").Object,
            _harness.Audit,
            NullLogger<AlertSettingService>.Instance);
        await _repo.SetAsync("Alert:OfflineThresholdMinutes", "not-a-number");

        var fromConfig = await serviceWithConfig.GetAlertSettingsAsync();

        Assert.Equal(15, fromConfig.OfflineThresholdMinutes);

        // 配置层也没有 → 回退默认 10。
        var fallback = await _service.GetAlertSettingsAsync();

        Assert.Equal(10, fallback.OfflineThresholdMinutes);
    }

    [Fact]
    public async Task GetSettings_out_of_range_db_value_falls_back()
    {
        await _repo.SetAsync("Alert:OfflineThresholdMinutes", "0");

        var settings = await _service.GetAlertSettingsAsync();

        Assert.Equal(10, settings.OfflineThresholdMinutes);
    }

    [Fact]
    public async Task UpdateSettings_non_admin_throws_permission()
    {
        var service = new AlertSettingService(
            _repo,
            new ConfigurationBuilder().Build(),
            TestHttpContext.For("member").Object,
            _harness.Audit,
            NullLogger<AlertSettingService>.Instance);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => service.UpdateAlertSettingsAsync(new AlertSettingsUpdateRequest(15)));

        Assert.Contains("管理员", ex.UserMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public async Task UpdateSettings_out_of_range_threshold_throws_validation(int minutes)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAlertSettingsAsync(new AlertSettingsUpdateRequest(minutes)));

        Assert.Contains("离线持续时间阈值", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateSettings_persists_and_audits()
    {
        await _service.UpdateAlertSettingsAsync(new AlertSettingsUpdateRequest(20));

        var values = await _repo.GetByKeysAsync(["Alert:OfflineThresholdMinutes"]);
        Assert.Equal("20", values["Alert:OfflineThresholdMinutes"]);

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Cluster, audit.Category);
        Assert.Equal(AuditAction.Update, audit.Action);
        Assert.Contains("20", audit.Target);
        Assert.Contains("告警设置", audit.Target);
    }
}
