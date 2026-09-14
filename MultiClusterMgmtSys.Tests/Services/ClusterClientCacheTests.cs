using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterClientCacheTests : IDisposable
{
    private const string MinimalKubeConfig = """
        apiVersion: v1
        kind: Config
        clusters:
        - name: c
          cluster:
            server: https://kubeconfig-cache:6443
        contexts:
        - name: ctx
          context:
            cluster: c
            user: u
        current-context: ctx
        users:
        - name: u
          user:
            token: abc
        """;

    private readonly ServiceHarness harness = new("admin", "Admin");

    public void Dispose() => harness.Dispose();

    private async Task<int> SeedAsync(string name)
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static ClusterInfo StandaloneCluster(string name, int id)
    {
        var cluster = TestData.NewCluster(name);
        cluster.Id = id;
        return cluster;
    }

    private static ClusterClientCache CountingCache(List<Mock<IKubernetes>> mocks, TimeSpan? idle = null)
        => new(_ =>
            {
                var mock = K8sMocks.Create();
                mocks.Add(mock);
                return mock.Object;
            },
            NullLogger<ClusterClientCache>.Instance, idle);

    private static (ClusterClientCache Cache, Func<int> Count) SingleClientCache(IKubernetes client, TimeSpan? idle = null)
    {
        var count = 0;
        var cache = new ClusterClientCache(_ =>
            {
                Interlocked.Increment(ref count);
                return client;
            },
            NullLogger<ClusterClientCache>.Instance, idle);
        return (cache, () => count);
    }

    [Fact]
    public void GetOrCreate_same_cluster_uses_factory_once()
    {
        var mocks = new List<Mock<IKubernetes>>();
        var cache = CountingCache(mocks);
        var cluster = StandaloneCluster("cache-hit", 11);

        var first = cache.GetOrCreate(cluster);
        var second = cache.GetOrCreate(cluster);

        Assert.Single(mocks);
        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreate_different_clusters_get_independent_clients()
    {
        var mocks = new List<Mock<IKubernetes>>();
        var cache = CountingCache(mocks);

        var a = cache.GetOrCreate(StandaloneCluster("iso-a", 12));
        var b = cache.GetOrCreate(StandaloneCluster("iso-b", 13));

        Assert.NotSame(a, b);
        Assert.Equal(2, mocks.Count);
    }

    private static async Task WaitForEvictionAsync(List<Mock<IKubernetes>> mocks)
    {
        for (var i = 0; i < 20; i++)
        {
            try
            {
                mocks[0].Verify(m => m.Dispose(), Times.Once);
                return;
            }
            catch (MockException)
            {
                await Task.Delay(50, TestContext.Current.CancellationToken);
            }
        }

        mocks[0].Verify(m => m.Dispose(), Times.Once);
    }

    [Fact]
    public async Task GetOrCreate_credential_change_rebuilds_and_disposes_old()
    {
        var mocks = new List<Mock<IKubernetes>>();
        var cache = CountingCache(mocks);
        var cluster = StandaloneCluster("cred-change", 21);

        var first = cache.GetOrCreate(cluster);
        cluster.Token = "rotated-token";
        var second = cache.GetOrCreate(cluster);

        Assert.NotSame(first, second);
        Assert.Equal(2, mocks.Count);
        await WaitForEvictionAsync(mocks);
        mocks[1].Verify(m => m.Dispose(), Times.Never);
    }

    [Fact]
    public void GetOrCreate_field_change_without_edit_notification_rebuilds()
    {
        var mocks = new List<Mock<IKubernetes>>();
        var cache = CountingCache(mocks);
        var cluster = StandaloneCluster("backfill", 22);

        var first = cache.GetOrCreate(cluster);
        cluster.ApiServer = "https://backfilled-host:6443";
        var second = cache.GetOrCreate(cluster);

        Assert.NotSame(first, second);
        Assert.Equal(2, mocks.Count);
    }

    [Fact]
    public void GetOrCreate_miss_builds_config_with_uniform_timeout()
    {
        var captured = new List<KubernetesClientConfiguration>();
        var cache = new ClusterClientCache(config =>
            {
                captured.Add(config);
                return K8sMocks.Create().Object;
            },
            NullLogger<ClusterClientCache>.Instance);
        var tokenCluster = StandaloneCluster("timeout-token", 31);
        var kubeCluster = StandaloneCluster("timeout-kube", 32);
        kubeCluster.ConnectionType = ConnectionType.KubeConfig;
        kubeCluster.KubeConfig = MinimalKubeConfig;

        cache.GetOrCreate(tokenCluster);
        cache.GetOrCreate(kubeCluster);

        Assert.Equal(2, captured.Count);
        Assert.All(captured, c => Assert.Equal(TimeSpan.FromSeconds(10), c.HttpClientTimeout));
    }

    [Fact]
    public async Task GetOrCreate_idle_entries_expire_and_release_clients()
    {
        var mocks = new List<Mock<IKubernetes>>();
        var cache = CountingCache(mocks, TimeSpan.FromMilliseconds(50));
        var cluster = StandaloneCluster("idle-expire", 41);

        var first = cache.GetOrCreate(cluster);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var second = cache.GetOrCreate(cluster);

        Assert.NotSame(first, second);
        Assert.Equal(2, mocks.Count);
        await WaitForEvictionAsync(mocks);
    }

    [Fact]
    public void Evicted_in_flight_call_degrades_via_existing_translation()
    {
        var original = new ObjectDisposedException(nameof(IKubernetes));

        var translated = K8sExceptionMapper.Translate(original, "刷新状态");

        Assert.Same(original, translated);
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_probe_round_reuses_cached_client()
    {
        var k8s = K8sMocks.Create();
        K8sMocks.SetupGetVersion(k8s, "1.31.0");
        K8sMocks.SetupListNodes(k8s);
        var (cache, count) = SingleClientCache(k8s.Object);
        var nodeService = new ClusterNodeService(
            harness.ClusterRepo, harness.Audit, NullLogger<ClusterNodeService>.Instance, cache);
        var service = new ClusterService(
            harness.ClusterRepo, nodeService, harness.Audit, NullLogger<ClusterService>.Instance, cache);
        var id = await SeedAsync("probe-reuse");

        await service.RefreshClusterStatusAsync(id);

        Assert.Equal(1, count());
    }

    [Fact]
    public async Task GetOrCreate_concurrent_calls_share_one_client()
    {
        var client = K8sMocks.Create().Object;
        var (cache, count) = SingleClientCache(client);
        var cluster = StandaloneCluster("concurrent", 51);

        var tasks = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => cache.GetOrCreate(cluster), TestContext.Current.CancellationToken));
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, count());
        Assert.All(results, r => Assert.Same(client, r));
    }
}
