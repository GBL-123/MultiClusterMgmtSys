using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class ClusterCompareServiceTests : IDisposable
{
    private static readonly string AlphaHost = "https://alpha:6443";

    private static readonly string BetaHost = "https://beta:6443";

    private static readonly string NamespaceName = "app";

    private static readonly string ResourceName = "web";

    private readonly ServiceHarness _harness = new("admin", "Admin");

    private readonly Mock<IHttpContextAccessor> _accessor = TestHttpContext.ForIdentity("admin", 7, "Admin");

    private readonly Mock<IKubernetes> _alpha = K8sMocks.Create();

    private readonly Mock<IKubernetes> _beta = K8sMocks.Create();

    private ClusterCompareService? _service;

    public void Dispose() => _harness.Dispose();

    private ClusterCompareService NewService(Mock<IHttpContextAccessor>? accessorOverride = null)
    {
        var accessor = accessorOverride ?? _accessor;
        var cache = new ClusterClientCache(
            conf => conf.Host == AlphaHost ? _alpha.Object : _beta.Object,
            NullLogger<ClusterClientCache>.Instance);
        var guard = new ResourceOwnershipGuard(_harness.OwnershipRepo, accessor.Object, NullLogger<ResourceOwnershipGuard>.Instance);
        var workloadService = new WorkloadService(
            _harness.ClusterRepo, guard, _harness.Audit, cache, NullLogger<WorkloadService>.Instance);
        var configMapService = new ConfigMapService(
            _harness.ClusterRepo, guard, _harness.Audit, cache, NullLogger<ConfigMapService>.Instance);
        _service = new ClusterCompareService(
            _harness.ClusterRepo, workloadService, configMapService, NullLogger<ClusterCompareService>.Instance);
        return _service;
    }

    private async Task<(int SourceId, int TargetId)> SeedTwoClustersAsync()
    {
        var source = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));
        var target = await _harness.ClusterRepo.AddAsync(TestData.NewCluster("beta"));
        return (source.Id, target.Id);
    }

    private static V1ConfigMap ConfigMap(string color) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = ResourceName,
            NamespaceProperty = NamespaceName,
            Uid = "uid-" + color,
            ResourceVersion = "123",
            CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ManagedFields = [new V1ManagedFieldsEntry { Manager = "kubectl" }],
            Labels = new Dictionary<string, string>
            {
                [ResourceOwnershipKeys.OwnerUidLabel] = "7",
                ["app"] = "web"
            },
            Annotations = new Dictionary<string, string>
            {
                [ResourceOwnershipKeys.OwnerNameAnnotation] = "someone"
            }
        },
        Data = new Dictionary<string, string> { ["color"] = color }
    };

    private static V1Deployment DeploymentWithServerFields()
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = ResourceName,
                NamespaceProperty = NamespaceName,
                Uid = "uid-dep",
                ResourceVersion = "456",
                CreationTimestamp = DateTime.UtcNow,
                ManagedFields = [new V1ManagedFieldsEntry { Manager = "kubectl" }]
            },
            Status = new V1DeploymentStatus { Replicas = 3 }
        };

    [Fact]
    public async Task GetPair_both_exist_with_difference_flags_clone()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        _beta.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("red"));

        var pair = await NewService().GetPairAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName));

        Assert.True(pair.SourceExists);
        Assert.True(pair.TargetExists);
        Assert.True(pair.HasDifference);
        Assert.True(pair.CanClone);
        Assert.Contains("blue", pair.SourceYaml, StringComparison.Ordinal);
        Assert.Contains("red", pair.TargetYaml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPair_identical_content_is_not_clonable()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        _beta.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));

        var pair = await NewService().GetPairAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName));

        Assert.False(pair.HasDifference);
        Assert.False(pair.CanClone);
    }

    [Fact]
    public async Task GetPair_target_missing_reports_missing_and_clonable()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        _beta.SetupReadConfigMapThrows(ResourceName, NamespaceName, K8sMocks.K8sError(404));

        var pair = await NewService().GetPairAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName));

        Assert.True(pair.SourceExists);
        Assert.False(pair.TargetExists);
        Assert.Equal("", pair.TargetYaml);
        Assert.False(pair.HasDifference);
        Assert.True(pair.CanClone);
    }

    [Fact]
    public async Task GetPair_same_cluster_is_rejected()
    {
        await SeedTwoClustersAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => NewService().GetPairAsync(new(1, 1, CompareKind.ConfigMap, NamespaceName, ResourceName)));

        Assert.Contains("不同的集群", ex.UserMessage);
    }

    [Fact]
    public async Task GetPair_unknown_cluster_throws_not_found()
    {
        var (sourceId, _) = await SeedTwoClustersAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().GetPairAsync(new(sourceId, 999, CompareKind.ConfigMap, NamespaceName, ResourceName)));
    }

    [Fact]
    public async Task GetSourceResourceNames_configmaps_are_sorted()
    {
        var (sourceId, _) = await SeedTwoClustersAsync();
        _alpha.SetupListNamespacedConfigMaps(
            NamespaceName,
            new V1ConfigMap { Metadata = new V1ObjectMeta { Name = "zeta" } },
            new V1ConfigMap { Metadata = new V1ObjectMeta { Name = "app-config" } });

        var names = await NewService().GetSourceResourceNamesAsync(sourceId, CompareKind.ConfigMap, NamespaceName);

        Assert.Equal(["app-config", "zeta"], names);
    }

    [Fact]
    public async Task GetSourceResourceNames_deployments_reads_lists()
    {
        var (sourceId, _) = await SeedTwoClustersAsync();
        _alpha.SetupListNamespacedDeployments(NamespaceName,
            new V1Deployment { Metadata = new V1ObjectMeta { Name = "web" } },
            new V1Deployment { Metadata = new V1ObjectMeta { Name = "api" } });

        var names = await NewService().GetSourceResourceNamesAsync(sourceId, CompareKind.Deployment, NamespaceName);

        Assert.Equal(["api", "web"], names);
    }

    [Fact]
    public async Task CloneConfigMap_strips_server_metadata_and_stamps_ownership()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        V1ConfigMap? created = null;
        _beta.Setup(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Callback<V1ConfigMap, string, string?, string?, string?, bool?,
                IReadOnlyDictionary<string, IReadOnlyList<string>>, System.Threading.CancellationToken>(
                (b, _, _, _, _, _, _, _) => created = b)
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap>());

        await NewService().CloneAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName));

        var body = Assert.IsType<V1ConfigMap>(created);
        Assert.Null(body.Metadata!.Uid);
        Assert.Null(body.Metadata.ResourceVersion);
        Assert.Null(body.Metadata.ManagedFields);
        Assert.Equal(NamespaceName, body.Metadata.NamespaceProperty);
        Assert.Equal(ResourceName, body.Metadata.Name);
        Assert.Equal("7", body.Metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
        Assert.Equal("web", body.Metadata.Labels["app"]);
        Assert.Equal("admin", body.Metadata.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation]);
        Assert.Equal("blue", body.Data["color"]);
    }

    [Fact]
    public async Task CloneDeployment_strips_status_and_server_fields()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadDeployment(ResourceName, NamespaceName, DeploymentWithServerFields());
        V1Deployment? created = null;
        _beta.Setup(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<System.Threading.CancellationToken>()))
            .Callback<V1Deployment, string, string?, string?, string?, bool?,
                IReadOnlyDictionary<string, IReadOnlyList<string>>, System.Threading.CancellationToken>(
                (b, _, _, _, _, _, _, _) => created = b)
            .ReturnsAsync(new HttpOperationResponse<V1Deployment>());

        await NewService().CloneAsync(new(sourceId, targetId, CompareKind.Deployment, NamespaceName, ResourceName));

        var body = Assert.IsType<V1Deployment>(created);
        Assert.Null(body.Metadata!.Uid);
        Assert.Null(body.Metadata.ResourceVersion);
        Assert.Null(body.Metadata.ManagedFields);
        Assert.Null(body.Status);
    }

    [Fact]
    public async Task Clone_missing_source_throws_not_found()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMapThrows(ResourceName, NamespaceName, K8sMocks.K8sError(404));

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => NewService().CloneAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName)));

        Assert.Contains("源资源", ex.UserMessage);
    }

    [Fact]
    public async Task Clone_existing_target_conflict_surfaces_chinese_conflict()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        _beta.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("red"));
        _beta.SetupCreateConfigMapThrows(NamespaceName, K8sMocks.K8sError(409));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => NewService().CloneAsync(new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName)));

        Assert.Equal("资源已被他人修改，请刷新后重试", ex.UserMessage);
    }

    [Fact]
    public async Task GetPair_member_without_ownership_sees_pair_but_cannot_clone()
    {
        var (sourceId, targetId) = await SeedTwoClustersAsync();
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("blue"));
        _beta.SetupReadConfigMap(ResourceName, NamespaceName, ConfigMap("red"));
        var member = TestHttpContext.ForIdentity("member", 9, "Member");

        var pair = await NewService(member).GetPairAsync(
            new(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName));

        Assert.True(pair.SourceExists);
        Assert.True(pair.HasDifference);
        Assert.False(pair.CanClone);
    }
}
