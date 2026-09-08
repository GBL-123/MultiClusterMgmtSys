using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class WorkloadServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly WorkloadService service;

    public WorkloadServiceTests()
    {
        service = new WorkloadService(
            harness.ClusterRepo, harness.Audit, NullLogger<WorkloadService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    private Task<int> SeedAsync()
        => harness.ClusterRepo.AddAsync(TestData.NewCluster("wl-cluster")).ContinueWith(t => t.Result.Id);

    private const string DeploymentYaml = """
        apiVersion: apps/v1
        kind: Deployment
        metadata:
          name: web
          namespace: app
        spec:
          replicas: 3
          selector:
            matchLabels:
              app: web
          template:
            metadata:
              labels:
                app: web
            spec:
              containers:
                - name: web
                  image: nginx
        """;

    [Fact]
    public async Task GetNamespacesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetNamespacesAsync(999));
    }

    [Fact]
    public async Task ListDeploymentsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ListDeploymentsAsync(new WorkloadQueryRequest(999, null)));
    }

    [Fact]
    public async Task ListDeploymentsAsync_null_namespace_lists_all()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListDeployments(new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } });

        var items = await service.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, null));

        var vm = Assert.Single(items);
        Assert.Equal("web", vm.Name);
        Assert.Equal(WorkloadKind.Deployment, vm.Kind);
    }

    [Fact]
    public async Task ListDeploymentsAsync_namespaced_when_namespace_set()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespacedDeployments("app", new V1Deployment { Metadata = new V1ObjectMeta { Name = "ns-web", NamespaceProperty = "app" } });

        var items = await service.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, "app"));

        Assert.Equal("ns-web", Assert.Single(items).Name);
    }

    [Fact]
    public async Task ListDeploymentsAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListDeploymentsThrows(K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetDeploymentAsync_missing_cluster_returns_null()
    {
        Assert.Null(await service.GetDeploymentAsync(new WorkloadKeyRequest(999, "web", "app")));
    }

    [Fact]
    public async Task GetDeploymentAsync_maps_detail()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDeployment("web", "app", new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", Uid = "uid-1" },
            Spec = new V1DeploymentSpec { Replicas = 3 }
        });

        var detail = await service.GetDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        Assert.NotNull(detail);
        Assert.Equal("web", detail!.Name);
        Assert.Equal(WorkloadKind.Deployment, detail.Kind);
        Assert.Equal(3, detail.DesiredCount);
        Assert.Equal("uid-1", detail.Uid);
    }

    [Fact]
    public async Task GetDeploymentAsync_k8s_404_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDeploymentThrows("ghost", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetDeploymentAsync(new WorkloadKeyRequest(clusterId, "ghost", "app")));
    }

    [Fact]
    public async Task CreateDeploymentFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, "{ broken")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task CreateDeploymentFromYamlAsync_missing_namespace_throws_validation()
    {
        var clusterId = await SeedAsync();
        var yaml = DeploymentYaml.Replace("  namespace: app\n", "");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.namespace", ex.UserMessage);
    }

    [Fact]
    public async Task CreateDeploymentFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateDeployment("app");

        await service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, DeploymentYaml));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Workload, audit.Category);
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("web", audit.Target);
    }

    [Fact]
    public async Task CreateDeploymentFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateDeploymentThrows("app", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, DeploymentYaml)));
    }

    [Fact]
    public async Task UpdateDeploymentFromYamlAsync_replaces_spec_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDeployment("web", "app", new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1DeploymentSpec { Replicas = 1 }
        });
        k8s.SetupReplaceDeployment("web", "app");

        var yaml = DeploymentYaml.Replace("replicas: 3", "replicas: 7");
        await service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", yaml));

        k8s.Verify(x => x.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
            It.Is<V1Deployment>(d => d.Spec!.Replicas == 7),
            "web", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateDeploymentFromYamlAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDeploymentThrows("ghost", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "ghost", "app", DeploymentYaml)));
    }

    [Fact]
    public async Task DeleteDeploymentAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteDeployment("web", "app");

        await service.DeleteDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteDeploymentAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteDeploymentThrows("web", "app", new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => service.DeleteDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app")));
    }

    [Fact]
    public async Task ScaleDeploymentAsync_updates_replicas_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDeploymentScale("web", "app", currentReplicas: 3);
        k8s.SetupReplaceDeploymentScale("web", "app");

        await service.ScaleDeploymentAsync(new WorkloadScaleRequest(clusterId, "web", "app", Replicas: 5));

        k8s.Verify(x => x.AppsV1.ReplaceNamespacedDeploymentScaleWithHttpMessagesAsync(
            It.Is<V1Scale>(s => s.Spec!.Replicas == 5),
            "web", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Scale, audit.Action);
        Assert.Contains("5", audit.Target);
    }

    [Fact]
    public async Task ScaleDeploymentAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReplaceDeploymentScaleThrows("web", "app", K8sMocks.K8sError(500));

        var request = new WorkloadScaleRequest(clusterId, "web", "app", Replicas: 2);
        var ex = await Record.ExceptionAsync(() => service.ScaleDeploymentAsync(request));
        Assert.NotNull(ex);
        Assert.IsNotAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task RestartDeploymentAsync_patches_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupPatchDeployment("web", "app");

        await service.RestartDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        k8s.Verify(x => x.AppsV1.PatchNamespacedDeploymentWithHttpMessagesAsync(
            It.IsAny<V1Patch>(),
            "web", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Restart, audit.Action);
    }

    [Fact]
    public async Task RestartDeploymentAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupPatchDeploymentThrows("web", "app", K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => service.RestartDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app")));
    }

    [Fact]
    public async Task GetStatefulSetAsync_maps_detail()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadStatefulSet("sts-1", "app", new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "sts-1", NamespaceProperty = "app", Uid = "uid-2" },
            Spec = new V1StatefulSetSpec { Replicas = 2, ServiceName = "headless" },
            Status = new V1StatefulSetStatus { ReadyReplicas = 2, CurrentRevision = "rev-1", UpdateRevision = "rev-1" }
        });

        var detail = await service.GetStatefulSetAsync(new WorkloadKeyRequest(clusterId, "sts-1", "app"));

        Assert.NotNull(detail);
        Assert.Equal(WorkloadKind.StatefulSet, detail!.Kind);
        Assert.Equal(2, detail.DesiredCount);
        Assert.Equal("uid-2", detail.Uid);
    }

    [Fact]
    public async Task GetDaemonSetAsync_maps_detail()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadDaemonSet("ds-1", "app");

        var detail = await service.GetDaemonSetAsync(new WorkloadKeyRequest(clusterId, "ds-1", "app"));

        Assert.NotNull(detail);
        Assert.Equal(WorkloadKind.DaemonSet, detail!.Kind);
    }

    [Fact]
    public async Task GetReplicaSetAsync_maps_detail()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadReplicaSet("rs-1", "app");

        var detail = await service.GetReplicaSetAsync(new WorkloadKeyRequest(clusterId, "rs-1", "app"));

        Assert.NotNull(detail);
        Assert.Equal(WorkloadKind.ReplicaSet, detail!.Kind);
    }

    [Fact]
    public async Task ListStatefulSetsAsync_maps_items()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListStatefulSets(new V1StatefulSet { Metadata = new V1ObjectMeta { Name = "sts-1" } });

        var items = await service.ListStatefulSetsAsync(new WorkloadQueryRequest(clusterId, null));

        Assert.Single(items);
        Assert.Equal(WorkloadKind.StatefulSet, items[0].Kind);
    }

    [Fact]
    public async Task ListDaemonSetsAsync_maps_items()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListDaemonSets(new V1DaemonSet { Metadata = new V1ObjectMeta { Name = "ds-1" } });

        var items = await service.ListDaemonSetsAsync(new WorkloadQueryRequest(clusterId, null));

        Assert.Single(items);
        Assert.Equal(WorkloadKind.DaemonSet, items[0].Kind);
    }

    [Fact]
    public async Task ListReplicaSetsAsync_maps_items()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListReplicaSets(new V1ReplicaSet { Metadata = new V1ObjectMeta { Name = "rs-1" } });

        var items = await service.ListReplicaSetsAsync(new WorkloadQueryRequest(clusterId, null));

        Assert.Single(items);
        Assert.Equal(WorkloadKind.ReplicaSet, items[0].Kind);
    }

    [Fact]
    public async Task CreateStatefulSetFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateStatefulSet("app");

        var yaml = DeploymentYaml.Replace("kind: Deployment", "kind: StatefulSet").Replace("name: web", "name: sts-web");
        await service.CreateStatefulSetFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Contains("有状态应用", audit.Target);
    }

    [Fact]
    public async Task CreateDaemonSetFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateDaemonSet("app");

        var yaml = DeploymentYaml.Replace("kind: Deployment", "kind: DaemonSet").Replace("name: web", "name: ds-web");
        await service.CreateDaemonSetFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Contains("守护进程", audit.Target);
    }

    [Fact]
    public async Task CreateReplicaSetFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateReplicaSet("app");

        var yaml = DeploymentYaml.Replace("kind: Deployment", "kind: ReplicaSet").Replace("name: web", "name: rs-web");
        await service.CreateReplicaSetFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Contains("副本集", audit.Target);
    }

    [Fact]
    public async Task ScaleStatefulSetAsync_updates_replicas_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadStatefulSetScale("sts-1", "app", currentReplicas: 2);
        k8s.SetupReplaceStatefulSetScale("sts-1", "app");

        await service.ScaleStatefulSetAsync(new WorkloadScaleRequest(clusterId, "sts-1", "app", Replicas: 4));

        k8s.Verify(x => x.AppsV1.ReplaceNamespacedStatefulSetScaleWithHttpMessagesAsync(
            It.Is<V1Scale>(s => s.Spec!.Replicas == 4),
            "sts-1", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(AuditAction.Scale, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task ScaleReplicaSetAsync_updates_replicas_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadReplicaSetScale("rs-1", "app", currentReplicas: 2);
        k8s.SetupReplaceReplicaSetScale("rs-1", "app");

        await service.ScaleReplicaSetAsync(new WorkloadScaleRequest(clusterId, "rs-1", "app", Replicas: 9));

        Assert.Equal(AuditAction.Scale, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task RestartStatefulSetAsync_audits_restart()
    {
        var clusterId = await SeedAsync();
        k8s.SetupPatchStatefulSet("sts-1", "app");

        await service.RestartStatefulSetAsync(new WorkloadKeyRequest(clusterId, "sts-1", "app"));

        Assert.Equal(AuditAction.Restart, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task RestartDaemonSetAsync_audits_restart()
    {
        var clusterId = await SeedAsync();
        k8s.SetupPatchDaemonSet("ds-1", "app");

        await service.RestartDaemonSetAsync(new WorkloadKeyRequest(clusterId, "ds-1", "app"));

        Assert.Equal(AuditAction.Restart, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task DeleteStatefulSetAsync_audits_delete()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteStatefulSet("sts-1", "app");

        await service.DeleteStatefulSetAsync(new WorkloadKeyRequest(clusterId, "sts-1", "app"));

        Assert.Equal(AuditAction.Delete, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task DeleteDaemonSetAsync_audits_delete()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteDaemonSet("ds-1", "app");

        await service.DeleteDaemonSetAsync(new WorkloadKeyRequest(clusterId, "ds-1", "app"));

        Assert.Equal(AuditAction.Delete, harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task DeleteReplicaSetAsync_audits_delete()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteReplicaSet("rs-1", "app");

        await service.DeleteReplicaSetAsync(new WorkloadKeyRequest(clusterId, "rs-1", "app"));

        Assert.Equal(AuditAction.Delete, harness.Db.AuditLogs.Single().Action);
    }
}
