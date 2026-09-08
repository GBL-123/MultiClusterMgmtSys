using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly ClusterService service;

    public ClusterServiceTests()
    {
        var nodeService = new ClusterNodeService(
            harness.ClusterRepo, harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Factory(k8s));
        service = new ClusterService(
            harness.ClusterRepo, nodeService, harness.Audit,
            NullLogger<ClusterService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    private Task<int> SeedAsync(string name, ClusterStatus status = ClusterStatus.Unknown, string? version = null)
        => harness.ClusterRepo.AddAsync(TestData.NewCluster(name, status: status, version: version)).ContinueWith(t => t.Result.Id);

    [Fact]
    public async Task GetPagedAsync_version_all_sentinel_maps_to_no_filter()
    {
        await SeedAsync("a", version: "1.29.0");
        await SeedAsync("b", version: null);

        var result = await service.GetPagedAsync(new ClusterQueryRequest { VersionSelection = VersionFilterSentinel.All });

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetPagedAsync_only_null_sentinel_filters_unknown_versions()
    {
        await SeedAsync("a", version: "1.29.0");
        await SeedAsync("b", version: null);
        await SeedAsync("c", version: "");

        var result = await service.GetPagedAsync(new ClusterQueryRequest { VersionSelection = VersionFilterSentinel.OnlyNull });

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task GetPagedAsync_exact_version_selection()
    {
        await SeedAsync("a", version: "1.29.0");
        await SeedAsync("b", version: "1.30.0");

        var result = await service.GetPagedAsync(new ClusterQueryRequest { VersionSelection = "1.29.0" });

        Assert.Equal(1, result.Total);
        Assert.Equal("a", result.Items.Single().Name);
    }

    [Fact]
    public async Task GetClusterDetailAsync_missing_returns_null()
    {
        Assert.Null(await service.GetClusterDetailAsync(999));
    }

    [Fact]
    public async Task GetClusterDetailAsync_offline_cluster_skips_k8s_and_is_unreachable()
    {
        var id = await SeedAsync("offline", status: ClusterStatus.Offline);

        var detail = await service.GetClusterDetailAsync(id);

        Assert.NotNull(detail);
        Assert.False(detail!.IsReachable);
        Assert.Empty(detail.Nodes);
    }

    [Fact]
    public async Task GetClusterDetailAsync_online_with_failing_k8s_degrades_gracefully()
    {
        var id = await SeedAsync("online", status: ClusterStatus.Online);

        var detail = await service.GetClusterDetailAsync(id);

        Assert.NotNull(detail);
        Assert.False(detail!.IsReachable);
        Assert.Empty(detail.Nodes);
    }

    [Fact]
    public async Task GetClusterDetailAsync_online_with_k8s_returns_nodes()
    {
        var id = await SeedAsync("online", status: ClusterStatus.Online);
        k8s.SetupListNodes(BuildNode("node-1", ready: true), BuildNode("node-2", ready: false));

        var detail = await service.GetClusterDetailAsync(id);

        Assert.NotNull(detail);
        Assert.True(detail!.IsReachable);
        Assert.Equal(2, detail.Nodes.Count);
        Assert.Equal("Ready", detail.Nodes[0].Status);
        Assert.Equal("NotReady", detail.Nodes[1].Status);
    }

    [Fact]
    public async Task GetClusterForEditAsync_missing_returns_null()
    {
        Assert.Null(await service.GetClusterForEditAsync(999));
    }

    [Fact]
    public async Task GetClusterForEditAsync_maps_edit_fields()
    {
        var id = await SeedAsync("editable", version: null);

        var edit = await service.GetClusterForEditAsync(id);

        Assert.NotNull(edit);
        Assert.Equal("editable", edit!.Name);
        Assert.Equal(ConnectionType.Token, edit.ConnectionType);
    }

    [Fact]
    public async Task AddClusterAsync_token_probe_failure_marks_offline_and_audits()
    {
        var request = new ClusterCreateRequest
        {
            Name = "new-cluster",
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://new:6443",
            Token = "tok"
        };

        var vm = await service.AddClusterAsync(request);

        Assert.Equal(ClusterStatus.Offline, vm.Status);
        Assert.Equal("new-cluster", vm.Name);
        var audit = await harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("new", audit.Target);
    }

    [Fact]
    public async Task AddClusterAsync_probe_success_sets_online_version_and_nodes()
    {
        k8s.SetupGetVersion("v1.30.2");
        k8s.SetupListNodes(BuildNode("n1", ready: true), BuildNode("n2", ready: true), BuildNode("n3", ready: false));

        var vm = await service.AddClusterAsync(new ClusterCreateRequest
        {
            Name = "healthy",
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://good:6443",
            Token = "tok"
        });

        Assert.Equal(ClusterStatus.Online, vm.Status);
        Assert.Equal("v1.30.2", vm.Version);
        Assert.Equal(3, vm.NodeCount);
    }

    [Fact]
    public async Task AddClusterAsync_applies_endpoints_from_request()
    {
        k8s.SetupGetVersion("v1.30.2");
        k8s.SetupListNodes();

        var vm = await service.AddClusterAsync(new ClusterCreateRequest
        {
            Name = "with-endpoints",
            ConnectionType = ConnectionType.Token,
            Endpoints =
            [
                new ClusterEndpointEditItem { Kind = ClusterEndpointKind.Vip, Value = " 10.1.1.1 ", Note = " 管理口 " },
                new ClusterEndpointEditItem { Kind = ClusterEndpointKind.Domain, Value = "k8s.example.com" }
            ]
        });

        var cluster = await harness.ClusterRepo.GetByIdAsync(vm.Id);
        var endpoints = cluster!.Endpoints.OrderBy(e => e.Kind).ToList();
        Assert.Equal(2, endpoints.Count);
        Assert.Equal("10.1.1.1", endpoints[0].Value);
        Assert.Equal("管理口", endpoints[0].Note);
        Assert.Equal("k8s.example.com", endpoints[1].Value);
    }

    [Fact]
    public async Task UpdateClusterAsync_missing_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateClusterAsync(new ClusterUpdateRequest { Id = 999, Name = "x" }));
    }

    [Fact]
    public async Task UpdateClusterAsync_unchanged_config_skips_probe()
    {
        var id = await SeedAsync("keep");
        var entity = await harness.ClusterRepo.GetByIdAsync(id);

        var vm = await service.UpdateClusterAsync(new ClusterUpdateRequest
        {
            Id = id,
            Name = "renamed",
            ConnectionType = entity!.ConnectionType,
            ApiServer = entity.ApiServer,
            Token = entity.Token,
            SkipTlsVerify = entity.SkipTlsVerify
        });

        Assert.Equal("renamed", vm.Name);
        Assert.Equal(ClusterStatus.Unknown, vm.Status);
        k8s.Verify(x => x.Version.GetCodeWithHttpMessagesAsync(
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        var audit = await harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateClusterAsync_changed_config_probes_and_marks_offline()
    {
        var id = await SeedAsync("flip");

        var vm = await service.UpdateClusterAsync(new ClusterUpdateRequest
        {
            Id = id,
            Name = "flip",
            ConnectionType = ConnectionType.Token,
            ApiServer = "https://changed:6443",
            Token = "new-token"
        });

        Assert.Equal(ClusterStatus.Offline, vm.Status);
    }

    [Fact]
    public async Task DeleteClusterAsync_removes_and_audits()
    {
        var id = await SeedAsync("doomed");

        await service.DeleteClusterAsync(id);

        Assert.Null(await harness.ClusterRepo.GetByIdAsync(id));
        var audit = await harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteClusterAsync_missing_is_silent_noop()
    {
        await service.DeleteClusterAsync(999);

        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateClusterEndpointsAsync_missing_throws_not_found()
    {
        var request = new ClusterEndpointsUpdateRequest(999, [new ClusterEndpointEditItem { Value = "1.2.3.4" }]);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateClusterEndpointsAsync(request));
    }

    [Fact]
    public async Task UpdateClusterEndpointsAsync_replaces_endpoints_and_audits()
    {
        var id = await SeedAsync("with-eps");

        await service.UpdateClusterEndpointsAsync(new ClusterEndpointsUpdateRequest(id,
        [
            new ClusterEndpointEditItem { Id = 1, Kind = ClusterEndpointKind.Vip, Value = "10.0.0.9", SortOrder = 1 }
        ]));

        var cluster = await harness.ClusterRepo.GetByIdAsync(id);
        Assert.Single(cluster!.Endpoints);
        Assert.Equal("10.0.0.9", cluster.Endpoints.First().Value);
        var audit = await harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Contains("端点", audit.Target);
    }

    [Fact]
    public async Task UpdateClusterEndpointsAsync_value_over_256_throws()
    {
        var id = await SeedAsync("long-ep");

        var request = new ClusterEndpointsUpdateRequest(id,
        [
            new ClusterEndpointEditItem { Value = new string('x', 257) }
        ]);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateClusterEndpointsAsync(request));
    }

    [Fact]
    public async Task RefreshClusterStatusAsync_marks_offline_on_probe_failure()
    {
        var id = await SeedAsync("was-online", status: ClusterStatus.Online);

        var vm = await service.RefreshClusterStatusAsync(id);

        Assert.Equal(ClusterStatus.Offline, vm.Status);
    }

    [Fact]
    public async Task RefreshAllClustersStatusAsync_counts_and_audits_status_changes()
    {
        await SeedAsync("was-online", status: ClusterStatus.Online);
        await SeedAsync("was-unknown", status: ClusterStatus.Unknown);

        var succeeded = await service.RefreshAllClustersStatusAsync(source: ClusterSyncSource.Scheduled);

        Assert.Equal(2, succeeded);
        var audits = await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, audits.Count);
        Assert.All(audits, a => Assert.Contains("定时同步", a.Target));
    }

    [Fact]
    public async Task GetAvailableVersionsAsync_returns_distinct_sorted()
    {
        await SeedAsync("a", version: "1.30.0");
        await SeedAsync("b", version: "1.29.0");
        await SeedAsync("c", version: "1.29.0");

        var versions = await service.GetAvailableVersionsAsync();

        Assert.Equal(["1.29.0", "1.30.0"], versions);
    }

    private static V1Node BuildNode(string name, bool ready)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name },
            Status = new k8s.Models.V1NodeStatus
            {
                Conditions = [new k8s.Models.V1NodeCondition { Type = "Ready", Status = ready ? "True" : "False" }],
                NodeInfo = new k8s.Models.V1NodeSystemInfo { KubeletVersion = "v1.30.2" },
                Addresses = [new k8s.Models.V1NodeAddress { Type = "InternalIP", Address = "10.0.0.1" }]
            },
            Spec = new k8s.Models.V1NodeSpec { Unschedulable = false }
        };
}
