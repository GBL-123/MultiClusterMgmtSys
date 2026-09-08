using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class ClusterNodeServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly ClusterNodeService service;

    public ClusterNodeServiceTests()
    {
        service = new ClusterNodeService(
            harness.ClusterRepo, harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    [Fact]
    public async Task GetClusterNodesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetClusterNodesAsync(999));
    }

    [Fact]
    public async Task GetClusterNodesAsync_maps_nodes_and_merges_remarks()
    {
        var clusterId = await SeedAsyncWithRemark();
        k8s.SetupListNodes(
            new V1Node
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "node-1",
                    Labels = new Dictionary<string, string>
                    {
                        ["node-role.kubernetes.io/control-plane"] = "",
                        ["other"] = "x"
                    }
                },
                Status = new V1NodeStatus
                {
                    Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }],
                    Addresses =
                    [
                        new V1NodeAddress { Type = "InternalIP", Address = "10.0.0.1" },
                        new V1NodeAddress { Type = "Hostname", Address = "node-1" }
                    ],
                    NodeInfo = new V1NodeSystemInfo { KubeletVersion = "v1.30.2", OsImage = "Ubuntu" }
                },
                Spec = new V1NodeSpec { Unschedulable = true }
            });

        var nodes = await service.GetClusterNodesAsync(clusterId);

        var node = nodes.Single();
        Assert.Equal("node-1", node.Name);
        Assert.Equal("Ready", node.Status);
        Assert.Equal("control-plane", node.Roles);
        Assert.True(node.Unschedulable);
        Assert.Equal("v1.30.2", node.KubeletVersion);
        var ip = node.IpAddresses.Single();
        Assert.Equal("10.0.0.1", ip.Address);
        Assert.Equal("管理口", ip.Note);
    }

    [Fact]
    public async Task GetClusterNodesAsync_k8s_404_translated_to_not_found()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-404"));
        k8s.SetupListNodesThrows(K8sMocks.K8sError(404));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetClusterNodesAsync(cluster.Id));

        Assert.IsAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task GetClusterNodesAsync_timeout_translated_to_unreachable()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("nodes-timeout"));
        k8s.SetupListNodesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(() => service.GetClusterNodesAsync(cluster.Id));
    }

    [Fact]
    public async Task GetNodeDetailAsync_missing_cluster_returns_null()
    {
        Assert.Null(await service.GetNodeDetailAsync(new NodeDetailQueryRequest(999, "n1")));
    }

    [Fact]
    public async Task GetNodeDetailAsync_offline_cluster_short_circuits_unreachable()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("down", status: ClusterStatus.Offline));

        var detail = await service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        Assert.NotNull(detail);
        Assert.False(detail!.IsReachable);
    }

    [Fact]
    public async Task GetNodeDetailAsync_maps_full_node_view()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-src"));
        k8s.SetupReadNode("n1", new V1Node
        {
            Metadata = new V1ObjectMeta
            {
                Name = "n1",
                CreationTimestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                Labels = new Dictionary<string, string> { ["env"] = "prod" },
                Annotations = new Dictionary<string, string> { ["k"] = "v" }
            },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True", Reason = "KubeletReady" }],
                Addresses = [new V1NodeAddress { Type = "InternalIP", Address = "10.0.0.5" }],
                Capacity = new Dictionary<string, ResourceQuantity> { ["cpu"] = new ResourceQuantity("4") },
                Allocatable = new Dictionary<string, ResourceQuantity> { ["cpu"] = new ResourceQuantity("3800m") },
                NodeInfo = new V1NodeSystemInfo { Architecture = "amd64", KubeletVersion = "v1.30.2" },
                Phase = "Running"
            },
            Spec = new V1NodeSpec
            {
                Unschedulable = false,
                PodCIDR = "10.244.0.0/24",
                Taints = [new V1Taint { Key = "dedicated", Value = "gpu", Effect = "NoSchedule" }]
            }
        });

        var detail = await service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        Assert.NotNull(detail);
        Assert.True(detail!.IsReachable);
        Assert.Equal("n1", detail.Name);
        Assert.Equal("Ready", detail.Status);
        Assert.Equal("10.244.0.0/24", detail.PodCIDR);
        Assert.Equal("Running", detail.Phase);
        Assert.Equal("4", detail.Capacity["cpu"]);
        Assert.Equal("amd64", detail.SystemInfo.Architecture);
        Assert.Single(detail.Conditions);
        Assert.Equal("KubeletReady", detail.Conditions[0].Reason);
        Assert.Single(detail.Taints);
        Assert.Equal("NoSchedule", detail.Taints[0].Effect);
        Assert.Equal("prod", detail.Labels["env"]);
        Assert.Equal("v", detail.Annotations["k"]);
        var addr = detail.Addresses.Single();
        Assert.Equal("10.0.0.5", addr.Address);
    }

    [Fact]
    public async Task GetNodeDetailAsync_k8s_error_translated()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-err"));
        k8s.SetupReadNodeThrows("n1", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1")));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_missing_cluster_throws_not_found()
    {
        var request = new NodeIpNotesUpdateRequest(999, "n1", [new NodeIpNoteEditItem { Address = "10.0.0.1", Note = "a" }]);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateNodeIpNotesAsync(request));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_note_over_64_throws_validation()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("note-err"));
        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
            [new NodeIpNoteEditItem { Address = "10.0.0.1", Note = new string('x', 65) }]);

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateNodeIpNotesAsync(request));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_adds_updates_and_removes_remarks()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("remark-src"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "n1", "10.0.0.1", "旧备注"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "n1", "10.0.0.9", "将被移除"));
        await harness.ClusterRepo.UpdateAsync(cluster);

        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
        [
            new NodeIpNoteEditItem { Address = "10.0.0.1", Note = "新备注" },
            new NodeIpNoteEditItem { Address = "10.0.0.2", Note = "新增备注" }
        ]);
        await service.UpdateNodeIpNotesAsync(request);

        var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
        var remarks = reloaded!.NodeIpRemarks.OrderBy(r => r.Address).ToList();
        Assert.Equal(2, remarks.Count);
        Assert.Equal("10.0.0.1", remarks[0].Address);
        Assert.Equal("新备注", remarks[0].Note);
        Assert.Equal("10.0.0.2", remarks[1].Address);
        Assert.Equal("新增备注", remarks[1].Note);

        var audit = await harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditCategory.Node, audit.Category);
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_null_note_on_new_address_is_not_added()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("null-note"));
        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
            [new NodeIpNoteEditItem { Address = "10.0.0.3", Note = null }]);
        await service.UpdateNodeIpNotesAsync(request);

        var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
        Assert.Empty(reloaded!.NodeIpRemarks);
    }

    private async Task<int> SeedAsyncWithRemark()
    {
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("remark-cluster"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "node-1", "10.0.0.1", "管理口"));
        await harness.ClusterRepo.UpdateAsync(cluster);
        return cluster.Id;
    }
}
