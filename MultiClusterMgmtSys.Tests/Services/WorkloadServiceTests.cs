using k8s;
using k8s.Autorest;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class WorkloadServiceTests
{
    [Fact]
    public async Task ListDeployments_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Workload(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.ListDeploymentsAsync(new WorkloadQueryRequest(999, "default")));
    }

    [Fact]
    public async Task GetDeployment_MissingCluster_ReturnsNull()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Workload(db, TestServices.ThrowingFactory());

        var result = await svc.GetDeploymentAsync(new WorkloadKeyRequest(999, "app", "default"));

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateDeployment_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.Workload(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(999, "{}")));
    }

    [Fact]
    public async Task CreateDeployment_YamlWithoutNamespace_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();
        var svc = TestServices.Workload(db, TestServices.ThrowingFactory());

        var yaml = """
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: app
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(1, yaml)));

        Assert.Contains("namespace", ex.UserMessage);
    }

    [Fact]
    public async Task CreateStatefulSet_BadYaml_ThrowsValidationWithChineseMessage()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();
        var svc = TestServices.Workload(db, TestServices.ThrowingFactory());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateStatefulSetFromYamlAsync(new WorkloadCreateRequest(1, "not-a-valid-yaml: [")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task DeleteDeployment_K8s404_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.DeleteNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<V1DeleteOptions>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 404 }));

        var svc = TestServices.Workload(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => svc.DeleteDeploymentAsync(new WorkloadKeyRequest(1, "app", "default")));

        Assert.Contains("删除部署", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateDeployment_K8s409OnRead_ThrowsConflict()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.ReadNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 409 }));

        var svc = TestServices.Workload(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => svc.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(1, "app", "default", "{}")));

        Assert.Contains("修改", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateDeployment_OverwritesOnlySpec_KeepsStatusAndMetadata()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var existing = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = "app",
                NamespaceProperty = "default",
                ResourceVersion = "100",
                Labels = new Dictionary<string, string> { ["server"] = "owned" }
            },
            Spec = new V1DeploymentSpec { Replicas = 2 },
            Status = new V1DeploymentStatus { ReadyReplicas = 2, ObservedGeneration = 1 }
        };

        V1Deployment? replaced = null;
        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.ReadNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = existing });
        fake.Setup(c => c.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .Callback<V1Deployment, string, string, string, string, string, bool?, IReadOnlyDictionary<string, IReadOnlyList<string>>, CancellationToken>(
                (body, _, _, _, _, _, _, _, _) => replaced = body)
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = existing });

        var svc = TestServices.Workload(db, _ => fake.Object);
        var yaml = """
            apiVersion: apps/v1
            kind: Deployment
            metadata:
              name: app
              labels:
                user: edit
            spec:
              replicas: 5
            """;

        await svc.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(1, "app", "default", yaml));

        Assert.NotNull(replaced);
        // 方案 A:仅 spec 覆盖;metadata/status 以服务器最新值为准
        Assert.Equal(5, replaced!.Spec.Replicas);
        Assert.Equal("100", replaced.Metadata.ResourceVersion);
        Assert.Equal("owned", replaced.Metadata.Labels["server"]);
        Assert.Equal(2, replaced.Status.ReadyReplicas);
        Assert.Equal(1, replaced.Status.ObservedGeneration);
    }

    [Fact]
    public async Task ScaleDeployment_UsesScaleSubresource_UpdatesReplicasAndAudits()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var scale = new V1Scale
        {
            Metadata = new V1ObjectMeta { Name = "app", NamespaceProperty = "default" },
            Spec = new V1ScaleSpec { Replicas = 2 }
        };

        V1Scale? replaced = null;
        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.ReadNamespacedDeploymentScaleWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = scale });
        fake.Setup(c => c.AppsV1.ReplaceNamespacedDeploymentScaleWithHttpMessagesAsync(
                It.IsAny<V1Scale>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .Callback<V1Scale, string, string, string, string, string, bool?, IReadOnlyDictionary<string, IReadOnlyList<string>>, CancellationToken>(
                (body, _, _, _, _, _, _, _, _) => replaced = body)
            .ReturnsAsync(new HttpOperationResponse<V1Scale> { Body = scale });

        var svc = TestServices.Workload(db, _ => fake.Object);

        await svc.ScaleDeploymentAsync(new WorkloadScaleRequest(1, "app", "default", 4));

        Assert.NotNull(replaced);
        Assert.Equal(4, replaced!.Spec.Replicas);

        var audit = db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Workload, audit.Category);
        Assert.Equal(AuditAction.Scale, audit.Action);
        Assert.Contains("default/app", audit.Target);
        Assert.Contains("→ 4", audit.Target);
    }

    [Fact]
    public async Task RestartDeployment_PatchesRestartedAtAnnotationAndAudits()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        V1Patch? patched = null;
        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.PatchNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Patch>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .Callback<V1Patch, string, string, string, string, string, bool?, bool?, IReadOnlyDictionary<string, IReadOnlyList<string>>, CancellationToken>(
                (body, _, _, _, _, _, _, _, _, _) => patched = body)
            .ReturnsAsync(new HttpOperationResponse<V1Deployment> { Body = new V1Deployment() });

        var svc = TestServices.Workload(db, _ => fake.Object);

        await svc.RestartDeploymentAsync(new WorkloadKeyRequest(1, "app", "default"));

        Assert.NotNull(patched);
        Assert.Contains("kubectl.kubernetes.io/restartedAt", patched!.Content.ToString());

        var audit = db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Workload, audit.Category);
        Assert.Equal(AuditAction.Restart, audit.Action);
        Assert.Contains("default/app", audit.Target);
    }

    [Fact]
    public async Task ListDaemonSets_K8s403_ThrowsPermission()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.ListNamespacedDaemonSetWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 403 }));

        var svc = TestServices.Workload(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => svc.ListDaemonSetsAsync(new WorkloadQueryRequest(1, "kube-system")));

        Assert.Equal("没有权限执行该操作", ex.UserMessage);
    }

    [Fact]
    public async Task DeleteStatefulSet_Success_WritesAuditWithKindLabel()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.DeleteNamespacedStatefulSetWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<V1DeleteOptions>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Status> { Body = new V1Status() });

        var svc = TestServices.Workload(db, _ => fake.Object);

        await svc.DeleteStatefulSetAsync(new WorkloadKeyRequest(1, "web", "default"));

        var audit = db.AuditLogs.Single();
        Assert.Equal(AuditAction.Delete, audit.Action);
        Assert.Contains("有状态应用: default/web @ 集群 c", audit.Target);
    }
}
