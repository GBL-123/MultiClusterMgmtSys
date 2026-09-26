using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class HelmServiceWriteTests
{
    [Fact]
    public async Task Install_writes_ownership_and_audit_on_success()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);
        var package = new byte[] { 1, 2, 3 };

        await service.InstallAsync(new HelmInstallRequest(cluster.Id, "web", "nginx", package, "replicaCount: 2", CreateNamespace: true, Wait: false));

        var ownership = await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx");
        Assert.NotNull(ownership);
        Assert.Equal(7, ownership!.OwnerUserId);
        Assert.Equal("alice", ownership.OwnerUserName);
        Assert.Equal(1, ownership.InstalledRevision);
        Assert.Equal(
            ["install", "nginx", "{chart}", "-n", "web", "-f", "{values}", "--create-namespace", "--kubeconfig", "{kubeconfig}"],
            runner.Invocations[0].Arguments);
        Assert.Equal(package, runner.Invocations[0].Files.Single(file => file.Name == "chart.tgz").Content);
        var audit = Assert.Single(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(AuditCategory.Helm, audit.Category);
        Assert.Equal(AuditAction.Install, audit.Action);
        Assert.Contains("安装 nginx", audit.Target);
        Assert.Contains("web", audit.Target);
        Assert.Equal("alice", audit.UserName);
    }

    [Fact]
    public async Task Install_failure_does_not_write_ownership_or_audit()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = _ => FakeHelmCliRunner.Failed(HelmFixtures.ConflictError) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.InstallAsync(new HelmInstallRequest(cluster.Id, "web", "nginx", [1], "", false, false)));

        Assert.Null(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
        Assert.Empty(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Install_rejects_invalid_release_name_without_running_helm()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.InstallAsync(new HelmInstallRequest(cluster.Id, "web", "Nginx", [1], "", false, false)));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Install_rejects_oversized_package()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object, maxPackageBytes: 4);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.InstallAsync(new HelmInstallRequest(cluster.Id, "web", "nginx", new byte[5], "", false, false)));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Install_requires_logged_in_identity()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner();
        var service = CreateService(harness, runner, TestHttpContext.Anonymous().Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.InstallAsync(new HelmInstallRequest(cluster.Id, "web", "nginx", [1], "", false, false)));

        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task Upgrade_owner_reuses_values_and_keeps_ownership()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, 7, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1, 2], ValuesYaml: "", ReuseValues: true, Wait: false));

        var upgradeInvocation = runner.Invocations.Single(invocation => invocation.Arguments[0] == "upgrade");
        Assert.Contains("--reuse-values", upgradeInvocation.Arguments);
        Assert.DoesNotContain("-f", upgradeInvocation.Arguments);
        var ownership = await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx");
        Assert.Equal(7, ownership!.OwnerUserId);
        Assert.Equal(1, ownership.InstalledRevision);
        var audit = Assert.Single(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(AuditAction.Upgrade, audit.Action);
    }

    [Fact]
    public async Task Upgrade_edited_values_passes_values_file()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, 7, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1, 2], "replicaCount: 3", ReuseValues: false, Wait: false));

        var upgradeInvocation = runner.Invocations.Single(invocation => invocation.Arguments[0] == "upgrade");
        Assert.Contains("-f", upgradeInvocation.Arguments);
        Assert.Contains(upgradeInvocation.Files, file => file.Name == "values.yaml");
        Assert.DoesNotContain("--reuse-values", upgradeInvocation.Arguments);
    }

    [Fact]
    public async Task Upgrade_denied_for_non_owner_member()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, ownerUserId: 8, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        var exception = await Assert.ThrowsAsync<PermissionException>(
            () => service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1], "", true, false)));

        Assert.Contains("仅可操作自己安装", exception.UserMessage);
        Assert.DoesNotContain(runner.Invocations, invocation => invocation.Arguments[0] == "upgrade");
    }

    [Fact]
    public async Task Upgrade_denied_for_unowned_release()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1], "", true, false)));
    }

    [Fact]
    public async Task Upgrade_denied_when_release_reinstalled_outside()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, 7, installedRevision: 5));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1], "", true, false)));
    }

    [Fact]
    public async Task Upgrade_allowed_for_admin_on_others_release()
    {
        using var harness = new ServiceHarness("admin", "Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, ownerUserId: 8, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.For("admin", "Admin").Object);

        await service.UpgradeAsync(new HelmUpgradeRequest(cluster.Id, "web", "nginx", [1], "", true, false));

        var audit = Assert.Single(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("admin", audit.UserName);
        Assert.Equal(AuditAction.Upgrade, audit.Action);
    }

    [Fact]
    public async Task Rollback_builds_revision_command_and_audits()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, 7, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.RollbackAsync(new HelmRollbackRequest(cluster.Id, "web", "nginx", Revision: 1));

        var rollbackInvocation = runner.Invocations.Single(invocation => invocation.Arguments[0] == "rollback");
        Assert.Equal(
            ["rollback", "nginx", "1", "-n", "web", "--kubeconfig", "{kubeconfig}"],
            rollbackInvocation.Arguments);
        var audit = Assert.Single(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(AuditAction.Rollback, audit.Action);
        Assert.Contains("revision 1", audit.Target);
    }

    [Fact]
    public async Task Uninstall_deletes_ownership_and_audits()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, 7, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await service.UninstallAsync(new HelmUninstallRequest(cluster.Id, "web", "nginx", KeepHistory: true));

        var uninstallInvocation = runner.Invocations.Single(invocation => invocation.Arguments[0] == "uninstall");
        Assert.Contains("--keep-history", uninstallInvocation.Arguments);
        Assert.Null(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
        var audit = Assert.Single(await harness.Db.AuditLogs.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(AuditAction.Uninstall, audit.Action);
        Assert.Contains("卸载 nginx", audit.Target);
    }

    [Fact]
    public async Task Uninstall_denied_for_non_owner_member()
    {
        using var harness = new ServiceHarness("alice");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        await harness.OwnershipRepo.UpsertAsync(NewOwnership(cluster.Id, ownerUserId: 8, installedRevision: 1));
        var runner = new FakeHelmCliRunner { Handler = Route(HelmFixtures.StatusJson) };
        var service = CreateService(harness, runner, TestHttpContext.ForIdentity("alice", 7).Object);

        await Assert.ThrowsAsync<PermissionException>(
            () => service.UninstallAsync(new HelmUninstallRequest(cluster.Id, "web", "nginx", false)));

        Assert.NotNull(await harness.OwnershipRepo.GetAsync(cluster.Id, "web", "nginx"));
    }

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
}
