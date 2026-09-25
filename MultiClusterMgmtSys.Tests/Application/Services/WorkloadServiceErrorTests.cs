using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class WorkloadServiceErrorTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();
    private readonly WorkloadService _service;

    public WorkloadServiceErrorTests()
    {
        _service = new WorkloadService(
            _harness.ClusterRepo, _harness.Audit, NullLogger<WorkloadService>.Instance, K8sMocks.Cache(_k8s));
    }

    public void Dispose() => _harness.Dispose();

    private Task<int> SeedAsync()
        => _harness.ClusterRepo.AddAsync(TestData.NewCluster("err-cluster")).ContinueWith(t => t.Result.Id);

    [Fact]
    public async Task ListStatefulSetsAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListStatefulSetsThrows(K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListStatefulSetsAsync(new WorkloadQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task ListDaemonSetsAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListDaemonSetsThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.ListDaemonSetsAsync(new WorkloadQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task ListReplicaSetsAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListReplicaSetsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListReplicaSetsAsync(new WorkloadQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetStatefulSetAsync_k8s_404_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadStatefulSetThrows("ghost", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetStatefulSetAsync(new WorkloadKeyRequest(clusterId, "ghost", "app")));
    }

    [Fact]
    public async Task GetDaemonSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadDaemonSetThrows("ds-x", "app", K8sMocks.K8sError(500));

        var request = new WorkloadKeyRequest(clusterId, "ds-x", "app");
        var ex = await Record.ExceptionAsync(() => _service.GetDaemonSetAsync(request));

        Assert.IsNotAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task GetReplicaSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadReplicaSetThrows("rs-x", "app", K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.GetReplicaSetAsync(new WorkloadKeyRequest(clusterId, "rs-x", "app")));
    }

    [Fact]
    public async Task CreateStatefulSetFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateStatefulSetThrows("app", K8sMocks.K8sError(409));

        var yaml = """
            apiVersion: apps/v1
            kind: StatefulSet
            metadata:
              name: sts-x
              namespace: app
            spec:
              serviceName: svc
            """;
        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateStatefulSetFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml)));
    }

    [Fact]
    public async Task CreateDaemonSetFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateDaemonSetFromYamlAsync(new WorkloadCreateRequest(clusterId, "{ broken")));
    }

    [Fact]
    public async Task CreateReplicaSetFromYamlAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateReplicaSetThrows("app", K8sMocks.K8sError(404));

        var yaml = """
            apiVersion: apps/v1
            kind: ReplicaSet
            metadata:
              name: rs-x
              namespace: app
            spec:
              replicas: 1
              selector:
                matchLabels:
                  app: x
              template:
                metadata:
                  labels:
                    app: x
                spec:
                  containers:
                    - name: c
                      image: nginx
            """;
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateReplicaSetFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml)));
    }

    [Fact]
    public async Task UpdateStatefulSetFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadStatefulSet("sts-1", "app", new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "sts-1", NamespaceProperty = "app" },
            Spec = new V1StatefulSetSpec { Replicas = 1, ServiceName = "svc" }
        });
        _k8s.SetupReplaceStatefulSet("sts-1", "app");

        var yaml = """
            apiVersion: apps/v1
            kind: StatefulSet
            metadata:
              name: sts-1
              namespace: app
            spec:
              serviceName: svc
              replicas: 5
              selector:
                matchLabels:
                  app: x
              template:
                metadata:
                  labels:
                    app: x
                spec:
                  containers:
                    - name: c
                      image: nginx
            """;
        await _service.UpdateStatefulSetFromYamlAsync(new WorkloadUpdateRequest(clusterId, "sts-1", "app", yaml));

        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task UpdateDaemonSetFromYamlAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadDaemonSetThrows("ds-1", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateDaemonSetFromYamlAsync(new WorkloadUpdateRequest(clusterId, "ds-1", "app", "{ }")));
    }

    [Fact]
    public async Task UpdateReplicaSetFromYamlAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadReplicaSetThrows("rs-1", "app", new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.UpdateReplicaSetFromYamlAsync(new WorkloadUpdateRequest(clusterId, "rs-1", "app", "{ }")));
    }

    [Fact]
    public async Task ScaleStatefulSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadStatefulSetScale("sts-1", "app", currentReplicas: 2);

        var request = new WorkloadScaleRequest(clusterId, "sts-1", "app", Replicas: 3);
        var ex = await Record.ExceptionAsync(() => _service.ScaleStatefulSetAsync(request));

        Assert.IsNotAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task ScaleReplicaSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadReplicaSetScale("rs-1", "app", currentReplicas: 2);

        var request = new WorkloadScaleRequest(clusterId, "rs-1", "app", Replicas: 3);
        var ex = await Record.ExceptionAsync(() => _service.ScaleReplicaSetAsync(request));

        Assert.IsNotAssignableFrom<BusinessException>(ex);
    }

    [Fact]
    public async Task DeleteStatefulSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupDeleteStatefulSetThrows("sts-1", "app", K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.DeleteStatefulSetAsync(new WorkloadKeyRequest(clusterId, "sts-1", "app")));
    }

    [Fact]
    public async Task DeleteDaemonSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupDeleteDaemonSetThrows("ds-1", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteDaemonSetAsync(new WorkloadKeyRequest(clusterId, "ds-1", "app")));
    }

    [Fact]
    public async Task DeleteReplicaSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupDeleteReplicaSetThrows("rs-1", "app", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteReplicaSetAsync(new WorkloadKeyRequest(clusterId, "rs-1", "app")));
    }

    [Fact]
    public async Task RestartDaemonSetAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupPatchDaemonSetThrows("ds-1", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RestartDaemonSetAsync(new WorkloadKeyRequest(clusterId, "ds-1", "app")));
    }
}
