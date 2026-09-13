using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterRefreshConcurrencyTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly ClusterService service;

    public ClusterRefreshConcurrencyTests()
    {
        var nodeService = new ClusterNodeService(
            harness.ClusterRepo, harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Factory(k8s));
        service = new ClusterService(
            harness.ClusterRepo, nodeService, harness.Audit,
            NullLogger<ClusterService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    private Task<int> SeedAsync(string name, ClusterStatus status = ClusterStatus.Online)
        => harness.ClusterRepo.AddAsync(TestData.NewCluster(name, status: status)).ContinueWith(t => t.Result.Id);

    private void SetupBlockingGetVersion(Func<CancellationToken, Task> behavior)
        => k8s.Setup(x => x.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyDictionary<string, IReadOnlyList<string>> _, CancellationToken ct) =>
            {
                await behavior(ct);
                throw new InvalidOperationException("probe fail");
            });

    [Fact]
    public async Task RefreshAllClustersStatusAsync_probes_clusters_concurrently()
    {
        await SeedAsync("conc-1");
        await SeedAsync("conc-2");

        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inFlight = 0;
        var maxInFlight = 0;
        var gate = new object();

        SetupBlockingGetVersion(async _ =>
        {
            var now = Interlocked.Increment(ref inFlight);
            lock (gate)
            {
                maxInFlight = Math.Max(maxInFlight, now);
            }

            if (now == 2)
            {
                bothStarted.TrySetResult();
            }

            try
            {
                await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        });

        var succeeded = await service.RefreshAllClustersStatusAsync();

        Assert.Equal(2, succeeded);
        Assert.True(maxInFlight >= 2, $"期望并发探测,实际最大在途 {maxInFlight}");
    }

    [Fact]
    public async Task RefreshAllClustersStatusAsync_caps_probe_concurrency_at_four()
    {
        for (var i = 1; i <= 6; i++)
        {
            await SeedAsync($"cap-{i}");
        }

        var fifthStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        var inFlight = 0;
        var maxInFlight = 0;
        var gate = new object();

        SetupBlockingGetVersion(async _ =>
        {
            var now = Interlocked.Increment(ref inFlight);
            lock (gate)
            {
                maxInFlight = Math.Max(maxInFlight, now);
            }

            if (Interlocked.Increment(ref started) >= 5)
            {
                fifthStarted.TrySetResult();
            }

            try
            {
                await fifthStarted.Task.WaitAsync(TimeSpan.FromMilliseconds(750));
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        });

        var succeeded = await service.RefreshAllClustersStatusAsync();

        Assert.Equal(6, succeeded);
        Assert.Equal(4, maxInFlight);
    }

    [Fact]
    public async Task RefreshAllClustersStatusAsync_pre_canceled_token_skips_probes()
    {
        var id = await SeedAsync("cancel-pre");
        var before = await harness.ClusterRepo.GetByIdAsync(id);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RefreshAllClustersStatusAsync(cancellationToken: cts.Token));

        k8s.Verify(x => x.Version.GetCodeWithHttpMessagesAsync(
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);

        var after = await harness.ClusterRepo.GetByIdAsync(id);
        Assert.Equal(before!.Status, after!.Status);
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.NodeCount, after.NodeCount);
        Assert.Equal(before.LastCheckedAt, after.LastCheckedAt);
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefreshAllClustersStatusAsync_cancel_mid_probe_leaves_state_untouched()
    {
        var id = await SeedAsync("cancel-mid");
        var before = await harness.ClusterRepo.GetByIdAsync(id);

        var probeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetupBlockingGetVersion(async ct =>
        {
            probeStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        });

        using var cts = new CancellationTokenSource();
        var refreshTask = service.RefreshAllClustersStatusAsync(cancellationToken: cts.Token);
        await probeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refreshTask);

        var after = await harness.ClusterRepo.GetByIdAsync(id);
        Assert.Equal(ClusterStatus.Online, after!.Status);
        Assert.Equal(before!.Version, after.Version);
        Assert.Equal(before.NodeCount, after.NodeCount);
        Assert.Equal(before.LastCheckedAt, after.LastCheckedAt);
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }
}
