using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class WorkloadServiceOwnershipTests : IDisposable
{
    private const string OwnerKey = ResourceOwnershipKeys.OwnerUidLabel;

    private const string DeploymentYaml = """
        apiVersion: apps/v1
        kind: Deployment
        metadata:
          name: web
          namespace: app
          labels:
            mcms.ms/owner-uid: "99"
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

    private readonly ServiceHarness _harness;

    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();

    public WorkloadServiceOwnershipTests()
    {
        _harness = new ServiceHarness("alice");
    }

    public void Dispose() => _harness.Dispose();

    private WorkloadService NewService(IHttpContextAccessor accessor)
        => WorkloadServiceTests.NewService(_harness, _k8s, accessor);

    private async Task<int> SeedAsync(string name = "own-cluster")
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static V1Deployment OwneredDeployment(int ownerUid = 7) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = "web",
            NamespaceProperty = "app",
            Labels = new Dictionary<string, string> { [OwnerKey] = ownerUid.ToString() }
        },
        Spec = new V1DeploymentSpec { Replicas = 1 }
    };

    private static void AddHelmAnnotations(V1ObjectMeta metadata, string releaseName, string releaseNamespace)
    {
        metadata.Annotations = new Dictionary<string, string>
        {
            [ResourceOwnershipKeys.HelmReleaseNameAnnotation] = releaseName,
            [ResourceOwnershipKeys.HelmReleaseNamespaceAnnotation] = releaseNamespace
        };
    }

    [Fact]
    public async Task CreateDeployment_member_stamps_own_ownership_and_audits()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateDeployment("app");
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, DeploymentYaml));

        _k8s.Verify(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
            It.Is<V1Deployment>(d => d.Metadata!.Labels![OwnerKey] == "7"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Equal("alice", audit.UserName);
    }

    [Fact]
    public async Task CreateDeployment_admin_stamps_admin_uid()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateDeployment("app");
        var service = NewService(TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        await service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, DeploymentYaml));

        _k8s.Verify(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
            It.Is<V1Deployment>(d => d.Metadata!.Labels![OwnerKey] == "1"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDeployment_anonymous_throws_and_no_k8s_call()
    {
        var clusterId = await SeedAsync();
        var service = NewService(TestHttpContext.Anonymous().Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, DeploymentYaml)));

        Assert.Empty(_harness.Db.AuditLogs);
    }

    [Fact]
    public async Task CreateDeployment_member_forbidden_namespace_rejected_before_k8s()
    {
        var clusterId = await SeedAsync();
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);
        var yaml = DeploymentYaml.Replace("namespace: app", "namespace: kube-system");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);
        _k8s.Verify(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
            It.IsAny<V1Deployment>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDeployment_owner_spec_overwrite_and_keeps_metadata()
    {
        var clusterId = await SeedAsync();
        var existing = new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = "web",
                NamespaceProperty = "app",
                Labels = new Dictionary<string, string>
                {
                    [OwnerKey] = "7",
                    ["keep"] = "me"
                }
            },
            Spec = new V1DeploymentSpec { Replicas = 1 }
        };
        _k8s.SetupReadDeployment("web", "app", existing);
        _k8s.SetupReplaceDeployment("web", "app");
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml));

        _k8s.Verify(x => x.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
            It.Is<V1Deployment>(d => d.Metadata!.Labels![OwnerKey] == "7" && d.Metadata!.Labels!["keep"] == "me"),
            "web", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task UpdateDeployment_non_owner_denied_before_k8s()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 8));
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml)));

        Assert.Contains("仅可操作自己创建", ex.UserMessage);
        _k8s.Verify(x => x.AppsV1.ReplaceNamespacedDeploymentWithHttpMessagesAsync(
            It.IsAny<V1Deployment>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_harness.Db.AuditLogs);
    }

    [Fact]
    public async Task UpdateDeployment_unowned_denied_for_member()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadDeployment("web", "app", new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } });
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml)));
    }

    [Fact]
    public async Task UpdateDeployment_unowned_allowed_for_admin()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadDeployment("web", "app", new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } });
        _k8s.SetupReplaceDeployment("web", "app");
        var service = NewService(TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        await service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml));

        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task UpdateDeployment_helm_interlock_ownership_repo_decides()
    {
        var clusterId = await SeedAsync();
        await _harness.OwnershipRepo.UpsertAsync(new HelmReleaseOwnership
        {
            ClusterId = clusterId,
            Namespace = "app",
            ReleaseName = "nginx",
            OwnerUserId = 7,
            OwnerUserName = "alice",
            InstalledAt = DateTime.UtcNow,
            InstalledRevision = 1
        });
        var existing = new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } };
        AddHelmAnnotations(existing.Metadata, "nginx", "app");
        _k8s.SetupReadDeployment("web", "app", existing);
        _k8s.SetupReplaceDeployment("web", "app");
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml));

        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task UpdateDeployment_helm_managed_without_record_denied_for_member()
    {
        var clusterId = await SeedAsync();
        var existing = new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } };
        AddHelmAnnotations(existing.Metadata, "ci-release", "app");
        _k8s.SetupReadDeployment("web", "app", existing);
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(clusterId, "web", "app", DeploymentYaml)));
    }

    [Fact]
    public async Task ScaleDeployment_owner_allowed_and_non_owner_denied()
    {
        var ownerCluster = await SeedAsync("scale-owner");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 7));
        _k8s.SetupReadDeploymentScale("web", "app", currentReplicas: 3);
        _k8s.SetupReplaceDeploymentScale("web", "app");
        var ownerService = NewService(TestHttpContext.ForIdentity("alice", 7).Object);
        await ownerService.ScaleDeploymentAsync(new WorkloadScaleRequest(ownerCluster, "web", "app", Replicas: 5));
        Assert.Equal(AuditAction.Scale, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task ScaleDeployment_non_owner_denied_before_scale_call()
    {
        var clusterId = await SeedAsync("scale-other");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 8));
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.ScaleDeploymentAsync(new WorkloadScaleRequest(clusterId, "web", "app", Replicas: 5)));

        _k8s.Verify(x => x.AppsV1.ReplaceNamespacedDeploymentScaleWithHttpMessagesAsync(
            It.IsAny<V1Scale>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RestartDeployment_non_owner_denied_before_patch()
    {
        var clusterId = await SeedAsync("restart-other");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 8));
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.RestartDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app")));

        _k8s.Verify(x => x.AppsV1.PatchNamespacedDeploymentWithHttpMessagesAsync(
            It.IsAny<V1Patch>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDeployment_owner_allowed()
    {
        var clusterId = await SeedAsync("del-owner");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 7));
        _k8s.SetupDeleteDeployment("web", "app");
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await service.DeleteDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        Assert.Equal(AuditAction.Delete, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task DeleteDeployment_non_owner_denied_and_no_audit()
    {
        var clusterId = await SeedAsync("del-other");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 8));
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.DeleteDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app")));

        Assert.Empty(_harness.Db.AuditLogs);
    }

    [Fact]
    public async Task ListDeployments_projects_can_operate_by_ownership()
    {
        var clusterId = await SeedAsync("list-own");
        _k8s.SetupListDeployments(
            OwneredDeployment(ownerUid: 7),
            new V1Deployment { Metadata = new V1ObjectMeta { Name = "no-label", NamespaceProperty = "app" } });
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        var items = await service.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, null));

        Assert.Equal(2, items.Count);
        Assert.True(items.Single(item => item.Name == "web").CanOperate);
        Assert.False(items.Single(item => item.Name == "no-label").CanOperate);
    }

    [Fact]
    public async Task ListDeployments_admin_sees_can_operate_everywhere()
    {
        var clusterId = await SeedAsync("list-admin");
        _k8s.SetupListDeployments(new V1Deployment { Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" } });
        var service = NewService(TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        var items = await service.ListDeploymentsAsync(new WorkloadQueryRequest(clusterId, null));

        Assert.True(Assert.Single(items).CanOperate);
    }

    [Fact]
    public async Task GetDeployment_detail_projects_can_operate()
    {
        var clusterId = await SeedAsync("detail-own");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 7));
        var service = NewService(TestHttpContext.ForIdentity("alice", 7).Object);

        var detail = await service.GetDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        Assert.True(detail!.CanOperate);
    }

    [Fact]
    public async Task GetDeployment_anonymous_gets_false_without_throw()
    {
        var clusterId = await SeedAsync("detail-anon");
        _k8s.SetupReadDeployment("web", "app", OwneredDeployment(ownerUid: 7));
        var service = NewService(TestHttpContext.Anonymous().Object);

        var detail = await service.GetDeploymentAsync(new WorkloadKeyRequest(clusterId, "web", "app"));

        Assert.False(detail!.CanOperate);
    }
}

