using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class ClustersPageFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static async Task<(BunitHost Ctx, IRenderedComponent<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters> Cut, ServiceHarness Harness)> RenderClustersAsync()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("flow-cluster"));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("keep-cluster"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("flow-cluster"));
        return (ctx, cut, harness);
    }

    [Fact]
    public async Task Delete_cluster_via_confirm_dialog_removes_it()
    {
        var (ctx, cut, harness) = await RenderClustersAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var deleteIcon = cut.FindComponents<MudTooltip>()
                .First(t => t.Instance.Text == "删除");
            deleteIcon.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除集群"));

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10 && await harness.ClusterRepo.GetByIdAsync(2) is not null; i++)
            {
                await provider.InvokeAsync(() => { });
            }

            var deleted = await harness.ClusterRepo.GetByIdAsync(2);
            Assert.Null(deleted);
            Assert.NotNull(await harness.ClusterRepo.GetByIdAsync(1));
            Assert.True(harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Batch_mode_toggles_checkbox_column_and_selection_bar()
    {
        var (ctx, cut, harness) = await RenderClustersAsync();
        try
        {
            var batchButton = cut.FindComponents<MudButton>()
                .First(b => b.Markup.Contains("批量操作"));
            await cut.InvokeAsync(async () => await batchButton.Instance.OnClick.InvokeAsync());

            Assert.Contains("退出批量", cut.Markup);
            Assert.NotEmpty(cut.FindComponents<MudCheckBox<bool>>());

            var exit = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("退出批量"));
            await cut.InvokeAsync(async () => await exit.Instance.OnClick.InvokeAsync());

            Assert.DoesNotContain("退出批量", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}

