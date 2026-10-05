using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Services;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// 快照保留清理服务的测试桩工厂:供既有 ClusterService 手动构造点补参,
/// 配置为空(生效默认 90 天)、系统时钟,实际清理经固定作用域工厂使用传入的健康快照仓储。
/// </summary>
public static class RetentionStubs
{
    /// <summary>构造保留清理服务;配置为空,时间源为系统时钟,作用域内固定解析传入仓储。</summary>
    /// <param name="healthRepo">健康快照仓储(通常与被测 ClusterService 同一份)。</param>
    /// <returns>可直接传入 ClusterService 的保留清理服务。</returns>
    public static SnapshotRetentionService For(IClusterHealthRepository healthRepo)
        => new(StubScopeFactory.Of(healthRepo), new ConfigurationBuilder().Build(), TimeProvider.System, NullLogger<SnapshotRetentionService>.Instance);
}
