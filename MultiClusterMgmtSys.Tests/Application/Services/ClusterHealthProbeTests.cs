using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class ClusterHealthProbeTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();
    private readonly ClusterService _service;

    public ClusterHealthProbeTests()
    {
        var nodeService = new ClusterNodeService(
            _harness.ClusterRepo, _harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Cache(_k8s));
        _service = new ClusterService(
            _harness.ClusterRepo, nodeService, _harness.Audit,
            NullLogger<ClusterService>.Instance, K8sMocks.Cache(_k8s), _harness.ClusterHealthRepo);
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedAsync(string name, ClusterStatus status = ClusterStatus.Unknown)
    {
        var added = await _harness.ClusterRepo.AddAsync(TestData.NewCluster(name, status: status));
        return added.Id;
    }

    private static V1Node Node(string name, string? readyStatus)
    {
        var node = new V1Node { Metadata = new V1ObjectMeta { Name = name } };
        if (readyStatus is not null)
        {
            node.Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = readyStatus }]
            };
        }

        return node;
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_appends_snapshot_with_ready_and_not_ready_counts()
    {
        _k8s.SetupGetVersion("1.31.0");
        _k8s.SetupListNodes(
            Node("ready-1", "True"),
            Node("ready-2", "True"),
            Node("ready-3", "True"),
            Node("ready-4", "True"),
            Node("ready-5", "True"),
            Node("ready-6", "True"),
            Node("not-ready", "False"),
            Node("no-condition", null));
        var id = await SeedAsync("snap-counts");

        await _service.RefreshClusterStatusAsync(id);

        var snapshot = (await _harness.ClusterHealthRepo.GetLatestPerClusterAsync())[id];
        Assert.Equal(id, snapshot.ClusterId);
        Assert.Equal(8, snapshot.TotalNodes);
        Assert.Equal(6, snapshot.ReadyNodes);
        Assert.Equal(2, snapshot.NotReadyNodes);
        Assert.Equal(snapshot.TotalNodes, snapshot.ReadyNodes + snapshot.NotReadyNodes);
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_counts_unknown_ready_condition_as_not_ready()
    {
        _k8s.SetupGetVersion("1.31.0");
        _k8s.SetupListNodes(Node("unknown", "Unknown"));
        var id = await SeedAsync("snap-unknown");

        await _service.RefreshClusterStatusAsync(id);

        var snapshot = (await _harness.ClusterHealthRepo.GetLatestPerClusterAsync())[id];
        Assert.Equal(1, snapshot.TotalNodes);
        Assert.Equal(0, snapshot.ReadyNodes);
        Assert.Equal(1, snapshot.NotReadyNodes);
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_appends_snapshot_for_each_successful_round()
    {
        _k8s.SetupGetVersion("1.31.0");
        _k8s.SetupListNodes(Node("n1", "True"));
        var id = await SeedAsync("snap-append");

        await _service.RefreshClusterStatusAsync(id);
        await _service.RefreshClusterStatusAsync(id);

        Assert.Equal(2, _harness.Db.ClusterHealthSnapshots.Count(s => s.ClusterId == id));
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_does_not_append_snapshot_when_probe_fails()
    {
        _k8s.SetupGetVersionThrows(K8sMocks.K8sError(500));
        var id = await SeedAsync("snap-fail", status: ClusterStatus.Online);

        var vm = await _service.RefreshClusterStatusAsync(id);

        Assert.Equal(ClusterStatus.Offline, vm.Status);
        Assert.Equal(0, vm.NodeCount);
        Assert.Empty(await _harness.ClusterHealthRepo.GetLatestPerClusterAsync());
    }

    [Fact]
    public async Task RefreshAllClustersStatusAsync_does_not_append_snapshot_when_cancelled()
    {
        await SeedAsync("snap-cancel");
        _k8s.Setup(x => x.Version.GetCodeWithHttpMessagesAsync(
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IReadOnlyDictionary<string, IReadOnlyList<string>> _, CancellationToken ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                throw new InvalidOperationException("unreachable");
            });

        using var cts = new CancellationTokenSource();
        var refresh = _service.RefreshAllClustersStatusAsync(cancellationToken: cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        Assert.Empty(await _harness.ClusterHealthRepo.GetLatestPerClusterAsync());
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_reuses_the_single_node_list_call()
    {
        _k8s.SetupGetVersion("1.31.0");
        _k8s.SetupListNodes(Node("n1", "True"));
        var id = await SeedAsync("snap-once");

        await _service.RefreshClusterStatusAsync(id);

        _k8s.VerifyListNodes(Times.Once());
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_keeps_cluster_online_when_snapshot_write_fails()
    {
        _k8s.SetupGetVersion("1.31.0");
        _k8s.SetupListNodes(Node("n1", "True"));
        var failingRepo = new Mock<IClusterHealthRepository>();
        failingRepo.Setup(r => r.AddAsync(It.IsAny<ClusterHealthSnapshot>()))
                   .ThrowsAsync(new InvalidOperationException("snapshot store down"));
        var nodeService = new ClusterNodeService(
            _harness.ClusterRepo, _harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Cache(_k8s));
        var probe = new ClusterService(
            _harness.ClusterRepo, nodeService, _harness.Audit,
            NullLogger<ClusterService>.Instance, K8sMocks.Cache(_k8s), failingRepo.Object);
        var id = await SeedAsync("snap-write-fail");

        var vm = await probe.RefreshClusterStatusAsync(id);

        Assert.Equal(ClusterStatus.Online, vm.Status);
        Assert.Equal(1, vm.NodeCount);
    }
}
