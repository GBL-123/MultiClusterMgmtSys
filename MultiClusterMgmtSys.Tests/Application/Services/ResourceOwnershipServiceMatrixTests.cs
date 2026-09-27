using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class ResourceOwnershipServiceMatrixTests : IDisposable
{
    private const string OwnerKey = ResourceOwnershipKeys.OwnerUidLabel;

    private const string ConfigMapYamlForged = """
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: cm-forged
          namespace: app
          labels:
            mcms.ms/owner-uid: "99"
          annotations:
            mcms.ms/owner-name: "mallory"
        data:
          key1: value1
        """;

    private const string SvcYaml = """
        apiVersion: v1
        kind: Service
        metadata:
          name: web-svc
          namespace: app
        spec:
          type: ClusterIP
          ports:
            - port: 80
              targetPort: 8080
        """;

    private const string SvcYamlForbiddenNs = """
        apiVersion: v1
        kind: Service
        metadata:
          name: web-svc
          namespace: kube-system
        spec:
          type: ClusterIP
          ports:
            - port: 80
        """;

    private readonly ServiceHarness _harness;

    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();

    public ResourceOwnershipServiceMatrixTests()
    {
        _harness = new ServiceHarness("alice");
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedAsync(string name = "matrix-cluster")
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static V1ConfigMap NewConfigMap(string name, string ns, string? owner = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null ? new Dictionary<string, string>() : new Dictionary<string, string> { [OwnerKey] = owner }
            },
            Data = new Dictionary<string, string> { ["key1"] = "value1" }
        };

    private static V1Service NewSvc(string name, string ns, string? owner = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null ? new Dictionary<string, string>() : new Dictionary<string, string> { [OwnerKey] = owner }
            },
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                ClusterIP = "10.96.0.10",
                Ports = [new V1ServicePort { Port = 80 }]
            }
        };

    [Fact]
    public async Task ConfigMap_create_member_stamps_and_overrides_forge()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateConfigMap("app");
        var service = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, ConfigMapYamlForged));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
            It.Is<V1ConfigMap>(cm => cm.Metadata!.Labels![OwnerKey] == "7"
                && cm.Metadata!.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation] == "alice"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigMap_create_member_forbidden_namespace_rejected()
    {
        var clusterId = await SeedAsync();
        var service = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        var yaml = ConfigMapYamlForged.Replace("namespace: app", "namespace: kube-system");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);
    }

    [Fact]
    public async Task ConfigMap_create_admin_creates_into_kube_namespace_allowed()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateConfigMap("kube-system");
        var service = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        await service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(
            clusterId, ConfigMapYamlForged.Replace("namespace: app", "namespace: kube-system")));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
            It.IsAny<V1ConfigMap>(), "kube-system",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigMap_update_owner_passes_and_unowned_member_denied()
    {
        var clusterId = await SeedAsync();
        var yaml = ValidConfigMapYaml("cm", "app", ownerUid: "7");
        _k8s.SetupReadConfigMap("cm", "app", NewConfigMap("cm", "app", owner: "7"));
        _k8s.SetupReplaceConfigMap("cm", "app");
        var ownerService = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await ownerService.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm", "app", yaml));
        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);

        _k8s.SetupReadConfigMap("cm2", "app", NewConfigMap("cm2", "app", owner: "8"));
        var nonOwnerService = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("bob", 9).Object);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => nonOwnerService.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm2", "app", ValidConfigMapYaml("cm2", "app"))));

        Assert.Contains("仅可操作自己创建", ex.UserMessage);
    }

    [Fact]
    public async Task ConfigMap_update_unowned_member_denied_admin_allowed()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadConfigMap("cm", "app", NewConfigMap("cm", "app"));
        var member = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => member.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm", "app", ValidConfigMapYaml("cm", "app"))));

        _k8s.SetupReadConfigMap("cm", "app", NewConfigMap("cm", "app"));
        _k8s.SetupReplaceConfigMap("cm", "app");
        var admin = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm", "app", ValidConfigMapYaml("cm", "app")));

        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);
    }

    [Fact]
    public async Task ConfigMap_delete_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadConfigMap("mine", "app", NewConfigMap("mine", "app", owner: "7"));
        _k8s.SetupDeleteConfigMap("mine", "app");
        var member = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await member.DeleteConfigMapAsync(new ConfigMapKeyRequest(clusterId, "mine", "app"));
        Assert.Equal(AuditAction.Delete, _harness.Db.AuditLogs.Single().Action);

        _k8s.SetupReadConfigMap("unowned", "app", NewConfigMap("unowned", "app"));
        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => member.DeleteConfigMapAsync(new ConfigMapKeyRequest(clusterId, "unowned", "app")));

        _k8s.SetupReadConfigMap("unowned", "app", NewConfigMap("unowned", "app"));
        _k8s.SetupDeleteConfigMap("unowned", "app");
        var admin = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.DeleteConfigMapAsync(new ConfigMapKeyRequest(clusterId, "unowned", "app"));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task ConfigMap_list_and_detail_project_can_operate()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListConfigMaps(NewConfigMap("mine", "app", owner: "7"), NewConfigMap("unowned", "app"));
        var service = ConfigMapServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        var items = await service.ListConfigMapsAsync(new ConfigMapQueryRequest(clusterId, null));

        Assert.True(items.Single(item => item.Name == "mine").CanOperate);
        Assert.False(items.Single(item => item.Name == "unowned").CanOperate);

        _k8s.SetupReadConfigMap("mine", "app", NewConfigMap("mine", "app", owner: "7"));
        var detail = await service.GetConfigMapAsync(new ConfigMapKeyRequest(clusterId, "mine", "app"));
        Assert.True(detail!.CanOperate);

        _k8s.SetupReadConfigMap("unowned", "app", NewConfigMap("unowned", "app"));
        var unownedDetail = await service.GetConfigMapAsync(new ConfigMapKeyRequest(clusterId, "unowned", "app"));
        Assert.False(unownedDetail!.CanOperate);
    }

    [Fact]
    public async Task Svc_create_member_stamps_and_blacklist_rejects()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateService("app");
        var service = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.CreateSvcFromYamlAsync(new SvcCreateRequest(clusterId, SvcYaml));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedServiceWithHttpMessagesAsync(
            It.Is<V1Service>(s => s.Metadata!.Labels![OwnerKey] == "7"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateSvcFromYamlAsync(new SvcCreateRequest(clusterId, SvcYamlForbiddenNs)));

        Assert.Contains("系统命名空间", ex.UserMessage);
    }

    [Fact]
    public async Task Svc_update_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        var yaml = ValidSvcYaml("web-svc", "app");
        _k8s.SetupReadService("web-svc", "app", NewSvc("web-svc", "app", owner: "7"));
        _k8s.SetupReplaceService("web-svc", "app");
        var ownerService = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await ownerService.UpdateSvcFromYamlAsync(new SvcUpdateRequest(clusterId, "web-svc", "app", yaml));
        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);

        var unowned = await SeedAsync("svc-unowned");
        _k8s.SetupReadService("other", "app", NewSvc("other", "app"));
        var member = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("bob", 9).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => member.UpdateSvcFromYamlAsync(new SvcUpdateRequest(unowned, "other", "app", ValidSvcYaml("other", "app"))));

        _k8s.SetupReadService("other", "app", NewSvc("other", "app"));
        _k8s.SetupReplaceService("other", "app");
        var admin = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.UpdateSvcFromYamlAsync(new SvcUpdateRequest(unowned, "other", "app", ValidSvcYaml("other", "app")));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task Svc_delete_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadService("web-svc", "app", NewSvc("web-svc", "app", owner: "7"));
        _k8s.SetupDeleteService("web-svc", "app");
        var member = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await member.DeleteSvcAsync(new SvcKeyRequest(clusterId, "web-svc", "app"));
        Assert.Equal(AuditAction.Delete, _harness.Db.AuditLogs.Single().Action);

        _k8s.SetupReadService("unowned", "app", NewSvc("unowned", "app"));
        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => member.DeleteSvcAsync(new SvcKeyRequest(clusterId, "unowned", "app")));

        _k8s.SetupReadService("unowned", "app", NewSvc("unowned", "app"));
        _k8s.SetupDeleteService("unowned", "app");
        var admin = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.DeleteSvcAsync(new SvcKeyRequest(clusterId, "unowned", "app"));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task Svc_list_detail_project_can_operate()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListServices(NewSvc("mine", "app", owner: "7"), NewSvc("unowned", "app"));
        var service = SvcServiceTests.NewTestService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        var items = await service.ListSvcsAsync(new SvcQueryRequest(clusterId, null));

        Assert.True(items.Single(item => item.Name == "mine").CanOperate);
        Assert.False(items.Single(item => item.Name == "unowned").CanOperate);

        _k8s.SetupReadService("mine", "app", NewSvc("mine", "app", owner: "7"));
        var detail = await service.GetSvcAsync(new SvcKeyRequest(clusterId, "mine", "app"));
        Assert.True(detail!.CanOperate);
    }

    private static string ValidConfigMapYaml(string name, string ns, string? ownerUid = null)
    {
        var lines = new List<string>
        {
            "apiVersion: v1",
            "kind: ConfigMap",
            "metadata:",
            $"  name: {name}",
            $"  namespace: {ns}"
        };
        if (ownerUid is not null)
        {
            lines.Add("  labels:");
            lines.Add($"    {ResourceOwnershipKeys.OwnerUidLabel}: \"{ownerUid}\"");
        }
        lines.Add("data:");
        lines.Add("  key1: value1");
        return string.Join('\n', lines);
    }

    private static string ValidSvcYaml(string name, string ns) => $"""
        apiVersion: v1
        kind: Service
        metadata:
          name: {name}
          namespace: {ns}
        spec:
          type: ClusterIP
          ports:
            - port: 80
              targetPort: 8080
        """;
}

