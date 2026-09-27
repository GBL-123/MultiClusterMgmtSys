using System.Globalization;
using k8s.Models;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class ResourceOwnershipPolicyTests
{
    [Fact]
    public void Stamp_writes_uid_label_and_name_annotation()
    {
        var metadata = new V1ObjectMeta { Name = "web" };

        ResourceOwnershipStamp.Stamp(metadata, 7, "alice");

        Assert.Equal("7", metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
        Assert.Equal("alice", metadata.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation]);
    }

    [Fact]
    public void Stamp_overwrites_forged_ownership_metadata()
    {
        var metadata = new V1ObjectMeta
        {
            Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = "8" },
            Annotations = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerNameAnnotation] = "bob" }
        };

        ResourceOwnershipStamp.Stamp(metadata, 7, "alice");

        Assert.Equal("7", metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
        Assert.Equal("alice", metadata.Annotations![ResourceOwnershipKeys.OwnerNameAnnotation]);
    }

    [Fact]
    public void Stamp_merges_with_existing_labels_without_trampling()
    {
        var metadata = new V1ObjectMeta
        {
            Labels = new Dictionary<string, string> { ["app"] = "web" }
        };

        ResourceOwnershipStamp.Stamp(metadata, 7, "alice");

        Assert.Equal("web", metadata.Labels!["app"]);
        Assert.Equal("7", metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
    }

    [Fact]
    public void Stamp_uses_invariant_culture_uid()
    {
        var metadata = new V1ObjectMeta();

        ResourceOwnershipStamp.Stamp(metadata, 123, "alice");

        Assert.Equal("123", metadata.Labels![ResourceOwnershipKeys.OwnerUidLabel]);
    }

    [Fact]
    public void CanOperate_owner_label_match_allows()
    {
        var metadata = NewLabeledMetadata(7);

        Assert.True(ResourceOwnershipPolicy.CanOperate(metadata, currentUserId: 7, helmOwnership: null));
    }

    [Fact]
    public void CanOperate_owner_label_mismatch_or_missing_denies()
    {
        Assert.False(ResourceOwnershipPolicy.CanOperate(NewLabeledMetadata(8), currentUserId: 7, helmOwnership: null));
        Assert.False(ResourceOwnershipPolicy.CanOperate(new V1ObjectMeta(), currentUserId: 7, helmOwnership: null));
        Assert.False(ResourceOwnershipPolicy.CanOperate(null, currentUserId: 7, helmOwnership: null));
    }

    [Fact]
    public void CanOperate_anonymous_denies()
    {
        Assert.False(ResourceOwnershipPolicy.CanOperate(NewLabeledMetadata(7), currentUserId: null, helmOwnership: null));
    }

    [Fact]
    public void CanOperate_non_numeric_owner_uid_denies()
    {
        var metadata = new V1ObjectMeta
        {
            Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = "not-a-number" }
        };

        Assert.False(ResourceOwnershipPolicy.CanOperate(metadata, currentUserId: 7, helmOwnership: null));
    }

    [Fact]
    public void CanOperate_helm_ownership_wins_over_labels()
    {
        var metadata = NewHelmMetadata("nginx", "web");
        metadata.Labels = new Dictionary<string, string>();
        var ownership = NewOwnership(ownerUserId: 8);
        // 对象 label 指向 7,Helm 记录指向 8:Helm 记录优先 → 7 不可操作
        metadata.Labels[ResourceOwnershipKeys.OwnerUidLabel] = "7";

        Assert.True(ResourceOwnershipPolicy.CanOperate(metadata, currentUserId: 8, helmOwnership: ownership));
        Assert.False(ResourceOwnershipPolicy.CanOperate(metadata, currentUserId: 7, helmOwnership: ownership));
    }

    [Fact]
    public void EnsureCreationTargetNamespaceAllowed_rejects_kube_prefix_for_member()
    {
        foreach (var ns in new[] { "kube-system", "kube-public", "kube-node-lease" })
        {
            var ex = Assert.Throws<ValidationException>(
                () => ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed(ns, isAdmin: false));
            Assert.Contains("系统命名空间", ex.UserMessage);
        }
    }

    [Fact]
    public void EnsureCreationTargetNamespaceAllowed_allows_default_and_custom_namespaces()
    {
        ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed("default", isAdmin: false);
        ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed("app", isAdmin: false);
    }

    [Fact]
    public void EnsureCreationTargetNamespaceAllowed_admin_bypasses_kube_prefix()
    {
        ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed("kube-system", isAdmin: true);
    }

    [Fact]
    public void EnsureCreationTargetNamespaceAllowed_kube_dot_is_not_protected()
    {
        // 实现口径:仅 kube- 前缀受保护,kube.x 不命中
        ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed("kube.x", isAdmin: false);
    }

    [Fact]
    public void TryGetHelmReleaseReference_requires_both_annotations()
    {
        Assert.Equal(("nginx", "web"), ResourceOwnershipPolicy.TryGetHelmReleaseReference(NewHelmMetadata("nginx", "web")));
        Assert.Null(ResourceOwnershipPolicy.TryGetHelmReleaseReference(new V1ObjectMeta()));
        Assert.Null(ResourceOwnershipPolicy.TryGetHelmReleaseReference(new V1ObjectMeta
        {
            Annotations = new Dictionary<string, string> { [ResourceOwnershipKeys.HelmReleaseNameAnnotation] = "nginx" }
        }));
        Assert.Null(ResourceOwnershipPolicy.TryGetHelmReleaseReference(null));
    }

    [Fact]
    public void CanOperateForIndex_uses_index_hits_and_falls_back_to_labels()
    {
        var helm = NewHelmMetadata("nginx", "web");
        var index = new Dictionary<(string Namespace, string ReleaseName), HelmReleaseOwnership>
        {
            [("web", "nginx")] = NewOwnership(ownerUserId: 8)
        };

        Assert.True(ResourceOwnershipPolicy.CanOperateForIndex(NewLabeledMetadata(8), currentUserId: 8, helmIndex: index));
        Assert.False(ResourceOwnershipPolicy.CanOperateForIndex(helm, currentUserId: 8, helmIndex: new Dictionary<(string Namespace, string ReleaseName), HelmReleaseOwnership>()));
    }

    private static V1ObjectMeta NewLabeledMetadata(int ownerUid) => new()
    {
        Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = ownerUid.ToString(CultureInfo.InvariantCulture) }
    };

    private static V1ObjectMeta NewHelmMetadata(string releaseName, string releaseNamespace) => new()
    {
        Annotations = new Dictionary<string, string>
        {
            [ResourceOwnershipKeys.HelmReleaseNameAnnotation] = releaseName,
            [ResourceOwnershipKeys.HelmReleaseNamespaceAnnotation] = releaseNamespace
        }
    };

    private static HelmReleaseOwnership NewOwnership(int ownerUserId) => new()
    {
        ClusterId = 1,
        Namespace = "web",
        ReleaseName = "nginx",
        OwnerUserId = ownerUserId,
        OwnerUserName = $"user-{ownerUserId}",
        InstalledAt = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc),
        InstalledRevision = 1
    };
}

