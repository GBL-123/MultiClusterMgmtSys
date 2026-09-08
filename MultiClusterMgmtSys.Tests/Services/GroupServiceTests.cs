using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class GroupServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly GroupService service;

    public GroupServiceTests()
    {
        service = new GroupService(
            new GroupRepository(harness.Db),
            harness.ClusterRepo,
            harness.Audit,
            NullLogger<GroupService>.Instance);
    }

    public void Dispose() => harness.Dispose();


    [Fact]
    public async Task GetGroupsAsync_returns_view_models_with_counts()
    {
        var g1 = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("g1"));
        await harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("c1", groupId: g1.Entity.Id));

        var groups = await service.GetGroupsAsync();

        var vm = Assert.Single(groups);
        Assert.Equal("g1", vm.Name);
        Assert.Equal(1, vm.ClusterCount);
    }

    [Fact]
    public async Task AddGroupAsync_creates_and_audits()
    {
        var vm = await service.AddGroupAsync("prod");

        Assert.True(vm.Id > 0);
        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Group, audit.Category);
        Assert.Equal(AuditAction.Create, audit.Action);
    }

    [Fact]
    public async Task DeleteGroupAsync_removes_and_audits()
    {
        var added = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("doomed"));
        await harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.DeleteGroupAsync(added.Entity.Id);

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteGroupAsync_missing_is_silent()
    {
        await service.DeleteGroupAsync(999);

        Assert.Empty(harness.Db.AuditLogs);
    }

    [Fact]
    public async Task RenameGroupAsync_missing_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.RenameGroupAsync(new GroupRenameRequest(999, "new")));
    }

    [Fact]
    public async Task RenameGroupAsync_renames_and_audits()
    {
        var added = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("old"));
        await harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.RenameGroupAsync(new GroupRenameRequest(added.Entity.Id, "fresh"));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Rename, audit.Action);
        Assert.Contains("fresh", audit.Target);
    }

    [Fact]
    public async Task MoveClustersToGroupAsync_sentinel_zero_throws_validation()
    {
        var request = new MoveClustersRequest([1], TargetGroupId: 0);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.MoveClustersToGroupAsync(request));

        Assert.Contains("目标分组无效", ex.UserMessage);
    }

    [Fact]
    public async Task MoveClustersToGroupAsync_moves_and_audits_with_group_name()
    {
        var g1 = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("target")).AsTask();
        await harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var c1 = await harness.ClusterRepo.AddAsync(TestData.NewCluster("c1"));

        var affected = await service.MoveClustersToGroupAsync(
            new MoveClustersRequest([c1.Id], g1.Entity.Id));

        Assert.Equal(1, affected);
        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Move, audit.Action);
        Assert.Contains("target", audit.Target);
    }

    [Fact]
    public async Task MoveClustersToGroupAsync_null_target_ungroups()
    {
        var g1 = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("g")).AsTask();
        await harness.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var c1 = await harness.ClusterRepo.AddAsync(TestData.NewCluster("c1", groupId: g1.Entity.Id));

        var affected = await service.MoveClustersToGroupAsync(
            new MoveClustersRequest([c1.Id], TargetGroupId: null));

        Assert.Equal(1, affected);
        var audit = harness.Db.AuditLogs.Single();
        Assert.Contains("未分组", audit.Target);
    }

    [Fact]
    public async Task GetUngroupedClusterCountAsync_counts()
    {
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("loose"));

        Assert.Equal(1, await service.GetUngroupedClusterCountAsync());
    }
}

