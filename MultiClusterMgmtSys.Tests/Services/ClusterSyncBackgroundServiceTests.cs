using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterSyncBackgroundServiceTests
{
    private static IConfiguration Config(params (string Key, string Value)[] settings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => (string?)s.Value))
            .Build();

    private static ClusterSyncBackgroundService Service(IServiceScopeFactory? scopeFactory = null)
        => new(scopeFactory ?? Mock.Of<IServiceScopeFactory>(), NullLogger<ClusterSyncBackgroundService>.Instance);

    private static IServiceScopeFactory ScopeFactory(ApplicationDbContext db, Func<KubernetesClientConfiguration, IKubernetes> factory)
    {
        var provider = new ServiceCollection()
            .AddScoped(_ => db)
            .AddScoped<ClusterRepository>()
            .AddScoped(_ => TestServices.Cluster(db, factory))
            .BuildServiceProvider();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(s => s.ServiceProvider).Returns(provider);
        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        return factoryMock.Object;
    }

    [Fact]
    public async Task RunOnceAsync_K8sUnavailable_MarksAllClustersOfflineAndContinues()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.AddRange(
            TestData.NewCluster("alpha", status: ClusterStatus.Online),
            TestData.NewCluster("beta", status: ClusterStatus.Online));
        await db.SaveChangesAsync();
        var svc = Service(ScopeFactory(db, TestServices.ThrowingFactory()));

        var succeeded = await svc.RunOnceAsync();

        Assert.Equal(2, succeeded);
        var clusters = db.Clusters.OrderBy(c => c.Name).ToList();
        Assert.Equal(2, clusters.Count);
        Assert.All(clusters, c => Assert.Equal(ClusterStatus.Offline, c.Status));
        Assert.All(clusters, c => Assert.NotNull(c.LastCheckedAt));
    }

    [Fact]
    public async Task RunOnceAsync_WithSettings_ScheduledSourceAuditsFlip()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("down", status: ClusterStatus.Online));
        await db.SaveChangesAsync();
        var svc = Service(ScopeFactory(db, TestServices.ThrowingFactory()));

        await svc.RunOnceAsync();

        var entry = Assert.Single(db.AuditLogs.ToList());
        Assert.Contains("(定时同步)", entry.Target);
    }
}
