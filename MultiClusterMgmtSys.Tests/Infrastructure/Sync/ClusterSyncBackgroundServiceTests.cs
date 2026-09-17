using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using Microsoft.AspNetCore.Http;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using Moq;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using k8s;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Sync;

public class ClusterSyncBackgroundServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();

    public ClusterSyncBackgroundServiceTests()
    {
        harness.Db.ClusterGroups.Add(TestData.NewGroup("g1"));
        harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken).Wait();
        harness.ClusterRepo.AddAsync(TestData.NewCluster("sync-1")).Wait();
        harness.ClusterRepo.AddAsync(TestData.NewCluster("sync-2")).Wait();
    }

    public void Dispose() => harness.Dispose();

    private ClusterSyncBackgroundService BuildService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(harness.Db);
        services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        services.AddSingleton<IClusterClientCache>(new ClusterClientCache(
            K8sMocks.Factory(k8s), NullLogger<ClusterClientCache>.Instance));
        services.AddScoped<IClusterRepository>(_ => harness.ClusterRepo);
        services.AddScoped(_ => harness.Audit);
        services.AddScoped<ClusterNodeService>();
        services.AddScoped<ClusterService>();
        services.AddScoped<ClusterSyncSettingService>();
        services.AddSingleton<IHttpContextAccessor>(TestHttpContext.For("admin", "Admin").Object);
        services.AddScoped<ClusterSyncBackgroundService>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ClusterSyncBackgroundService>();
    }

    [Fact]
    public async Task RunOnceAsync_refreshes_all_clusters()
    {
        var service = BuildService();

        var succeeded = await service.RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, succeeded);
    }

    [Fact]
    public async Task RunOnceAsync_marks_clusters_offline_when_k8s_unreachable()
    {
        var service = BuildService();

        await service.RunOnceAsync(TestContext.Current.CancellationToken);

        var clusters = await harness.ClusterRepo.GetAllIdsAsync();
        foreach (var id in clusters)
        {
            var cluster = await harness.ClusterRepo.GetByIdAsync(id);
            Assert.Equal(MultiClusterMgmtSys.Domain.Enums.ClusterStatus.Offline, cluster!.Status);
        }
    }

    [Fact]
    public async Task RunOnceAsync_forwards_cancellation_token()
    {
        var service = BuildService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunOnceAsync(cts.Token));
    }
}
