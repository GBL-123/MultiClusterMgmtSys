using k8s;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class HelmServiceTests
{
    [Fact]
    public async Task ListReleases_maps_entries_and_admin_can_operate_all()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.ReleaseListJson) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        var releases = await service.ListReleasesAsync(cluster.Id);

        Assert.Equal(2, releases.Count);
        Assert.All(releases, release => Assert.True(release.CanOperate));
        var nginx = releases.Single(release => release.Name == "nginx");
        Assert.Equal("web", nginx.Namespace);
        Assert.Equal("nginx-1.2.3", nginx.Chart);
        Assert.Equal("nginx", nginx.ChartName);
        Assert.Equal("1.2.3", nginx.ChartVersion);
        Assert.Equal("1.25.0", nginx.AppVersion);
        Assert.Equal(3, nginx.Revision);
        Assert.Equal("已部署", nginx.StatusText);
        Assert.Equal("online", nginx.StatusCssClass);
        Assert.NotNull(nginx.UpdatedAt);
        var redis = releases.Single(release => release.Name == "redis");
        Assert.Equal("已失败", redis.StatusText);
        Assert.Equal("offline", redis.StatusCssClass);
        Assert.Equal("list", runner.Invocations[0].Arguments[0]);
        Assert.Equal(cluster.Id, runner.Invocations[0].Cluster.Id);
    }

    [Fact]
    public async Task ListReleases_member_can_operate_only_own_records()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, "web", "nginx", ownerUserId: 7, installedRevision: 3));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, "cache", "redis", ownerUserId: 8, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.ReleaseListJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        var releases = await service.ListReleasesAsync(cluster.Id);

        Assert.True(releases.Single(release => release.Name == "nginx").CanOperate);
        Assert.False(releases.Single(release => release.Name == "redis").CanOperate);
    }

    [Fact]
    public async Task ListReleases_treats_release_reinstalled_outside_as_unowned()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, "web", "nginx", ownerUserId: 7, installedRevision: 5));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.ReleaseListJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        var releases = await service.ListReleasesAsync(cluster.Id);

        Assert.False(releases.Single(release => release.Name == "nginx").CanOperate);
    }

    [Fact]
    public async Task ListReleases_throws_not_found_for_missing_cluster()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var service = CreateService(harness, new FakeHelmCliRunner(), TestHttpContext.For("admin", "Admin").Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ListReleasesAsync(999));
    }

    [Fact]
    public async Task GetReleaseDetail_maps_status_fields_and_can_operate()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, "web", "nginx", ownerUserId: 7, installedRevision: 3));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        var detail = await service.GetReleaseDetailAsync(new HelmReleaseKeyRequest(cluster.Id, "nginx", "web"));

        Assert.Equal("nginx", detail.Name);
        Assert.Equal("web", detail.Namespace);
        Assert.Equal(3, detail.Revision);
        Assert.Equal("deployed", detail.Status);
        Assert.Equal("Upgrade complete", detail.Description);
        Assert.Contains("POD_NAME", detail.Notes);
        Assert.Equal("nginx", detail.ChartName);
        Assert.Equal("1.2.3", detail.ChartVersion);
        Assert.Equal("1.25.0", detail.AppVersion);
        Assert.Contains("kind: Deployment", detail.Manifest);
        Assert.NotNull(detail.LastDeployedAt);
        Assert.True(detail.CanOperate);
        Assert.Equal("status", runner.Invocations[0].Arguments[0]);
    }

    [Fact]
    public async Task GetReleaseHistory_maps_items()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.HistoryJson) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        var history = await service.GetReleaseHistoryAsync(new HelmReleaseKeyRequest(cluster.Id, "nginx", "web"));

        Assert.Equal(2, history.Count);
        Assert.Equal(1, history[0].Revision);
        Assert.Equal("已取代", history[0].StatusText);
        Assert.Equal("Install complete", history[0].Description);
        Assert.Equal(3, history[1].Revision);
        Assert.Equal("已部署", history[1].StatusText);
    }

    [Fact]
    public async Task GetReleaseValues_returns_yaml_text()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.UserValuesYaml) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        var values = await service.GetReleaseValuesAsync(new HelmReleaseKeyRequest(cluster.Id, "nginx", "web"));

        Assert.Contains("replicaCount: 2", values);
        Assert.Equal(["get", "values", "nginx", "-n", "web", "-o", "yaml", "--kubeconfig", "{kubeconfig}"], runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task GetReleaseManifest_returns_text()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.Manifest) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        var manifest = await service.GetReleaseManifestAsync(new HelmReleaseKeyRequest(cluster.Id, "nginx", "web"));

        Assert.Contains("kind: Deployment", manifest);
        Assert.Equal("get", runner.Invocations[0].Arguments[0]);
    }

    [Fact]
    public async Task Read_failure_translates_to_business_exception()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.ListReleasesAsync(cluster.Id));

        Assert.Contains("Release 不存在", exception.UserMessage);
    }

    [Fact]
    public async Task GetNamespaces_returns_sorted_namespace_names()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var k8s = K8sMocks.Create();
        k8s.SetupListNamespaces("web", "cache");
        var service = CreateService(harness, new FakeHelmCliRunner(), TestHttpContext.For("admin", "Admin").Object, k8s);

        var namespaces = await service.GetNamespacesAsync(cluster.Id);

        Assert.Equal(["cache", "web"], namespaces);
    }

    [Fact]
    public void ParseChartPackage_maps_metadata_and_values()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var service = CreateService(harness, new FakeHelmCliRunner(), TestHttpContext.For("admin", "Admin").Object);
        var package = HelmTestPackages.Create(
            ("nginx/Chart.yaml", "apiVersion: v2\nname: nginx\nversion: 1.2.3\nappVersion: \"1.25.0\"\n"),
            ("nginx/values.yaml", "replicaCount: 2\n"));

        var info = service.ParseChartPackage(package);

        Assert.Equal("nginx", info.Name);
        Assert.Equal("1.2.3", info.Version);
        Assert.Equal("1.25.0", info.AppVersion);
        Assert.Equal("nginx", info.SuggestedReleaseName);
        Assert.Contains("replicaCount: 2", info.ValuesYaml);
    }

    [Fact]
    public void ParseChartPackage_rejects_oversized_package()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var service = CreateService(harness, new FakeHelmCliRunner(), TestHttpContext.For("admin", "Admin").Object, maxPackageBytes: 4);

        Assert.Throws<ValidationException>(() => service.ParseChartPackage(new byte[5]));
    }

    private static HelmService CreateService(
        ServiceHarness harness,
        FakeHelmCliRunner runner,
        IHttpContextAccessor accessor,
        Mock<IKubernetes>? k8s = null,
        long maxPackageBytes = 52_428_800)
        => new(
            harness.ClusterRepo,
            harness.OwnershipRepo,
            runner,
            new HelmOptions { MaxPackageBytes = maxPackageBytes },
            K8sMocks.Cache(k8s ?? K8sMocks.Create()),
            accessor,
            harness.Audit,
            NullLogger<HelmService>.Instance);

    private static HelmReleaseOwnership NewOwnership(int clusterId, string namespaceName, string releaseName, int ownerUserId, int installedRevision) => new()
    {
        ClusterId = clusterId,
        Namespace = namespaceName,
        ReleaseName = releaseName,
        OwnerUserId = ownerUserId,
        OwnerUserName = $"user-{ownerUserId}",
        InstalledAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
        InstalledRevision = installedRevision
    };
}
