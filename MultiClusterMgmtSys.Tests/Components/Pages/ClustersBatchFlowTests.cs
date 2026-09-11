using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class ClustersBatchFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, int GroupId, IRenderedComponent<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters> Cut)> RenderAsync()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        var group = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("side-group"));
        await harness.Db.SaveChangesAsync(CancellationToken.None);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("batch-cluster"));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("second-cluster"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("batch-cluster"));
        return (ctx, harness, group.Entity.Id, cut);
    }

    [Fact]
    public async Task Batch_mode_select_and_clear_selection_flow()
    {
        var (ctx, harness, groupId, cut) = await RenderAsync();
        try
        {
            var batchButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("批量操作"));
            await cut.InvokeAsync(async () => await batchButton.Instance.OnClick.InvokeAsync());

            Assert.Contains("退出批量", cut.Markup);
            Assert.NotEmpty(cut.FindComponents<MudCheckBox<bool>>());

            var checkbox = cut.FindComponents<MudCheckBox<bool>>().First();
            await cut.InvokeAsync(async () => await checkbox.Instance.ValueChanged!.InvokeAsync(true));

            cut.WaitForState(() => cut.Markup.Contains("个集群已选"), TimeSpan.FromSeconds(5));

            var clearButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("清空选择"));
            await cut.InvokeAsync(async () => await clearButton.Instance.OnClick.InvokeAsync());

            cut.WaitForState(() => !cut.Markup.Contains("个集群已选"), TimeSpan.FromSeconds(5));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Sidebar_group_click_navigates_with_group_query()
    {
        var (ctx, harness, groupId, cut) = await RenderAsync();
        try
        {
            var groupItem = cut.FindAll(".mud-list-item")
                .First(e => e.TextContent.Contains("side-group"));
            groupItem.Click();

            await cut.InvokeAsync(() => { });

            var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
            Assert.Contains($"group={groupId}", navigationManager.Uri);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_group_button_opens_dialog()
    {
        var (ctx, harness, groupId, cut) = await RenderAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var createTooltip = cut.FindComponents<MudTooltip>()
                .First(t => t.Instance.Text == "新建分组");
            createTooltip.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));

            Assert.Contains("分组名称", provider.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}
