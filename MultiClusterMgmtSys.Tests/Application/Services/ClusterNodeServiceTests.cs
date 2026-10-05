using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class ClusterNodeServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();
    private readonly Mock<IHttpContextAccessor> _accessor = TestHttpContext.For("admin", "Admin");
    private readonly ClusterNodeService _service;

    public ClusterNodeServiceTests()
    {
        _service = NewService();
    }

    public void Dispose() => _harness.Dispose();

    private ClusterNodeService NewService() => new(
        _harness.ClusterRepo, _harness.Audit, NullLogger<ClusterNodeService>.Instance, K8sMocks.Cache(_k8s), _accessor.Object);

    [Fact]
    public async Task GetClusterNodesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClusterNodesAsync(999));
    }

    [Fact]
    public async Task GetClusterNodesAsync_maps_nodes_and_merges_remarks()
    {
        var clusterId = await SeedAsyncWithRemark();
        _k8s.SetupListNodes(
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

        var nodes = await _service.GetClusterNodesAsync(clusterId);

        var node = nodes.Single();
        Assert.Equal("node-1", node.Name);
        Assert.Equal("Ready", node.Status);
        Assert.Equal("就绪", node.StatusText);
        Assert.Equal("online", node.StatusCssClass);
        Assert.Equal("control-plane", node.Roles);
        Assert.Equal("控制平面", node.RolesText);
        Assert.True(node.Unschedulable);
        Assert.Equal("v1.30.2", node.KubeletVersion);
        var ip = node.IpAddresses.Single();
        Assert.Equal("10.0.0.1", ip.Address);
        Assert.Equal("管理口", ip.Note);
    }

    [Fact]
    public async Task GetClusterNodesAsync_k8s_404_translated_to_not_found()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-404"));
        _k8s.SetupListNodesThrows(K8sMocks.K8sError(404));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetClusterNodesAsync(cluster.Id));

        Assert.IsAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task GetClusterNodesAsync_timeout_translated_to_unreachable()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("nodes-timeout"));
        _k8s.SetupListNodesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(() => _service.GetClusterNodesAsync(cluster.Id));
    }

    [Fact]
    public async Task GetNodeDetailAsync_missing_cluster_returns_null()
    {
        Assert.Null(await _service.GetNodeDetailAsync(new NodeDetailQueryRequest(999, "n1")));
    }

    [Fact]
    public async Task GetNodeDetailAsync_offline_cluster_short_circuits_unreachable()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("down", status: ClusterStatus.Offline));

        var detail = await _service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        Assert.NotNull(detail);
        Assert.False(detail!.IsReachable);
    }

    [Fact]
    public async Task GetNodeDetailAsync_maps_full_node_view()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-src"));
        _k8s.SetupReadNode("n1", new V1Node
        {
            ApiVersion = "v1",
            Kind = "Node",
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

        var detail = await _service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        Assert.NotNull(detail);
        Assert.True(detail!.IsReachable);
        Assert.Equal("n1", detail.Name);
        Assert.Equal("Ready", detail.Status);
        Assert.Equal("就绪", detail.StatusText);
        Assert.Equal("online", detail.StatusCssClass);
        Assert.Equal("10.244.0.0/24", detail.PodCIDR);
        Assert.Equal("Running", detail.Phase);
        Assert.Equal("运行中", detail.PhaseText);
        var cpu = Assert.Single(detail.Resources);
        Assert.Equal("cpu", cpu.Key);
        Assert.Equal("CPU", cpu.Label);
        Assert.Equal("4 核", cpu.CapacityText);
        Assert.Equal("3.8 核", cpu.AllocatableText);
        Assert.Equal(95, cpu.AllocatablePercent);
        Assert.Equal("amd64", detail.SystemInfo.Architecture);
        Assert.Single(detail.Conditions);
        Assert.Equal("KubeletReady", detail.Conditions[0].Reason);
        Assert.Equal("就绪", detail.Conditions[0].TypeText);
        Assert.Equal("成立", detail.Conditions[0].StatusText);
        Assert.Equal("online", detail.Conditions[0].StatusCssClass);
        Assert.Single(detail.Taints);
        Assert.Equal("NoSchedule", detail.Taints[0].Effect);
        Assert.Equal("禁止调度", detail.Taints[0].EffectText);
        Assert.Equal("prod", detail.Labels["env"]);
        Assert.Equal("v", detail.Annotations["k"]);
        Assert.Contains("name: n1", detail.Yaml);
        Assert.Contains("kind: Node", detail.Yaml);
        var addr = detail.Addresses.Single();
        Assert.Equal("10.0.0.5", addr.Address);
        Assert.Equal("内网 IP", addr.TypeText);
    }

    [Fact]
    public async Task GetNodeDetailAsync_formats_resources_and_orders_rows()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("resource-src"));
        _k8s.SetupReadNode("n1", new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Capacity = new Dictionary<string, ResourceQuantity>
                {
                    ["pods"] = new("110"),
                    ["zeta"] = new("1"),
                    ["memory"] = new("16297496Ki"),
                    ["cpu"] = new("4"),
                    ["hugepages-2Mi"] = new("2Mi"),
                    ["ephemeral-storage"] = new("8Gi"),
                    ["example.com/fpga"] = new("2")
                },
                Allocatable = new Dictionary<string, ResourceQuantity>
                {
                    ["cpu"] = new("3800m"),
                    ["memory"] = new("15942336Ki"),
                    ["pods"] = new("110"),
                    ["nvidia.com/gpu"] = new("1")
                }
            }
        });

        var detail = await _service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        Assert.NotNull(detail);
        Assert.Equal(
            ["cpu", "memory", "ephemeral-storage", "pods", "hugepages-2Mi", "example.com/fpga", "nvidia.com/gpu", "zeta"],
            detail!.Resources.Select(r => r.Key));

        var cpu = detail.Resources[0];
        Assert.Equal("CPU", cpu.Label);
        Assert.Equal("4", cpu.CapacityRaw);
        Assert.Equal("4 核", cpu.CapacityText);
        Assert.Equal("3.8 核", cpu.AllocatableText);
        Assert.Equal(95, cpu.AllocatablePercent);

        var memory = detail.Resources[1];
        Assert.Equal("内存", memory.Label);
        Assert.Equal("16297496Ki", memory.CapacityRaw);
        Assert.Equal("15.5 GiB", memory.CapacityText);
        Assert.Equal("15.2 GiB", memory.AllocatableText);

        var storage = detail.Resources[2];
        Assert.Equal("临时存储", storage.Label);
        Assert.Equal("8 GiB", storage.CapacityText);
        Assert.Equal("—", storage.AllocatableText);
        Assert.Null(storage.AllocatablePercent);

        var pods = detail.Resources[3];
        Assert.Equal("Pod", pods.Label);
        Assert.Equal("110 个", pods.CapacityText);
        Assert.Equal(100, pods.AllocatablePercent);

        var hugePages = detail.Resources[4];
        Assert.Equal("大页", hugePages.Label);
        Assert.Equal("2 MiB", hugePages.CapacityText);

        var unknown = detail.Resources[5];
        Assert.Equal("example.com/fpga", unknown.Label);
        Assert.Equal("2", unknown.CapacityText);

        var allocatableOnly = detail.Resources[6];
        Assert.Equal("—", allocatableOnly.CapacityText);
        Assert.Equal("1", allocatableOnly.AllocatableText);

        Assert.Equal("zeta", detail.Resources[7].Key);
    }

    [Fact]
    public async Task GetNodeDetailAsync_falls_back_to_raw_quantity_when_not_convertible()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("overflow-src"));
        _k8s.SetupReadNode("n1", new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Capacity = new Dictionary<string, ResourceQuantity>
                {
                    ["memory"] = new("1000000000000000000000E")
                }
            }
        });

        var detail = await _service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1"));

        var memory = Assert.Single(detail!.Resources);
        Assert.NotNull(memory.CapacityRaw);
        Assert.Equal(memory.CapacityRaw, memory.CapacityText);
        Assert.Null(memory.AllocatablePercent);
    }

    [Fact]
    public async Task GetNodeDetailAsync_k8s_error_translated()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-err"));
        _k8s.SetupReadNodeThrows("n1", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetNodeDetailAsync(new NodeDetailQueryRequest(cluster.Id, "n1")));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_missing_cluster_throws_not_found()
    {
        var request = new NodeIpNotesUpdateRequest(999, "n1", [new NodeIpNoteEditItem { Address = "10.0.0.1", Note = "a" }]);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateNodeIpNotesAsync(request));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_note_over_64_throws_validation()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("note-err"));
        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
            [new NodeIpNoteEditItem { Address = "10.0.0.1", Note = new string('x', 65) }]);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateNodeIpNotesAsync(request));
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_adds_updates_and_removes_remarks()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("remark-src"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "n1", "10.0.0.1", "旧备注"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "n1", "10.0.0.9", "将被移除"));
        await _harness.ClusterRepo.UpdateAsync(cluster);

        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
        [
            new NodeIpNoteEditItem { Address = "10.0.0.1", Note = "新备注" },
            new NodeIpNoteEditItem { Address = "10.0.0.2", Note = "新增备注" }
        ]);
        await _service.UpdateNodeIpNotesAsync(request);

        var reloaded = await _harness.ClusterRepo.GetByIdAsync(cluster.Id);
        var remarks = reloaded!.NodeIpRemarks.OrderBy(r => r.Address).ToList();
        Assert.Equal(2, remarks.Count);
        Assert.Equal("10.0.0.1", remarks[0].Address);
        Assert.Equal("新备注", remarks[0].Note);
        Assert.Equal("10.0.0.2", remarks[1].Address);
        Assert.Equal("新增备注", remarks[1].Note);

        var audit = await _harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditCategory.Node, audit.Category);
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateNodeIpNotesAsync_null_note_on_new_address_is_not_added()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("null-note"));
        var request = new NodeIpNotesUpdateRequest(cluster.Id, "n1",
            [new NodeIpNoteEditItem { Address = "10.0.0.3", Note = null }]);
        await _service.UpdateNodeIpNotesAsync(request);

        var reloaded = await _harness.ClusterRepo.GetByIdAsync(cluster.Id);
        Assert.Empty(reloaded!.NodeIpRemarks);
    }

    [Fact]
    public async Task CordonAsync_member_denied_without_k8s_call_or_audit()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns(TestHttpContext.For("member").Object.HttpContext);
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("maint-member"));
        var request = new NodeMaintenanceRequest(cluster.Id, "n1");

        var ex = await Assert.ThrowsAsync<PermissionException>(() => _service.CordonAsync(request));

        Assert.Equal("节点维护操作仅管理员可用", ex.UserMessage);
        _k8s.VerifyNodePatched("n1", Times.Never());
        Assert.Empty(await _harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CordonAsync_patches_unschedulable_true_and_writes_audit()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("cordon-ok"));
        _k8s.SetupPatchNode("n1");
        var request = new NodeMaintenanceRequest(cluster.Id, "n1");

        await _service.CordonAsync(request);

        var audit = await _harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditCategory.Node, audit.Category);
        Assert.Equal(AuditAction.Cordon, audit.Action);
        Assert.Contains("n1", audit.Target);
        Assert.Contains(cluster.Name, audit.Target);
    }

    [Fact]
    public async Task UncordonAsync_patches_unschedulable_false_and_writes_audit()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("uncordon-ok"));
        _k8s.SetupPatchNode("n1");
        var request = new NodeMaintenanceRequest(cluster.Id, "n1");

        await _service.UncordonAsync(request);

        var audit = await _harness.Db.AuditLogs.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AuditCategory.Node, audit.Category);
        Assert.Equal(AuditAction.Uncordon, audit.Action);
        Assert.Contains("n1", audit.Target);
    }

    [Fact]
    public async Task SetNodeSchedulable_missing_cluster_throws_not_found()
    {
        var request = new NodeMaintenanceRequest(999, "n1");

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CordonAsync(request));
    }

    [Fact]
    public async Task CordonAsync_k8s_404_translated_to_not_found()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("cordon-404"));
        _k8s.SetupPatchNodeThrows("n1", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CordonAsync(new NodeMaintenanceRequest(cluster.Id, "n1")));
    }

    [Fact]
    public async Task UncordonAsync_k8s_403_translated_to_permission()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("uncordon-403"));
        _k8s.SetupPatchNodeThrows("n1", K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.UncordonAsync(new NodeMaintenanceRequest(cluster.Id, "n1")));
    }

    [Fact]
    public async Task DrainPreflight_classifies_pods_by_owner()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-cls"));
        _k8s.SetupListPods(
            DrainPod("web-abc", "app", "ReplicaSet"),
            DrainPod("kube-proxy-zzz", "kube-system", "DaemonSet"),
            DrainPod("ad-hoc", "default", null));

        var preflight = await _service.GetNodeDrainPreflightAsync(new NodeDrainRequest(cluster.Id, "n1"));

        Assert.Equal(1, preflight.MigratablePods.Count);
        Assert.Equal("web-abc", preflight.MigratablePods[0].Name);
        Assert.Equal("app", preflight.MigratablePods[0].Namespace);
        Assert.Equal("ReplicaSet", preflight.MigratablePods[0].OwnerKind);
        Assert.Equal("kube-proxy-zzz", preflight.DaemonSetPods.Single().Name);
        Assert.Equal("ad-hoc", preflight.BarePods.Single().Name);
    }

    [Fact]
    public async Task DrainPreflight_k8s_error_translated_and_no_audit()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-err"));
        _k8s.SetupListPodsThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.GetNodeDrainPreflightAsync(new NodeDrainRequest(cluster.Id, "n1")));
        Assert.Empty(await _harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DrainPreflight_member_denied()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns(TestHttpContext.For("member").Object.HttpContext);

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.GetNodeDrainPreflightAsync(new NodeDrainRequest(999, "n1")));
    }

    [Fact]
    public async Task DrainNode_evicts_migratable_skips_daemonset_and_bare()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-ok"));
        _k8s.SetupReadNode("n1", RawNode("n1", unschedulable: false));
        _k8s.SetupListPods(
            DrainPod("web-a", "app", "ReplicaSet"),
            DrainPod("web-b", "app", "ReplicaSet"),
            DrainPod("ds-1", "kube-system", "DaemonSet"),
            DrainPod("bare-1", "default", null));
        _k8s.SetupPatchNode("n1");
        _k8s.SetupEvictPod("app", "web-a");
        _k8s.SetupEvictPod("app", "web-b");

        var report = await _service.DrainNodeAsync(new NodeDrainRequest(cluster.Id, "n1"));

        Assert.Equal(2, report.Evicted);
        Assert.Equal(1, report.Skipped);
        Assert.Equal(0, report.Blocked);
        _k8s.VerifyPodEvicted("app", "web-a", Times.Once());
        _k8s.VerifyPodEvicted("app", "web-b", Times.Once());
        _k8s.VerifyNodePatched("n1", Times.Once());
        var audits = await _harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, audits.Count);
        var drain = audits.Single(a => a.Action == AuditAction.Drain);
        Assert.Equal(AuditCategory.Node, drain.Category);
        Assert.Contains("成功 2/跳过 1/阻塞 0", drain.Target);
        Assert.Contains(cluster.Name, drain.Target);
    }

    [Fact]
    public async Task DrainNode_already_cordoned_does_not_repeat_cordon_audit()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-cordoned"));
        _k8s.SetupReadNode("n1", RawNode("n1", unschedulable: true));
        _k8s.SetupListPods(DrainPod("web-a", "app", "ReplicaSet"));
        _k8s.SetupEvictPod("app", "web-a");

        var report = await _service.DrainNodeAsync(new NodeDrainRequest(cluster.Id, "n1"));

        Assert.Equal(1, report.Evicted);
        _k8s.VerifyNodePatched("n1", Times.Never());
        var audits = await _harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        var drain = audits.Single();
        Assert.Equal(AuditAction.Drain, drain.Action);
    }

    [Fact]
    public async Task DrainNode_429_blocks_and_continues()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-pdb"));
        _k8s.SetupReadNode("n1", RawNode("n1", unschedulable: true));
        _k8s.SetupListPods(
            DrainPod("quota-1", "app", "StatefulSet"),
            DrainPod("free-1", "app", "ReplicaSet"));
        _k8s.SetupEvictPodBlocked("app", "quota-1");
        _k8s.SetupEvictPod("app", "free-1");

        var report = await _service.DrainNodeAsync(new NodeDrainRequest(cluster.Id, "n1"));

        Assert.Equal(1, report.Evicted);
        Assert.Equal(1, report.Blocked);
        var blocked = report.BlockedPods.Single();
        Assert.Equal("quota-1", blocked.Name);
        Assert.Equal("app", blocked.Namespace);
        var audits = await _harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        var drain = audits.Single();
        Assert.Contains("成功 1/跳过 0/阻塞 1", drain.Target);
    }

    [Fact]
    public async Task DrainNode_member_denied_no_k8s_mutation()
    {
        _accessor.SetupGet(a => a.HttpContext).Returns(TestHttpContext.For("member").Object.HttpContext);
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-member"));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.DrainNodeAsync(new NodeDrainRequest(cluster.Id, "n1")));
        _k8s.VerifyNodePatched("n1", Times.Never());
        _k8s.VerifyPodEvictionNever();
    }

    [Fact]
    public async Task DrainNode_missing_cluster_throws_not_found()
    {
        _k8s.SetupListPods();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DrainNodeAsync(new NodeDrainRequest(999, "n1")));
    }

    [Fact]
    public async Task DrainNode_reports_progress_sequence()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("drain-progress"));
        _k8s.SetupReadNode("n1", RawNode("n1", unschedulable: true));
        _k8s.SetupListPods(
            DrainPod("p-1", "app", "ReplicaSet"),
            DrainPod("p-2", "app", "ReplicaSet"),
            DrainPod("p-3", "app", "ReplicaSet"));
        _k8s.SetupEvictPod("app", "p-1");
        _k8s.SetupEvictPod("app", "p-2");
        _k8s.SetupEvictPod("app", "p-3");
        var reports = new List<NodeDrainProgress>();

        await _service.DrainNodeAsync(new NodeDrainRequest(cluster.Id, "n1"),
            new DrainNodeProgressRecorder(p => reports.Add(p)));

        Assert.Equal(
            [new NodeDrainProgress(1, 3), new NodeDrainProgress(2, 3), new NodeDrainProgress(3, 3)],
            reports);
    }

    private static V1Pod DrainPod(string name, string ns, string? ownerKind)
    {
        var pod = new V1Pod
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns }
        };
        if (ownerKind is not null)
        {
            pod.Metadata.OwnerReferences = [new V1OwnerReference { Kind = ownerKind, Name = "owner", Uid = "uid", ApiVersion = "v1" }];
        }

        return pod;
    }

    private static V1Node RawNode(string name, bool unschedulable)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name },
            Spec = new V1NodeSpec { Unschedulable = unschedulable }
        };

    private sealed class DrainNodeProgressRecorder(Action<NodeDrainProgress> onReport) : IProgress<NodeDrainProgress>
    {
        public void Report(NodeDrainProgress value) => onReport(value);
    }

    private async Task<int> SeedAsyncWithRemark()
    {
        var cluster = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("remark-cluster"));
        cluster.NodeIpRemarks.Add(TestData.NewIpRemark(cluster.Id, "node-1", "10.0.0.1", "管理口"));
        await _harness.ClusterRepo.UpdateAsync(cluster);
        return cluster.Id;
    }
}
