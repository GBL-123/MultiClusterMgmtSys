using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Application.Services;

/// <summary>
/// 快照保留清理服务测试:配置解析(默认/覆写/非法回退)、进程内限频与失败静默。
/// </summary>
public sealed class SnapshotRetentionServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IClusterHealthRepository> _repo = new();

    private readonly StubTimeProvider _time = new(Now);

    private readonly List<DateTime> _cutoffs = [];

    private SnapshotRetentionService NewService(Dictionary<string, string?>? config = null)
    {
        _cutoffs.Clear();
        _repo.Reset();
        _repo
            .Setup(r => r.DeleteCapturedBeforeAsync(It.IsAny<DateTime>()))
            .Callback<DateTime>(cutoff => _cutoffs.Add(cutoff))
            .ReturnsAsync(2);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config ?? [])
            .Build();
        return new SnapshotRetentionService(StubScopeFactory.Of(_repo.Object), configuration, _time, NullLogger<SnapshotRetentionService>.Instance);
    }

    [Fact]
    public async Task Cleanup_uses_default_90_days_when_unconfigured()
    {
        var service = NewService();

        var deleted = await service.CleanupIfDueAsync();

        Assert.Equal(2, deleted);
        var cutoff = Assert.Single(_cutoffs);
        Assert.Equal(Now.AddDays(-90), cutoff);
    }

    [Fact]
    public async Task Cleanup_uses_configured_30_days()
    {
        var service = NewService(new Dictionary<string, string?>
        {
            ["ClusterSync:SnapshotRetentionDays"] = "30"
        });

        await service.CleanupIfDueAsync();

        Assert.Equal(Now.AddDays(-30), Assert.Single(_cutoffs));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("abc")]
    public async Task Cleanup_falls_back_to_default_on_invalid_config(string raw)
    {
        var service = NewService(new Dictionary<string, string?>
        {
            ["ClusterSync:SnapshotRetentionDays"] = raw
        });

        await service.CleanupIfDueAsync();

        Assert.Equal(Now.AddDays(-90), Assert.Single(_cutoffs));
    }

    [Fact]
    public async Task Cleanup_throttles_until_throttle_window_elapses()
    {
        var service = NewService();

        Assert.Equal(2, await service.CleanupIfDueAsync());
        Assert.Null(await service.CleanupIfDueAsync());
        Assert.Single(_cutoffs);

        _time.Advance(TimeSpan.FromMinutes(61));
        Assert.Equal(2, await service.CleanupIfDueAsync());
        Assert.Equal(2, _cutoffs.Count);
    }

    [Fact]
    public async Task Cleanup_swallows_repository_failures()
    {
        var service = NewService();
        _repo
            .Setup(r => r.DeleteCapturedBeforeAsync(It.IsAny<DateTime>()))
            .ThrowsAsync(new IOException("disk busy"));

        var deleted = await service.CleanupIfDueAsync();

        Assert.Equal(0, deleted);
    }
}
