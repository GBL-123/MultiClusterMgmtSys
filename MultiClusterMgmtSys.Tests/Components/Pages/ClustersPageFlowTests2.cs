using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class ClustersPageFlowTests2
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, IRenderedComponent<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters> Cut, Microsoft.EntityFrameworkCore.DbUpdateException? Unused, ClusterGroup Doomed, ClusterGroup Target)> RenderAsync()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        var doomedGroup = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("doomed-group"));
        var targetGroup = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("target-group"));
        await harness.Db.SaveChangesAsync(CancellationToken.None);
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("refresh-me", groupId: targetGroup.Entity.Id));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("other-cluster"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("target-group"));
        return (ctx, harness, k8s, cut, null!, doomedGroup.Entity, targetGroup.Entity);
    }

    [Fact]
    public async Task Refresh_all_updates_status_to_offline()
    {
        var (ctx, harness, k8s, cut, _, doomedGroupEntry, targetGroupEntry) = await RenderAsync();
        try
        {
            var refreshButton = cut.FindComponents<MudButton>()
                .First(b => b.Markup.Contains("刷新所有集群"));
            refreshButton.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await cut.InvokeAsync(() => { });
                var statuses = await harness.ClusterRepo.GetAllIdsAsync();
                var allOffline = true;
                foreach (var id in statuses)
                {
                    var cluster = await harness.ClusterRepo.GetByIdAsync(id);
                    if (cluster is not null && cluster.Status != ClusterStatus.Offline)
                    {
                        allOffline = false;
                        break;
                    }
                }
                if (allOffline) break;
            }

            var ids = await harness.ClusterRepo.GetAllIdsAsync();
            foreach (var id in ids)
            {
                var cluster = await harness.ClusterRepo.GetByIdAsync(id);
                Assert.Equal(ClusterStatus.Offline, cluster!.Status);
            }
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Delete_group_via_sidebar_confirm_flow()
    {
        var (ctx, harness, k8s, cut, _, doomedGroupEntry, targetGroupEntry) = await RenderAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var deleteTooltips = cut.FindComponents<MudTooltip>()
                .Where(t => t.Instance.Text == "删除分组")
                .ToList();
            Assert.Equal(2, deleteTooltips.Count);
            var doomedTooltip = deleteTooltips
                .First(t => t.Instance.Text == "删除分组");
            doomedTooltip.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除分组"), TimeSpan.FromSeconds(5));

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                if (await harness.Db.ClusterGroups.FindAsync(doomedGroupEntry.Id) is null) break;
            }

            Assert.Null(await harness.Db.ClusterGroups.FindAsync(doomedGroupEntry.Id));
            Assert.NotNull(await harness.Db.ClusterGroups.FindAsync(targetGroupEntry.Id));
            Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Group && a.Action == AuditAction.Delete && a.Target.Contains("doomed-group")));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}






