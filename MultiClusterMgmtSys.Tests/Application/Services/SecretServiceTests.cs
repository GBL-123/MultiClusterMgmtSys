using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class SecretServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();
    private readonly SecretService _service;

    public SecretServiceTests()
    {
        _service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 7, "Admin").Object);
    }

    internal static SecretService NewService(
        ServiceHarness harness,
        Mock<IKubernetes> k8s,
        IHttpContextAccessor accessor)
        => new(
            harness.ClusterRepo,
            new ResourceOwnershipGuard(harness.OwnershipRepo, accessor, NullLogger<ResourceOwnershipGuard>.Instance),
            harness.Audit,
            K8sMocks.Cache(k8s),
            NullLogger<SecretService>.Instance);

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedClusterAsync()
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster("secret-cluster"))).Id;

    private static V1Secret NewSecret(string name, string ns, string? password = "old-pass", string? legacy = "legacy")
    {
        var data = new Dictionary<string, byte[]>();
        if (password is not null)
        {
            data["password"] = Encoding.UTF8.GetBytes(password);
        }
        if (legacy is not null)
        {
            data["legacy-key"] = Encoding.UTF8.GetBytes(legacy);
        }
        return new V1Secret
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Type = "Opaque",
            Data = data
        };
    }

    private const string ValidYaml = """
        apiVersion: v1
        kind: Secret
        metadata:
          name: app-secret
          namespace: default
        type: Opaque
        stringData:
          password: plain123
        """;

    [Fact]
    public async Task GetNamespacesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetNamespacesAsync(999));
    }

    [Fact]
    public async Task GetNamespacesAsync_returns_sorted_namespace_names()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListNamespaces("default", "kube-system", "app");

        var namespaces = await _service.GetNamespacesAsync(clusterId);

        Assert.Equal(["app", "default", "kube-system"], namespaces);
    }

    [Fact]
    public async Task GetNamespacesAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListNamespacesThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(() => _service.GetNamespacesAsync(clusterId));
    }

    [Fact]
    public async Task ListSecretsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListSecretsAsync(new SecretQueryRequest(999, null)));
    }

    [Fact]
    public async Task ListSecretsAsync_null_namespace_lists_all()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListSecrets(NewSecret("sec-a", "default"));

        var items = await _service.ListSecretsAsync(new SecretQueryRequest(clusterId, null));

        Assert.Single(items);
        Assert.Equal("sec-a", items[0].Name);
        Assert.Equal("Opaque", items[0].Type);
        Assert.Equal(2, items[0].KeyCount);
        Assert.True(items[0].CanOperate);
    }

    [Fact]
    public async Task ListSecretsAsync_namespaced_when_namespace_set()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListNamespacedSecrets("app", NewSecret("sec-b", "app"));

        var items = await _service.ListSecretsAsync(new SecretQueryRequest(clusterId, "app"));

        Assert.Single(items);
        Assert.Equal("sec-b", items[0].Name);
    }

    [Fact]
    public async Task ListSecretsAsync_projects_can_operate_for_owned_and_unowned()
    {
        var clusterId = await SeedClusterAsync();
        var owned = NewSecret("own", "default", password: null, legacy: null);
        owned.Metadata.Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = "9" };
        var foreign = NewSecret("foreign", "default", password: null, legacy: null);
        foreign.Metadata.Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = "7" };
        _k8s.SetupListSecrets(owned, foreign);
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        var items = await service.ListSecretsAsync(new SecretQueryRequest(clusterId, null));

        Assert.Equal(2, items.Count);
        Assert.True(items.Single(i => i.Name == "own").CanOperate);
        Assert.False(items.Single(i => i.Name == "foreign").CanOperate);
    }

    [Fact]
    public async Task ListSecretsAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListSecretsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListSecretsAsync(new SecretQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetSecretDetailAsync_missing_cluster_returns_null()
    {
        Assert.Null(await _service.GetSecretDetailAsync(new SecretKeyRequest(999, "sec", "default")));
    }

    [Fact]
    public async Task GetSecretDetailAsync_maps_masked_entries_and_base64_yaml()
    {
        var clusterId = await SeedClusterAsync();
        var secret = NewSecret("sec-a", "default");
        secret.Data["blob"] = [0xFF, 0xFE, 0x00, 0xC3, 0x28];
        _k8s.SetupReadSecret("sec-a", "default", secret);

        var detail = await _service.GetSecretDetailAsync(new SecretKeyRequest(clusterId, "sec-a", "default"));

        Assert.NotNull(detail);
        Assert.Equal("sec-a", detail!.Name);
        Assert.Equal("Opaque", detail.Type);
        Assert.True(detail.CanOperate);
        var password = detail.Entries.Single(e => e.Key == "password");
        Assert.True(password.IsText);
        Assert.Equal(8, password.ByteCount);
        Assert.Equal("", password.Base64);
        Assert.Null(password.Value);
        var blob = detail.Entries.Single(e => e.Key == "blob");
        Assert.False(blob.IsText);
        Assert.Equal(Convert.ToBase64String([0xFF, 0xFE, 0x00, 0xC3, 0x28]), blob.Base64);
        Assert.Contains("b2xkLXBhc3M=", detail.Yaml);
        Assert.DoesNotContain("old-pass", detail.Yaml);
    }

    [Fact]
    public async Task GetSecretDetailAsync_restricted_for_non_owner()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        var detail = await service.GetSecretDetailAsync(new SecretKeyRequest(clusterId, "sec-a", "default"));

        Assert.NotNull(detail);
        Assert.False(detail!.CanOperate);
        Assert.Empty(detail.Entries);
        Assert.Equal("", detail.Yaml);
    }

    [Fact]
    public async Task GetSecretDetailAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecretThrows("missing", "default", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetSecretDetailAsync(new SecretKeyRequest(clusterId, "missing", "default")));
    }

    [Fact]
    public async Task RevealSecretKeyAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RevealSecretKeyAsync(new SecretRevealRequest(999, "sec", "default", "password")));
    }

    [Fact]
    public async Task RevealSecretKeyAsync_missing_key_throws_not_found()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "sec-a", "default", "nope")));

        Assert.Contains("nope", ex.UserMessage);
    }

    [Fact]
    public async Task RevealSecretKeyAsync_binary_content_rejected()
    {
        var clusterId = await SeedClusterAsync();
        var secret = NewSecret("sec-a", "default");
        secret.Data["blob"] = [0xFF, 0xFE, 0x00, 0xC3, 0x28];
        _k8s.SetupReadSecret("sec-a", "default", secret);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "sec-a", "default", "blob")));

        Assert.Contains("不是文本内容", ex.UserMessage);
    }

    [Fact]
    public async Task RevealSecretKeyAsync_returns_plaintext_and_audits()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));

        var plaintext = await _service.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "sec-a", "default", "password"));

        Assert.Equal("old-pass", plaintext);
        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Secret, audit.Category);
        Assert.Equal(AuditAction.View, audit.Action);
        Assert.Contains("password", audit.Target);
        Assert.DoesNotContain("old-pass", audit.Target);
    }

    [Fact]
    public async Task RevealSecretKeyAsync_denied_for_non_owner()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.RevealSecretKeyAsync(new SecretRevealRequest(clusterId, "sec-a", "default", "password")));
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedClusterAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, "{ broken")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_kind_mismatch_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: not-a-secret
              namespace: default
            data:
              k: v
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("kind 必须为 Secret", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_missing_namespace_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: no-ns
            stringData:
              k: v
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.namespace", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_placeholder_rejected()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: app-secret
              namespace: default
            data:
              password: "<REDACTED:password>"
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("不能在创建时使用占位符", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_invalid_base64_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: app-secret
              namespace: default
            data:
              password: "!!!not-base64!!!"
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("base64", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_encodes_string_data_and_stamps_ownership()
    {
        var clusterId = await SeedClusterAsync();
        V1Secret? created = null;
        _k8s.SetupCreateSecret("default", null, body => created = body);

        await _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, ValidYaml));

        Assert.NotNull(created);
        Assert.Equal("app-secret", created!.Metadata.Name);
        Assert.Equal("plain123", Encoding.UTF8.GetString(created.Data["password"]));
        Assert.Null(created.StringData);
        Assert.Equal("Opaque", created.Type);
        Assert.Equal("7", created.Metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
        Assert.Equal("admin", created.Metadata.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation]);
        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("app-secret", audit.Target);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_member_protected_namespace_rejected()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: evil
              namespace: kube-system
            stringData:
              k: v
            """;
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);
    }

    [Fact]
    public async Task CreateSecretFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupCreateSecretThrows("default", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateSecretFromYamlAsync(new SecretCreateRequest(clusterId, ValidYaml)));
    }

    [Fact]
    public async Task GetSecretForEditAsync_returns_placeholder_yaml()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));

        var edit = await _service.GetSecretForEditAsync(new SecretKeyRequest(clusterId, "sec-a", "default"));

        Assert.Equal("sec-a", edit.Name);
        Assert.Equal("default", edit.Namespace);
        Assert.Equal("Opaque", edit.Type);
        Assert.True(edit.CanOperate);
        Assert.Contains("<REDACTED:password>", edit.Yaml);
        Assert.Contains("<REDACTED:legacy-key>", edit.Yaml);
        Assert.DoesNotContain("old-pass", edit.Yaml);
    }

    [Fact]
    public async Task GetSecretForEditAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetSecretForEditAsync(new SecretKeyRequest(999, "sec", "default")));
    }

    [Fact]
    public async Task GetSecretForEditAsync_denied_for_non_owner()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.GetSecretForEditAsync(new SecretKeyRequest(clusterId, "sec-a", "default")));
    }

    [Fact]
    public async Task UpdateSecretFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var request = new SecretUpdateRequest(clusterId, "sec-a", "default", "{ not: [ valid yaml");

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateSecretFromYamlAsync(request));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateSecretFromYamlAsync_merges_keep_override_delete_and_audits()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));
        V1Secret? replaced = null;
        _k8s.SetupReplaceSecret("sec-a", "default", body => replaced = body);
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: sec-a
              namespace: default
            type: Opaque
            data:
              password: "<REDACTED:password>"
              extra: "bmV3LXZhbHVl"
            """;

        await _service.UpdateSecretFromYamlAsync(new SecretUpdateRequest(clusterId, "sec-a", "default", yaml));

        Assert.NotNull(replaced);
        Assert.Equal("old-pass", Encoding.UTF8.GetString(replaced!.Data["password"]));
        Assert.Equal("new-value", Encoding.UTF8.GetString(replaced.Data["extra"]));
        Assert.DoesNotContain("legacy-key", replaced.Data.Keys);
        Assert.Null(replaced.StringData);
        Assert.Equal("Opaque", replaced.Type);
        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateSecretFromYamlAsync_keeps_server_type_when_yaml_type_blank()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default"));
        V1Secret? replaced = null;
        _k8s.SetupReplaceSecret("sec-a", "default", body => replaced = body);
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: sec-a
              namespace: default
            data:
              password: "<REDACTED:password>"
            """;

        await _service.UpdateSecretFromYamlAsync(new SecretUpdateRequest(clusterId, "sec-a", "default", yaml));

        Assert.NotNull(replaced);
        Assert.Equal("Opaque", replaced!.Type);
    }

    [Fact]
    public async Task UpdateSecretFromYamlAsync_missing_placeholder_value_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec-a", "default", NewSecret("sec-a", "default", password: "old-pass", legacy: null));
        var yaml = """
            apiVersion: v1
            kind: Secret
            metadata:
              name: sec-a
              namespace: default
            data:
              gone: "<REDACTED:gone>"
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateSecretFromYamlAsync(new SecretUpdateRequest(clusterId, "sec-a", "default", yaml)));

        Assert.Contains("占位符已失效", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateSecretFromYamlAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecretThrows("sec-a", "default", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateSecretFromYamlAsync(new SecretUpdateRequest(clusterId, "sec-a", "default", ValidYaml)));
    }

    [Fact]
    public async Task DeleteSecretAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteSecretAsync(new SecretKeyRequest(999, "sec", "default")));
    }

    [Fact]
    public async Task DeleteSecretAsync_audits_on_success()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec", "default", NewSecret("sec", "default"));
        _k8s.SetupDeleteSecret("sec", "default");

        await _service.DeleteSecretAsync(new SecretKeyRequest(clusterId, "sec", "default"));

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Secret, audit.Category);
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteSecretAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadSecret("sec", "default", NewSecret("sec", "default"));
        _k8s.SetupDeleteSecretThrows("sec", "default", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteSecretAsync(new SecretKeyRequest(clusterId, "sec", "default")));
    }
}
