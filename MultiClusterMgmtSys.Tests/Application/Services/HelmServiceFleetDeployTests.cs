using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class HelmServiceFleetDeployTests
{
    [Fact]
    public async Task Deploy_requires_admin_and_runs_no_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.DeployToFleetAsync(NewRequest([cluster.Id])));

        Assert.Empty(runner.Invocations);
        Assert.Null(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deploy_rejects_invalid_release_name_without_running_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, Admin());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.DeployToFleetAsync(NewRequest([cluster.Id], releaseName: "Nginx")));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Deploy_rejects_invalid_namespace_without_running_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, Admin());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.DeployToFleetAsync(NewRequest([cluster.Id], namespaceName: "Web")));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Deploy_rejects_oversized_package_without_running_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, Admin(), maxPackageBytes: 4);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.DeployToFleetAsync(NewRequest([cluster.Id], chartPackage: new byte[5])));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Deploy_rejects_empty_cluster_ids_without_running_helm()
    {
        using var harness = new ServiceHarness("alice");
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, Admin());

        await Assert.ThrowsAsync<ValidationException>(
            () => service.DeployToFleetAsync(NewRequest([])));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Deploy_rejects_unknown_cluster_ids_and_runs_no_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, Admin());

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.DeployToFleetAsync(NewRequest([cluster.Id, 99])));

        Assert.Empty(runner.Invocations);
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deploy_installs_missing_releases_with_unified_values()
    {
        using var harness = new ServiceHarness("alice");
        var first = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var second = await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging"));
        var runner = new FakeHelmCliRunner
        {
            Handler = invocation => invocation.Arguments[0] == "status"
                ? FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError)
                : FakeHelmCliRunner.Succeeded()
        };
        var service = CreateService(harness, runner, Admin());
        var package = new byte[] { 1, 2, 3 };

        var result = await service.DeployToFleetAsync(
            NewRequest([first.Id, second.Id], valuesYaml: "replicaCount: 2", createNamespace: true, wait: true));

        Assert.True(result.SuccessCount == 2 && result.FailureCount == 0);
        Assert.Equal(first.Id, result.Items[0].ClusterId);
        Assert.Equal("prod", result.Items[0].ClusterName);
        Assert.Equal(HelmFleetDeployAction.Install, result.Items[0].Action);
        Assert.True(result.Items[0].Succeeded);
        Assert.Equal(HelmFleetDeployAction.Install, result.Items[1].Action);
        Assert.Equal(2, runner.Invocations.Count(invocation => invocation.Arguments[0] == "status"));
        var installs = runner.Invocations.Where(invocation => invocation.Arguments[0] == "install").ToList();
        Assert.Equal(2, installs.Count);
        Assert.Equal(
            [
                "install", "nginx", "{chart}", "-n", "web", "-f", "{values}",
                "--create-namespace", "--wait", "--timeout", "300s", "--kubeconfig", "{kubeconfig}"
            ],
            installs[0].Arguments);
        Assert.Equal([1, 2], [.. installs.Select(invocation => invocation.Cluster.Id).OrderBy(id => id)]);
        Assert.Equal([2, 2], installs.Select(invocation => invocation.Files.Single(file => file.Name == "chart.tgz").Content.Length));
        var ownership = await harness.OwnershipRepo.GetAsync(first.Id, "web", "nginx");
        Assert.NotNull(ownership);
        Assert.Equal(7, ownership!.OwnerUserId);
        Assert.Equal("alice", ownership.OwnerUserName);
        Assert.Equal(1, ownership.InstalledRevision);
        var audits = await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, audits.Count);
        Assert.All(audits, audit => Assert.Equal(AuditCategory.Helm, audit.Category));
        Assert.All(audits, audit => Assert.Equal(AuditAction.Install, audit.Action));
        Assert.All(audits, audit => Assert.Contains("批量下发安装", audit.Target));
        Assert.Equal("alice", audits[0].UserName);
    }

    [Fact]
    public async Task Deploy_upgrades_existing_releases_without_reuse_values()
    {
        using var harness = new ServiceHarness("alice");
        var first = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var second = await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(first.Id, ownerUserId: 8, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, Admin());

        var result = await service.DeployToFleetAsync(NewRequest([first.Id, second.Id], valuesYaml: "replicaCount: 2"));

        Assert.True(result.SuccessCount == 2 && result.FailureCount == 0);
        Assert.All(result.Items, item => Assert.Equal(HelmFleetDeployAction.Upgrade, item.Action));
        Assert.All(
            runner.Invocations.Where(invocation => invocation.Arguments[0] == "upgrade").ToList(),
            upgrade =>
            {
                Assert.DoesNotContain("--reuse-values", upgrade.Arguments);
                Assert.Contains("-f", upgrade.Arguments);
            });
        var ownership = await harness.OwnershipRepo.GetAsync(first.Id, "web", "nginx");
        Assert.Equal(8, ownership!.OwnerUserId);
        Assert.Equal(1, ownership.InstalledRevision);
        var audits = await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, audits.Count);
        Assert.All(audits, audit => Assert.Equal(AuditAction.Upgrade, audit.Action));
        Assert.Contains(audits, audit => audit.Target.Contains("批量下发升级"));
    }

    [Fact]
    public async Task Deploy_isolates_cluster_failures_and_keeps_batch_running()
    {
        using var harness = new ServiceHarness("alice");
        var first = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var second = await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging"));
        var third = await harness.ClusterRepo.AddAsync(TestData.NewCluster("dev"));
        var runner = new FakeHelmCliRunner
        {
            Handler = invocation => invocation.Cluster.Id switch
            {
                2 => FakeHelmCliRunner.Failed(HelmFixtures.ConflictError),
                _ when invocation.Arguments[0] == "status" =>
                    FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError),
                _ => FakeHelmCliRunner.Succeeded()
            }
        };
        var service = CreateService(harness, runner, Admin());

        var result = await service.DeployToFleetAsync(NewRequest([first.Id, second.Id, third.Id], valuesYaml: "replicaCount: 2"));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        var failed = result.Items.Single(item => !item.Succeeded);
        Assert.Equal(second.Id, failed.ClusterId);
        Assert.Contains("已存在同名 release", failed.Message);
        var audits = await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, audits.Count);
        Assert.Equal(2, runner.Invocations.Count(invocation => invocation.Arguments[0] == "install"));
        Assert.Equal(3, runner.Invocations.Count(invocation => invocation.Arguments[0] == "status"));
        Assert.DoesNotContain(runner.Invocations, invocation => invocation.Arguments[0] == "upgrade");
        var ownerships = await harness.Db.HelmReleaseOwnerships.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, ownerships.Count);
        Assert.DoesNotContain(ownerships, ownership => ownership.ClusterId == second.Id);
        Assert.Contains(ownerships, ownership => ownership.ClusterId == first.Id);
        Assert.Contains(ownerships, ownership => ownership.ClusterId == third.Id);
    }

    [Fact]
    public async Task Deploy_reports_progress_per_cluster()
    {
        using var harness = new ServiceHarness("alice");
        var first = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var second = await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging"));
        var third = await harness.ClusterRepo.AddAsync(TestData.NewCluster("dev"));
        var runner = new FakeHelmCliRunner
        {
            Handler = invocation => invocation.Arguments[0] == "status"
                ? FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError)
                : FakeHelmCliRunner.Succeeded()
        };
        var service = CreateService(harness, runner, Admin());
        var reports = new List<(int Current, int Total)>();

        var result = await service.DeployToFleetAsync(
            NewRequest([first.Id, second.Id, third.Id]),
            new CollectingProgress(reports.Add));

        Assert.Equal(3, reports.Count);
        Assert.Equal([(1, 3), (2, 3), (3, 3)], [.. reports.OrderBy(report => report.Current)]);
        Assert.Equal(3, result.SuccessCount);
    }

    [Fact]
    public async Task Deploy_preflight_unreachable_records_failure_without_deploying()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner
        {
            Handler = _ => FakeHelmCliRunner.Failed(HelmFixtures.UnreachableError)
        };
        var service = CreateService(harness, runner, Admin());

        var result = await service.DeployToFleetAsync(NewRequest([cluster.Id]));

        Assert.True(result.SuccessCount == 0 && result.FailureCount == 1);
        Assert.Equal(ClusterUnreachableExceptionMessage, result.Items.Single().Message);
        Assert.Single(runner.Invocations);
        Assert.Equal("status", runner.Invocations.Single().Arguments[0]);
        Assert.Null(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deploy_unclassified_failure_records_failure_item()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner
        {
            Handler = invocation => invocation.Arguments[0] == "status"
                ? FakeHelmCliRunner.Failed(HelmFixtures.TemplateError)
                : FakeHelmCliRunner.Succeeded()
        };
        var service = CreateService(harness, runner, Admin());

        var result = await service.DeployToFleetAsync(NewRequest([cluster.Id]));

        Assert.True(result.SuccessCount == 0 && result.FailureCount == 1);
        Assert.Contains("Helm 操作失败", result.Items.Single().Message);
        Assert.Null(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
    }

    private const string ClusterUnreachableExceptionMessage = "无法连接目标集群,请检查集群连通性与凭据";

    private static IHttpContextAccessor Admin() => TestHttpContext.ForIdentity("alice", 7, "Admin").Object;

    private static HelmFleetDeployRequest NewRequest(
        IReadOnlyList<int> clusterIds,
        string releaseName = "nginx",
        string namespaceName = "web",
        byte[]? chartPackage = null,
        string valuesYaml = "",
        bool createNamespace = false,
        bool wait = false)
        => new(namespaceName, releaseName, chartPackage ?? [1, 2], valuesYaml, createNamespace, wait, clusterIds);

    private static Func<HelmCliInvocation, HelmCliResult> Route(string statusJson)
        => invocation => invocation.Arguments[0] == "status"
            ? FakeHelmCliRunner.Succeeded(statusJson)
            : FakeHelmCliRunner.Succeeded();

    private static HelmService CreateService(
        ServiceHarness harness,
        FakeHelmCliRunner runner,
        IHttpContextAccessor accessor,
        long maxPackageBytes = 52_428_800)
        => new(
            harness.ClusterRepo,
            harness.OwnershipRepo,
            runner,
            new HelmOptions { MaxPackageBytes = maxPackageBytes },
            K8sMocks.Cache(K8sMocks.Create()),
            accessor,
            harness.Audit,
            NullLogger<HelmService>.Instance);

    private static HelmReleaseOwnership NewOwnership(int clusterId, int ownerUserId, int installedRevision) => new()
    {
        ClusterId = clusterId,
        Namespace = "web",
        ReleaseName = "nginx",
        OwnerUserId = ownerUserId,
        OwnerUserName = $"user-{ownerUserId}",
        InstalledAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
        InstalledRevision = installedRevision
    };

    private sealed class CollectingProgress(Action<(int Current, int Total)> onReport) : IProgress<(int Current, int Total)>
    {
        public void Report((int Current, int Total) value) => onReport(value);
    }
}
