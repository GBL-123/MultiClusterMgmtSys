using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class StorageServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new("admin", "Admin");

    private readonly Mock<IKubernetes> _k8s = K8sMocks.Create();

    private readonly StorageService _service;

    public StorageServiceTests()
    {
        _service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("admin", 7, "Admin").Object);
    }

    internal static StorageService NewService(
        ServiceHarness harness,
        Mock<IKubernetes> k8s,
        IHttpContextAccessor accessor)
        => new(
            harness.ClusterRepo,
            new ResourceOwnershipGuard(harness.OwnershipRepo, accessor, NullLogger<ResourceOwnershipGuard>.Instance),
            harness.Audit,
            K8sMocks.Cache(k8s),
            NullLogger<StorageService>.Instance);

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedClusterAsync()
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster("storage-cluster"))).Id;

    private static V1PersistentVolumeClaim NewClaim(string name, string ns, string? owner = null, string phase = "Bound")
    {
        var labels = new Dictionary<string, string>();
        if (owner is not null)
        {
            labels[ResourceOwnershipKeys.OwnerUidLabel] = owner;
        }

        return new V1PersistentVolumeClaim
        {
            ApiVersion = "v1",
            Kind = "PersistentVolumeClaim",
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, Labels = labels, Uid = $"uid-{name}" },
            Spec = new V1PersistentVolumeClaimSpec
            {
                StorageClassName = "standard",
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new ResourceQuantity("5Gi") }
                }
            },
            Status = new V1PersistentVolumeClaimStatus { Phase = phase }
        };
    }

    private static V1Pod MountingPod(string name, string claimName)
        => K8sMocks.NewPod(name, "app", customize: pod =>
            pod.Spec!.Volumes = [new V1Volume { Name = "data", PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = claimName } }]);

    private static V1PersistentVolume NewVolume(string name, string? claimNs = null, string? claimName = null)
        => new()
        {
            ApiVersion = "v1",
            Kind = "PersistentVolume",
            Metadata = new V1ObjectMeta { Name = name },
            Spec = new V1PersistentVolumeSpec
            {
                PersistentVolumeReclaimPolicy = "Retain",
                StorageClassName = "slow-storage",
                Capacity = new Dictionary<string, ResourceQuantity> { ["storage"] = new ResourceQuantity("10Gi") },
                ClaimRef = claimName is null ? null : new V1ObjectReference { NamespaceProperty = claimNs ?? "app", Name = claimName },
                HostPath = new V1HostPathVolumeSource { Path = "/data/pv-1" }
            },
            Status = new V1PersistentVolumeStatus { Phase = claimName is null ? "Available" : "Bound" }
        };

    private static V1StorageClass NewClass(string name)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name },
            Provisioner = "kubernetes.io/no-provisioner",
            ReclaimPolicy = "Delete",
            VolumeBindingMode = "Immediate",
            AllowVolumeExpansion = true
        };

    private const string ValidYaml = """
        apiVersion: v1
        kind: PersistentVolumeClaim
        metadata:
          name: app-claim
          namespace: default
        spec:
          accessModes:
          - ReadWriteOnce
          resources:
            requests:
              storage: 5Gi
          storageClassName: standard
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
    public async Task ListClaimsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListClaimsAsync(new StorageClaimQueryRequest(999, null)));
    }

    [Fact]
    public async Task ListClaimsAsync_null_namespace_lists_all()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListClaims(NewClaim("claim-a", "default"));

        var items = await _service.ListClaimsAsync(new StorageClaimQueryRequest(clusterId, null));

        var claim = Assert.Single(items);
        Assert.Equal("claim-a", claim.Name);
        Assert.Equal("Bound", claim.Phase);
        Assert.Equal("已绑定", claim.PhaseText);
        Assert.Equal("5Gi", claim.Capacity);
        Assert.Equal("standard", claim.StorageClass);
        Assert.True(claim.CanOperate);
    }

    [Fact]
    public async Task ListClaimsAsync_namespaced_when_namespace_set()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListNamespacedClaims("app", NewClaim("claim-b", "app"));

        var items = await _service.ListClaimsAsync(new StorageClaimQueryRequest(clusterId, "app"));

        Assert.Single(items);
        Assert.Equal("claim-b", items[0].Name);
    }

    [Fact]
    public async Task ListClaimsAsync_projects_can_operate_for_owned_and_unowned()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListClaims(NewClaim("own", "default", owner: "9"), NewClaim("foreign", "default", owner: "7"));
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        var items = await service.ListClaimsAsync(new StorageClaimQueryRequest(clusterId, null));

        Assert.Equal(2, items.Count);
        Assert.True(items.Single(i => i.Name == "own").CanOperate);
        Assert.False(items.Single(i => i.Name == "foreign").CanOperate);
    }

    [Fact]
    public async Task ListClaimsAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListClaimsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListClaimsAsync(new StorageClaimQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetClaimDetailAsync_missing_cluster_returns_null()
    {
        Assert.Null(await _service.GetClaimDetailAsync(new StorageClaimKeyRequest(999, "claim", "default")));
    }

    [Fact]
    public async Task GetClaimDetailAsync_maps_fields_and_mounted_pods()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("claim-a", "app", NewClaim("claim-a", "app"));
        _k8s.SetupListNamespacedPods("app", MountingPod("web-1", "claim-a"), MountingPod("web-2", "other-claim"));

        var detail = await _service.GetClaimDetailAsync(new StorageClaimKeyRequest(clusterId, "claim-a", "app"));

        Assert.NotNull(detail);
        Assert.Equal("claim-a", detail!.Name);
        Assert.Equal("app", detail.Namespace);
        Assert.Equal("Bound", detail.Phase);
        Assert.Equal("已绑定", detail.PhaseText);
        Assert.Equal("5Gi", detail.Capacity);
        Assert.Equal("standard", detail.StorageClass);
        Assert.Equal(["ReadWriteOnce"], detail.AccessModes);
        Assert.True(detail.CanOperate);
        var mounted = Assert.Single(detail.MountedPods);
        Assert.Equal("web-1", mounted.Name);
        Assert.Contains("kind: PersistentVolumeClaim", detail.Yaml);
    }

    [Fact]
    public async Task GetClaimDetailAsync_pod_query_failure_degrades_to_empty()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("claim-a", "app", NewClaim("claim-a", "app"));
        _k8s.SetupListNamespacedPodsThrows("app", K8sMocks.K8sError(403));

        var detail = await _service.GetClaimDetailAsync(new StorageClaimKeyRequest(clusterId, "claim-a", "app"));

        Assert.NotNull(detail);
        Assert.Empty(detail!.MountedPods);
    }

    [Fact]
    public async Task GetClaimDetailAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaimThrows("missing", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetClaimDetailAsync(new StorageClaimKeyRequest(clusterId, "missing", "app")));
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_invalid_yaml_throws_validation()
    {
        var clusterId = await SeedClusterAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, "{ broken")));

        Assert.Contains("YAML 格式错误", ex.UserMessage);
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_kind_mismatch_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: not-a-claim
              namespace: default
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, yaml)));

        Assert.Contains("kind 必须为 PersistentVolumeClaim", ex.UserMessage);
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_missing_namespace_throws_validation()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: PersistentVolumeClaim
            metadata:
              name: no-ns
            spec:
              accessModes:
              - ReadWriteOnce
              resources:
                requests:
                  storage: 5Gi
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.namespace", ex.UserMessage);
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_member_protected_namespace_rejected()
    {
        var clusterId = await SeedClusterAsync();
        var yaml = """
            apiVersion: v1
            kind: PersistentVolumeClaim
            metadata:
              name: evil
              namespace: kube-system
            spec:
              accessModes:
              - ReadWriteOnce
              resources:
                requests:
                  storage: 5Gi
            """;
        var service = NewService(_harness, _k8s, TestHttpContext.ForIdentity("member", 9, "Member").Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, yaml)));

        Assert.Contains("系统命名空间", ex.UserMessage);
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_stamps_ownership_and_audits()
    {
        var clusterId = await SeedClusterAsync();
        V1PersistentVolumeClaim? created = null;
        _k8s.SetupCreateClaim("default", null, body => created = body);

        await _service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, ValidYaml));

        Assert.NotNull(created);
        Assert.Equal("PersistentVolumeClaim", created!.Kind);
        Assert.Equal("app-claim", created.Metadata.Name);
        Assert.Equal("standard", created.Spec.StorageClassName);
        Assert.Equal("7", created.Metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
        Assert.Equal("admin", created.Metadata.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation]);
        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Storage, audit.Category);
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("app-claim", audit.Target);
    }

    [Fact]
    public async Task CreateClaimFromYamlAsync_conflict_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupCreateClaimThrows("default", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateClaimFromYamlAsync(new StorageClaimCreateRequest(clusterId, ValidYaml)));
    }

    [Fact]
    public async Task DeleteClaimAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteClaimAsync(new StorageClaimKeyRequest(999, "claim", "default")));
    }

    [Fact]
    public async Task DeleteClaimAsync_audits_on_success()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("claim-a", "app", NewClaim("claim-a", "app"));
        _k8s.SetupDeleteClaim("claim-a", "app");

        await _service.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "claim-a", "app"));

        var audit = _harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Storage, audit.Category);
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteClaimAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("claim-a", "app", NewClaim("claim-a", "app"));
        _k8s.SetupDeleteClaimThrows("claim-a", "app", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.DeleteClaimAsync(new StorageClaimKeyRequest(clusterId, "claim-a", "app")));
    }

    [Fact]
    public async Task ListVolumesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListVolumesAsync(new StorageVolumeQueryRequest(999)));
    }

    [Fact]
    public async Task ListVolumesAsync_maps_fields()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListVolumes(NewVolume("pv-1"), NewVolume("pv-2", claimNs: "app", claimName: "data-claim"));

        var items = await _service.ListVolumesAsync(new StorageVolumeQueryRequest(clusterId));

        Assert.Equal(2, items.Count);
        var bound = items.Single(i => i.Name == "pv-2");
        Assert.Equal("Bound", bound.Phase);
        Assert.Equal("已绑定", bound.PhaseText);
        Assert.Equal("10Gi", bound.Capacity);
        Assert.Equal("Retain", bound.ReclaimPolicy);
        Assert.Equal("app/data-claim", bound.BoundClaim);
        Assert.Equal("slow-storage", bound.StorageClassName);
        Assert.True(items.Single(i => i.Name == "pv-1").BoundClaim is null);
    }

    [Fact]
    public async Task ListVolumesAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListVolumesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListVolumesAsync(new StorageVolumeQueryRequest(clusterId)));
    }

    [Fact]
    public async Task GetVolumeDetailAsync_missing_cluster_returns_null()
    {
        Assert.Null(await _service.GetVolumeDetailAsync(new StorageVolumeKeyRequest(999, "pv-1")));
    }

    [Fact]
    public async Task GetVolumeDetailAsync_maps_fields_and_source()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadVolume("pv-1", NewVolume("pv-1"));

        var detail = await _service.GetVolumeDetailAsync(new StorageVolumeKeyRequest(clusterId, "pv-1"));

        Assert.NotNull(detail);
        Assert.Equal("pv-1", detail!.Name);
        Assert.Equal("Available", detail.Phase);
        Assert.Equal("可用", detail.PhaseText);
        Assert.Equal("10Gi", detail.Capacity);
        Assert.Equal("Retain", detail.ReclaimPolicy);
        Assert.Null(detail.BoundClaim);
        Assert.Equal("slow-storage", detail.StorageClassName);
        Assert.Equal("HostPath:/data/pv-1", detail.SourceText);
        Assert.Contains("kind: PersistentVolume", detail.Yaml);
    }

    [Fact]
    public async Task GetVolumeDetailAsync_k8s_404_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadVolumeThrows("missing", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetVolumeDetailAsync(new StorageVolumeKeyRequest(clusterId, "missing")));
    }

    [Fact]
    public async Task ListStorageClassesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ListStorageClassesAsync(new StorageClassQueryRequest(999)));
    }

    [Fact]
    public async Task ListStorageClassesAsync_maps_fields()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListStorageClasses(NewClass("standard"));

        var items = await _service.ListStorageClassesAsync(new StorageClassQueryRequest(clusterId));

        var sc = Assert.Single(items);
        Assert.Equal("standard", sc.Name);
        Assert.Equal("kubernetes.io/no-provisioner", sc.Provisioner);
        Assert.Equal("Delete", sc.ReclaimPolicy);
        Assert.Equal("Immediate", sc.VolumeBindingMode);
        Assert.True(sc.AllowVolumeExpansion);
    }

    [Fact]
    public async Task ListStorageClassesAsync_k8s_error_translated()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupListStorageClassesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.ListStorageClassesAsync(new StorageClassQueryRequest(clusterId)));
    }
}
