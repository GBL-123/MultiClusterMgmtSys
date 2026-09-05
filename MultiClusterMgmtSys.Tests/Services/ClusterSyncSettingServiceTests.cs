using Microsoft.Extensions.Configuration;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterSyncSettingServiceTests
{
    private static IConfiguration Config(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

    [Fact]
    public async Task Get_NoDbRowsNoConfig_ReturnsDefaults()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config());

        var settings = await svc.GetClusterSyncSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.Equal(5, settings.IntervalMinutes);
    }

    [Fact]
    public async Task Get_DbRowsOverrideConfig()
    {
        using var db = SqliteDbFactory.CreateContext();
        await db.AppSettings.AddRangeAsync(
            new MultiClusterMgmtSys.Data.Entities.AppSetting { Key = "ClusterSync:Enabled", Value = "False" },
            new MultiClusterMgmtSys.Data.Entities.AppSetting { Key = "ClusterSync:IntervalMinutes", Value = "10" });
        await db.SaveChangesAsync();
        var svc = TestServices.SyncSetting(db, Config(("ClusterSync:Enabled", "true")));

        var settings = await svc.GetClusterSyncSettingsAsync();

        Assert.False(settings.Enabled);
        Assert.Equal(10, settings.IntervalMinutes);
    }

    [Fact]
    public async Task Get_InvalidDbValue_FallsBackToConfigOrDefault()
    {
        using var db = SqliteDbFactory.CreateContext();
        await db.AppSettings.AddRangeAsync(
            new MultiClusterMgmtSys.Data.Entities.AppSetting { Key = "ClusterSync:Enabled", Value = "not-a-bool" },
            new MultiClusterMgmtSys.Data.Entities.AppSetting { Key = "ClusterSync:IntervalMinutes", Value = "0" });
        await db.SaveChangesAsync();
        var svc = TestServices.SyncSetting(db, Config(("ClusterSync:IntervalMinutes", "15")));

        var settings = await svc.GetClusterSyncSettingsAsync();

        Assert.True(settings.Enabled);
        Assert.Equal(15, settings.IntervalMinutes);
    }

    [Fact]
    public async Task Get_InvalidConfigValue_FallsBackToDefault()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config(("ClusterSync:IntervalMinutes", "abc")));

        var settings = await svc.GetClusterSyncSettingsAsync();

        Assert.Equal(5, settings.IntervalMinutes);
    }

    [Fact]
    public async Task Update_Admin_PersistsAndAudits()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config(), "tester", "Admin");

        await svc.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(false, 30));

        var rows = db.AppSettings.ToList().ToDictionary(s => s.Key, s => s.Value);
        Assert.Equal("False", rows["ClusterSync:Enabled"]);
        Assert.Equal("30", rows["ClusterSync:IntervalMinutes"]);
        var entry = Assert.Single(db.AuditLogs.ToList());
        Assert.Equal(AuditCategory.Cluster, entry.Category);
        Assert.Equal(AuditAction.Update, entry.Action);
        Assert.Contains("间隔 30 分钟", entry.Target);
        Assert.Contains("停用", entry.Target);
    }

    [Fact]
    public async Task Update_Member_ThrowsPermission()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config(), "tester", "Member");

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => svc.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(true, 10)));

        Assert.Contains("仅管理员", ex.UserMessage);
        Assert.Empty(db.AppSettings.ToList());
    }

    [Fact]
    public async Task Update_NoIdentity_ThrowsPermission()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config());

        await Assert.ThrowsAsync<PermissionException>(
            () => svc.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(true, 10)));

        Assert.Empty(db.AppSettings.ToList());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public async Task Update_IntervalOutOfRange_ThrowsValidation(int minutes)
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.SyncSetting(db, Config(), "tester", "Admin");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.UpdateClusterSyncSettingsAsync(new ClusterSyncSettingsUpdateRequest(true, minutes)));

        Assert.Contains("1~1440", ex.UserMessage);
        Assert.Empty(db.AppSettings.ToList());
    }
}
