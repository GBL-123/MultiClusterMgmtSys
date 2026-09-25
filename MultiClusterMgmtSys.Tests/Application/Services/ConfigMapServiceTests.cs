using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class ConfigMapServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();
    private readonly ConfigMapService _service;

    public ConfigMapServiceTests()
    {
        _service = new ConfigMapService(
            _harness.ClusterRepo, _harness.Audit, NullLogger<ConfigMapService>.Instance, K8sMocks.Cache(_k8s));
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedClusterAsync()
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-cluster"))).Id;

    private static V1ConfigMap NewConfigMap(string name, string ns, string? value = null)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Data = new Dictionary<string, string> { ["key1"] = value ?? "value1" }
        };

    private const string ValidYaml = """
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: test-cm
          namespace: default
        data:
          key1: value1
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
    public async Task ListConfigMapsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListConfigMapsAsync(new ConfigMapQueryRequest(999, null)));
    }

    [Fact]
    public async Task ListConfigMapsAsync_null_namespace_lists_all()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListConfigMaps(NewConfigMap("cm-a", "default"));

        var items = await _service.ListConfigMapsAsync(new ConfigMapQueryRequest(clusterId, null));

        Assert.Single(items);
        Assert.Equal("cm-a", items[0].Name);
    }

    [Fact]
    public async Task ListConfigMapsAsync_namespaced_when_namespace_set()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListNamespacedConfigMaps("app", NewConfigMap("cm-b", "app"));

        var items = await _service.ListConfigMapsAsync(new ConfigMapQueryRequest(clusterId, "app"));

        Assert.Single(items);
        Assert.Equal("cm-b", items[0].Name);
    }

    [Fact]
    public async Task ListConfigMapsAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListConfigMapsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListConfigMapsAsync(new ConfigMapQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetConfigMapAsync_missing_cluster_returns_null()
    {
        Assert.Null(await _service.GetConfigMapAsync(new ConfigMapKeyRequest(999, "cm", "default")));
    }

    [Fact]
    public async Task GetConfigMapAsync_maps_detail_view()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMap("cm-a", "default", NewConfigMap("cm-a", "default", "hello"));

        var detail = await _service.GetConfigMapAsync(new ConfigMapKeyRequest(clusterId, "cm-a", "default"));

        Assert.NotNull(detail);
        Assert.Equal("cm-a", detail!.Name);
        Assert.Equal("hello", detail.Data["key1"]);
    }

    [Fact]
    public async Task GetConfigMapAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMapThrows("missing", "default", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetConfigMapAsync(new ConfigMapKeyRequest(clusterId, "missing", "default")));
    }

    [Fact]
    public async Task DeleteConfigMapAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteConfigMapAsync(new ConfigMapKeyRequest(999, "cm", "default")));
    }

    [Fact]
    public async Task DeleteConfigMapAsync_audits_on_success()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupDeleteConfigMap("cm", "default");

        await _service.DeleteConfigMapAsync(new ConfigMapKeyRequest(clusterId, "cm", "default"));

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Configmap, audit.Category);
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteConfigMapAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupDeleteConfigMapThrows("cm", "default", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteConfigMapAsync(new ConfigMapKeyRequest(clusterId, "cm", "default")));
    }

    [Fact]
    public async Task UpdateConfigMapFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var request = new ConfigMapUpdateRequest(clusterId, "default", "cm", "{ not: [ valid yaml");

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateConfigMapFromYamlAsync(request));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateConfigMapFromYamlAsync_replaces_data_and_audits()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMap("cm-a", "default", NewConfigMap("cm-a", "default"));
        _k8s.SetupReplaceConfigMap("cm-a", "default");

        var yaml = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: cm-a
              namespace: default
            data:
              key1: replaced
            """;
        await _service.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm-a", "default", yaml));

        _k8s.Verify(x => x.CoreV1.ReplaceNamespacedConfigMapWithHttpMessagesAsync(
            It.Is<V1ConfigMap>(cm => cm.Data["key1"] == "replaced"),
            "cm-a", "default",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Update, audit.Action);
    }

    [Fact]
    public async Task UpdateConfigMapFromYamlAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMapThrows("cm", "default", K8sMocks.K8sError(404));
        var yaml = ValidYaml.Replace("test-cm", "cm");

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(clusterId, "cm", "default", yaml)));
    }

    [Fact]
    public async Task CreateConfigMapFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedClusterAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, "{ broken")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task CreateConfigMapFromYamlAsync_missing_namespace_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: no-ns
            data:
              k: v
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.namespace", ex.UserMessage);
    }

    [Fact]
    public async Task CreateConfigMapFromYamlAsync_success_audits()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupCreateConfigMap("default");

        await _service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, ValidYaml));

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("test-cm", audit.Target);
    }

    [Fact]
    public async Task CreateConfigMapFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupCreateConfigMapThrows("default", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(clusterId, ValidYaml)));
    }
}


