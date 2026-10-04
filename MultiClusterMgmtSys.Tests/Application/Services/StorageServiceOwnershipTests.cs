using System.Text;
using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class StorageServiceOwnershipTests : IDisposable
{
    private const string OwnerKey = ResourceOwnershipKeys.OwnerUidLabel;

    private const string ClaimYamlForged = """
        apiVersion: v1
        kind: PersistentVolumeClaim
        metadata:
          name: claim-forged
          namespace: app
          labels:
            mcms.ms/owner-uid: "99"
          annotations:
            mcms.ms/owner-name: "mallory"
        spec:
          accessModes:
          - ReadWriteOnce
          resources:
            requests:
              storage: 5Gi
        """;

    private readonly ServiceHarness _harness;

    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();

    public StorageServiceOwnershipTests()
    {
        _harness = new ServiceHarness("alice");
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedAsync(string name = "storage-matrix-cluster")
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static V1PersistentVolumeClaim NewClaim(string name, string ns, string? owner = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null ? new Dictionary<string, string>() : new Dictionary<string, string> { [OwnerKey] = owner }
            },
            Spec = new V1PersistentVolumeClaimSpec
            {
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new ResourceQuantity("5Gi") }
                }
            }
        };

    private static string ValidClaimYaml(string name, string ns, string? ownerUid = null)
    {
        var lines = new List<string>
        {
            "apiVersion: v1",
            "kind: PersistentVolumeClaim",
            "metadata:",
            $"  name: {name}",
            $"  namespace: {ns}"
        };
        if (ownerUid is not null)
        {
            lines.Add("  labels:");
            lines.Add($"    {ResourceOwnershipKeys.OwnerUidLabel}: \"{ownerUid}\"");
        }

        lines.Add("spec:");
        lines.Add("  accessModes:");
        lines.Add("  - ReadWriteOnce");
        lines.Add("  resources:");
        lines.Add("    requests:");
        lines.Add("      storage: 5Gi");
        return string.Join('\n', lines);
    }

    [Fact]
    public async Task Claim_create_member_blacklist_rejected_admin_allowed()
    {
        var clusterId = await SeedAsync();
        var service = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        var yaml = ClaimYamlForged.Replace("namespace: app", "namespace: kube-system");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);

        var adminCluster = await SeedAsync("storage-admin-ns-cluster");
        _k8s.SetupCreateClaim("kube-system");
        var admin = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        await admin.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(
            adminCluster, ClaimYamlForged.Replace("namespace: app", "namespace: kube-system")));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedPersistentVolumeClaimWithHttpMessagesAsync(
            It.IsAny<V1PersistentVolumeClaim>(), "kube-system",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Claim_create_member_stamps_and_overrides_forged_label()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateClaim("app");
        var service = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, ClaimYamlForged));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedPersistentVolumeClaimWithHttpMessagesAsync(
            It.Is<V1PersistentVolumeClaim>(claim => claim.Metadata!.Labels![OwnerKey] == "7"
                && claim.Metadata!.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation] == "alice"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(_harness.Db.AuditLogs.Single(a => a.Action == AuditAction.Create).Category == AuditCategory.Storage);
    }

    [Fact]
    public async Task Claim_delete_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadClaim("mine", "app", NewClaim("mine", "app", owner: "7"));
        _k8s.SetupDeleteClaim("mine", "app");
        var owner = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await owner.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "mine", "app"));
        Assert.Equal(AuditAction.Delete, _harness.Db.AuditLogs.Single().Action);

        _k8s.SetupReadClaim("unowned", "app", NewClaim("unowned", "app"));
        await Assert.ThrowsAsync<PermissionException>(
            () => owner.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "unowned", "app")));

        _k8s.SetupReadClaim("unowned", "app", NewClaim("unowned", "app"));
        _k8s.SetupDeleteClaim("unowned", "app");
        var admin = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "unowned", "app"));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task Claim_anonymous_fail_closed_without_audit()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadClaim("mine", "app", NewClaim("mine", "app", owner: "7"));
        var anonymous = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.Anonymous().Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => anonymous.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "mine", "app")));

        Assert.Empty(_harness.Db.AuditLogs);
    }

    [Fact]
    public async Task Claim_list_and_detail_project_can_operate()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListClaims(NewClaim("mine", "app", owner: "7"), NewClaim("unowned", "app"));
        var service = StorageServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        var items = await service.ListClaimsAsync(new StorageClaimQueryRequest(clusterId, null));

        Assert.True(items.Single(item => item.Name == "mine").CanOperate);
        Assert.False(items.Single(item => item.Name == "unowned").CanOperate);

        _k8s.SetupReadClaim("mine", "app", NewClaim("mine", "app", owner: "7"));
        _k8s.SetupListNamespacedPods("app");
        var detail = await service.GetClaimDetailAsync(new StorageClaimKeyRequest(clusterId, "mine", "app"));
        Assert.True(detail!.CanOperate);

        _k8s.SetupReadClaim("unowned", "app", NewClaim("unowned", "app"));
        var unownedDetail = await service.GetClaimDetailAsync(new StorageClaimKeyRequest(clusterId, "unowned", "app"));
        Assert.False(unownedDetail!.CanOperate);
        Assert.NotEmpty(unownedDetail.Yaml);
    }
}
