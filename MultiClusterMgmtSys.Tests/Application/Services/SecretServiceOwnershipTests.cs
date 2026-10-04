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

public class SecretServiceOwnershipTests : IDisposable
{
    private const string OwnerKey = ResourceOwnershipKeys.OwnerUidLabel;

    private const string SecretYamlForged = """
        apiVersion: v1
        kind: Secret
        metadata:
          name: sec-forged
          namespace: app
          labels:
            mcms.ms/owner-uid: "99"
          annotations:
            mcms.ms/owner-name: "mallory"
        type: Opaque
        stringData:
          token: plain123
        """;

    private readonly ServiceHarness _harness;

    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();

    public SecretServiceOwnershipTests()
    {
        _harness = new ServiceHarness("alice");
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedAsync(string name = "secret-matrix-cluster")
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static V1Secret NewSecret(string name, string ns, string? owner = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null ? new Dictionary<string, string>() : new Dictionary<string, string> { [OwnerKey] = owner }
            },
            Type = "Opaque",
            Data = new Dictionary<string, byte[]> { ["token"] = Encoding.UTF8.GetBytes("value1") }
        };

    private static string ValidSecretYaml(string name, string ns, string? ownerUid = null)
    {
        var lines = new List<string>
        {
            "apiVersion: v1",
            "kind: Secret",
            "metadata:",
            $"  name: {name}",
            $"  namespace: {ns}"
        };
        if (ownerUid is not null)
        {
            lines.Add("  labels:");
            lines.Add($"    {ResourceOwnershipKeys.OwnerUidLabel}: \"{ownerUid}\"");
        }
        lines.Add("type: Opaque");
        lines.Add("stringData:");
        lines.Add("  token: plain123");
        return string.Join('\n', lines);
    }

    [Fact]
    public async Task Secret_create_member_blacklist_rejected_admin_allowed()
    {
        var clusterId = await SeedAsync();
        var service = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        var yaml = SecretYamlForged.Replace("namespace: app", "namespace: kube-system");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);

        var adminCluster = await SeedAsync("secret-admin-ns-cluster");
        _k8s.SetupCreateSecret("kube-system");
        var admin = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        await admin.CreateSecretFromYamlAsync(new SecretCreateRequest(
            adminCluster, SecretYamlForged.Replace("namespace: app", "namespace: kube-system")));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedSecretWithHttpMessagesAsync(
            It.IsAny<V1Secret>(), "kube-system",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Secret_create_member_stamps_and_overrides_forge()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupCreateSecret("app");
        var service = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, SecretYamlForged));

        _k8s.Verify(x => x.CoreV1.CreateNamespacedSecretWithHttpMessagesAsync(
            It.Is<V1Secret>(s => s.Metadata!.Labels![OwnerKey] == "7"
                && s.Metadata!.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation] == "alice"
                && Encoding.UTF8.GetString(s.Data!["token"]) == "plain123"),
            "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Secret_update_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        var yaml = ValidSecretYaml("sec-a", "app");
        _k8s.SetupReadSecret("sec-a", "app", NewSecret("sec-a", "app", owner: "7"));
        _k8s.SetupReplaceSecret("sec-a", "app");
        var ownerService = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await ownerService.UpdateSecretFromYamlAsync(new SecretUpdateRequest(clusterId, "sec-a", "app", yaml));
        Assert.Equal(AuditAction.Update, _harness.Db.AuditLogs.Single().Action);

        var unownedCluster = await SeedAsync("secret-unowned-cluster");
        _k8s.SetupReadSecret("other", "app", NewSecret("other", "app"));
        var member = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("bob", 9).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => member.UpdateSecretFromYamlAsync(new SecretUpdateRequest(unownedCluster, "other", "app", ValidSecretYaml("other", "app"))));

        _k8s.SetupReadSecret("other", "app", NewSecret("other", "app"));
        _k8s.SetupReplaceSecret("other", "app");
        var admin = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.UpdateSecretFromYamlAsync(new SecretUpdateRequest(unownedCluster, "other", "app", ValidSecretYaml("other", "app")));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task Secret_delete_ownership_matrix()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadSecret("mine", "app", NewSecret("mine", "app", owner: "7"));
        _k8s.SetupDeleteSecret("mine", "app");
        var member = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);
        await member.DeleteSecretAsync(new SecretKeyRequest(clusterId, "mine", "app"));
        Assert.Equal(AuditAction.Delete, _harness.Db.AuditLogs.Single().Action);

        _k8s.SetupReadSecret("unowned", "app", NewSecret("unowned", "app"));
        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => member.DeleteSecretAsync(new SecretKeyRequest(clusterId, "unowned", "app")));

        _k8s.SetupReadSecret("unowned", "app", NewSecret("unowned", "app"));
        _k8s.SetupDeleteSecret("unowned", "app");
        var admin = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);
        await admin.DeleteSecretAsync(new SecretKeyRequest(clusterId, "unowned", "app"));

        Assert.Equal(2, _harness.Db.AuditLogs.Count());
    }

    [Fact]
    public async Task Secret_reveal_unowned_denied_without_audit_admin_allowed()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadSecret("stranger", "app", NewSecret("stranger", "app"));
        var member = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => member.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "stranger", "app", "token")));

        Assert.Contains("仅可操作自己创建", ex.UserMessage);
        Assert.Empty(_harness.Db.AuditLogs.Where(a => a.Action == AuditAction.View));

        _k8s.SetupReadSecret("stranger", "app", NewSecret("stranger", "app"));
        var admin = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 1, "Admin").Object);

        var plaintext = await admin.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "stranger", "app", "token"));

        Assert.Equal("value1", plaintext);
        var audit = _harness.Db.AuditLogs.Single(a => a.Action == AuditAction.View);
        Assert.Equal(AuditCategory.Secret, audit.Category);
        Assert.Contains("token", audit.Target);
        Assert.DoesNotContain("value1", audit.Target);
    }

    [Fact]
    public async Task Secret_anonymous_fail_closed_without_audit()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupReadSecret("mine", "app", NewSecret("mine", "app", owner: "7"));
        var anonymous = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.Anonymous().Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => anonymous.DeleteSecretAsync(new SecretKeyRequest(clusterId, "mine", "app")));

        Assert.Empty(_harness.Db.AuditLogs);
    }

    [Fact]
    public async Task Secret_list_and_detail_project_can_operate()
    {
        var clusterId = await SeedAsync();
        _k8s.SetupListSecrets(NewSecret("mine", "app", owner: "7"), NewSecret("unowned", "app"));
        var service = SecretServiceTests.NewService(_harness, _k8s, TestHttpContext.ForIdentity("alice", 7).Object);

        var items = await service.ListSecretsAsync(new SecretQueryRequest(clusterId, null));

        Assert.True(items.Single(item => item.Name == "mine").CanOperate);
        Assert.False(items.Single(item => item.Name == "unowned").CanOperate);

        _k8s.SetupReadSecret("mine", "app", NewSecret("mine", "app", owner: "7"));
        var detail = await service.GetSecretDetailAsync(new SecretKeyRequest(clusterId, "mine", "app"));
        Assert.True(detail!.CanOperate);
        Assert.NotEmpty(detail.Entries);

        _k8s.SetupReadSecret("unowned", "app", NewSecret("unowned", "app"));
        var unownedDetail = await service.GetSecretDetailAsync(new SecretKeyRequest(clusterId, "unowned", "app"));
        Assert.False(unownedDetail!.CanOperate);
        Assert.Empty(unownedDetail.Entries);
        Assert.Equal(string.Empty, unownedDetail.Yaml);
    }
}
