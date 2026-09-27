using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class ResourceOwnershipGuardTests
{
    [Fact]
    public async Task CanOperateAsync_admin_short_circuits_everything()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var guard = NewGuard(harness, TestHttpContext.For("admin", "Admin").Object);

        Assert.True(guard.IsAdmin());
        Assert.True(await guard.CanOperateAsync(7, metadata: new V1ObjectMeta()));
        Assert.True(await guard.CanOperateAsync(7, metadata: null));
    }

    [Fact]
    public async Task CanOperateAsync_owner_label_match_allows_and_mismatch_denies()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);
        var mine = NewLabeledMetadata(7);
        var theirs = NewLabeledMetadata(8);

        Assert.True(await guard.CanOperateAsync(9, mine));
        Assert.False(await guard.CanOperateAsync(9, theirs));
        Assert.False(await guard.CanOperateAsync(9, new V1ObjectMeta()));
    }

    [Fact]
    public async Task CanOperateAsync_helm_managed_object_falls_back_to_ownership_repo()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);
        var clusterId = await SeedClusterAsync(harness, "helm-cluster");
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(clusterId, releaseName: "nginx", rsNamespace: "web", ownerUserId: 7));

        Assert.True(await guard.CanOperateAsync(clusterId, NewHelmMetadata("nginx", "web")));
        Assert.False(await guard.CanOperateAsync(clusterId, NewHelmMetadata("redis", "cache")));
    }

    [Fact]
    public async Task RequireOperateAsync_admin_passes_silently()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var guard = NewGuard(harness, TestHttpContext.For("admin", "Admin").Object);

        await guard.RequireOperateAsync(7, "web", "nginx", new V1ObjectMeta());
    }

    [Fact]
    public async Task RequireOperateAsync_owner_passes()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);

        await guard.RequireOperateAsync(7, "web", "nginx", NewLabeledMetadata(7));
    }

    [Fact]
    public async Task RequireOperateAsync_other_user_or_unowned_throws_chinese_permission()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);

        var ex1 = await Assert.ThrowsAsync<PermissionException>(
            () => guard.RequireOperateAsync(7, "web", "nginx", NewLabeledMetadata(8)));
        var ex2 = await Assert.ThrowsAsync<PermissionException>(
            () => guard.RequireOperateAsync(7, "web", "nginx", new V1ObjectMeta()));

        Assert.Contains("仅可操作自己创建", ex1.UserMessage);
        Assert.Contains("仅可操作自己创建", ex2.UserMessage);
    }

    [Fact]
    public async Task RequireOperateAsync_helm_interlock_respects_repo()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);
        var clusterId = await SeedClusterAsync(harness, "helm-interlock");
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(clusterId, releaseName: "nginx", rsNamespace: "web", ownerUserId: 7));

        await guard.RequireOperateAsync(clusterId, "web", "nginx", NewHelmMetadata("nginx", "web"));

        var ex = await Assert.ThrowsAsync<PermissionException>(
            () => guard.RequireOperateAsync(clusterId, "web", "nginx", NewHelmMetadata("redis", "cache")));
        Assert.Contains("仅可操作自己创建", ex.UserMessage);
    }

    [Fact]
    public void RequireCreator_returns_identity_for_logged_in()
    {
        using var harness = new ServiceHarness("bob");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("bob", 3).Object);

        Assert.Equal((3, "bob"), guard.RequireCreator());
    }

    [Fact]
    public void RequireCreator_anonymous_throws_permission()
    {
        using var harness = new ServiceHarness();
        var guard = NewGuard(harness, TestHttpContext.Anonymous().Object);

        var ex = Assert.Throws<PermissionException>(() => guard.RequireCreator());
        Assert.Contains("无法获取当前登录账号信息", ex.UserMessage);
    }

    [Fact]
    public async Task GetHelmOwnershipIndex_returns_records_of_that_cluster_only()
    {
        using var harness = new ServiceHarness("alice");
        var guard = NewGuard(harness, TestHttpContext.ForIdentity("alice", 7).Object);
        var clusterA = await SeedClusterAsync(harness, "helm-index-a");
        var clusterB = await SeedClusterAsync(harness, "helm-index-b");
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(clusterA, releaseName: "nginx", rsNamespace: "web", ownerUserId: 7));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(clusterB, releaseName: "redis", rsNamespace: "cache", ownerUserId: 7));

        var index = await guard.GetHelmOwnershipIndexAsync(clusterA);

        Assert.Single(index);
        Assert.True(index.ContainsKey(("web", "nginx")));
    }

    private static async Task<int> SeedClusterAsync(ServiceHarness harness, string name)
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private static ResourceOwnershipGuard NewGuard(ServiceHarness harness, IHttpContextAccessor accessor)
        => new(harness.OwnershipRepo, accessor, NullLogger<ResourceOwnershipGuard>.Instance);

    private static V1ObjectMeta NewLabeledMetadata(int ownerUid) => new()
    {
        Labels = new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = ownerUid.ToString() }
    };

    private static V1ObjectMeta NewHelmMetadata(string releaseName, string releaseNamespace) => new()
    {
        Annotations = new Dictionary<string, string>
        {
            [ResourceOwnershipKeys.HelmReleaseNameAnnotation] = releaseName,
            [ResourceOwnershipKeys.HelmReleaseNamespaceAnnotation] = releaseNamespace
        }
    };

    private static HelmReleaseOwnership NewOwnership(int clusterId, string releaseName, string rsNamespace, int ownerUserId) => new()
    {
        ClusterId = clusterId,
        Namespace = rsNamespace,
        ReleaseName = releaseName,
        OwnerUserId = ownerUserId,
        OwnerUserName = $"user-{ownerUserId}",
        InstalledAt = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc),
        InstalledRevision = 1
    };
}
