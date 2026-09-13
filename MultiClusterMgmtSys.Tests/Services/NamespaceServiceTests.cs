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

public class NamespaceServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly NamespaceService service;

    public NamespaceServiceTests()
    {
        service = new NamespaceService(
            harness.ClusterRepo, harness.Audit, NullLogger<NamespaceService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    private async Task<int> SeedClusterAsync()
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-cluster"))).Id;

    private static V1Namespace NewNamespace(
        string name,
        string phase = "Active",
        Dictionary<string, string>? labels = null,
        Dictionary<string, string>? annotations = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                Labels = labels,
                Annotations = annotations,
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Status = new V1NamespaceStatus { Phase = phase }
        };

    private const string ValidYaml = """
        apiVersion: v1
        kind: Namespace
        metadata:
          name: dev-team
        """;

    [Fact]
    public async Task ListNamespacesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListNamespacesAsync(999));
    }

    [Fact]
    public async Task ListNamespacesAsync_maps_status_and_labels()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupListNamespaceObjects(
            NewNamespace("default", "Active", labels: new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" }),
            NewNamespace("old-dev", "Terminating"));

        var items = await service.ListNamespacesAsync(clusterId);

        Assert.Equal(2, items.Count);
        Assert.Equal("default", items[0].Name);
        Assert.Equal("Active", items[0].Phase);
        Assert.Equal("在线", items[0].StatusText);
        Assert.Equal("online", items[0].StatusCssClass);
        Assert.Equal(2, items[0].LabelCount);
        Assert.Equal("Terminating", items[1].Phase);
        Assert.Equal("未知", items[1].StatusText);
        Assert.Equal("unknown", items[1].StatusCssClass);
    }

    [Fact]
    public async Task ListNamespacesAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupListNamespacesThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(() => service.ListNamespacesAsync(clusterId));
    }

    [Fact]
    public async Task GetNamespaceAsync_missing_cluster_returns_null()
    {
        Assert.Null(await service.GetNamespaceAsync(new NamespaceKeyRequest(999, "dev")));
    }

    [Fact]
    public async Task GetNamespaceAsync_maps_detail_view()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupReadNamespace("dev", NewNamespace(
            "dev",
            labels: new Dictionary<string, string> { ["team"] = "platform" },
            annotations: new Dictionary<string, string> { ["owner"] = "ops" }));

        var detail = await service.GetNamespaceAsync(new NamespaceKeyRequest(clusterId, "dev"));

        Assert.NotNull(detail);
        Assert.Equal("dev", detail!.Name);
        Assert.Equal("在线", detail.StatusText);
        Assert.Equal("platform", detail.Labels["team"]);
        Assert.Equal("ops", detail.Annotations["owner"]);
        Assert.Contains("name: dev", detail.Yaml);
        Assert.Contains("owner: ops", detail.Yaml);
    }

    [Fact]
    public async Task GetNamespaceAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupReadNamespaceThrows("missing", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetNamespaceAsync(new NamespaceKeyRequest(clusterId, "missing")));
    }

    [Fact]
    public async Task CreateNamespaceFromYamlAsync_invalid_yaml_throws_validation_without_k8s_call()
    {
        var clusterId = await SeedClusterAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateNamespaceFromYamlAsync(new NamespaceCreateRequest(clusterId, "{ broken")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
        VerifyCreateNamespaceNever();
    }

    [Fact]
    public async Task CreateNamespaceFromYamlAsync_missing_name_throws_validation_without_k8s_call()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: Namespace
            metadata:
              labels:
                team: platform
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateNamespaceFromYamlAsync(new NamespaceCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.name", ex.UserMessage);
        VerifyCreateNamespaceNever();
    }

    [Fact]
    public async Task CreateNamespaceFromYamlAsync_success_audits()
    {
        var clusterId = await SeedClusterAsync();
        V1Namespace? created = null;
        k8s.SetupCreateNamespace(onCreated: ns => created = ns);

        await service.CreateNamespaceFromYamlAsync(new NamespaceCreateRequest(clusterId, ValidYaml));

        Assert.Equal("dev-team", created?.Metadata?.Name);
        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Namespace, audit.Category);
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("dev-team", audit.Target);
        Assert.Contains("ns-cluster", audit.Target);
    }

    [Fact]
    public async Task CreateNamespaceFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupCreateNamespaceThrows(K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateNamespaceFromYamlAsync(new NamespaceCreateRequest(clusterId, ValidYaml)));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("kube-system")]
    [InlineData("kube-public")]
    [InlineData("kube-custom")]
    public async Task DeleteNamespaceAsync_protected_names_rejected_without_k8s_call(string name)
    {
        var clusterId = await SeedClusterAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.DeleteNamespaceAsync(new NamespaceKeyRequest(clusterId, name)));

        Assert.Contains("系统命名空间", ex.UserMessage);
        k8s.Verify(x => x.CoreV1.DeleteNamespaceWithHttpMessagesAsync(
            It.IsAny<string>(),
            It.IsAny<V1DeleteOptions?>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<bool?>(),
            It.IsAny<bool?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteNamespaceAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteNamespaceAsync(new NamespaceKeyRequest(999, "dev")));
    }

    [Fact]
    public async Task DeleteNamespaceAsync_success_audits()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupDeleteNamespace("dev");

        await service.DeleteNamespaceAsync(new NamespaceKeyRequest(clusterId, "dev"));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Namespace, audit.Category);
        Assert.Equal(AuditAction.Delete, audit.Action);
        Assert.Contains("dev", audit.Target);
        Assert.Contains("ns-cluster", audit.Target);
    }

    [Fact]
    public async Task DeleteNamespaceAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        k8s.SetupDeleteNamespaceThrows("dev", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeleteNamespaceAsync(new NamespaceKeyRequest(clusterId, "dev")));
    }

    private void VerifyCreateNamespaceNever()
        => k8s.Verify(x => x.CoreV1.CreateNamespaceWithHttpMessagesAsync(
            It.IsAny<V1Namespace>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
}
