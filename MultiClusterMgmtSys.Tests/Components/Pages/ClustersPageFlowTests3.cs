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

public class ClustersPageFlowTests3
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Row_refresh_marks_cluster_offline()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<k8s.KubernetesClientConfiguration, k8s.IKubernetes>>(K8sMocks.Factory(k8s));
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("row-refresh", status: ClusterStatus.Online));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("row-refresh"));

        var refreshTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "刷新");
        refreshTooltip.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await cut.InvokeAsync(() => { });
            var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
            if (reloaded!.Status == ClusterStatus.Offline)
            {
                break;
            }
        }

        var final = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
        Assert.Equal(ClusterStatus.Offline, final!.Status);
    }

    [Fact]
    public async Task Add_cluster_button_opens_edit_dialog()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<k8s.KubernetesClientConfiguration, k8s.IKubernetes>>(K8sMocks.Factory(k8s));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("opener"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("opener"));

        var provider = ctx.Render<MudDialogProvider>();
        var addButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("添加集群"));
        addButton.Find("button").Click();

        provider.WaitForState(() => provider.Markup.Contains("集群名称"), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Rename_group_dialog_opens_with_prefill()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<k8s.KubernetesClientConfiguration, k8s.IKubernetes>>(K8sMocks.Factory(k8s));
        var group = await harness.Db.ClusterGroups.AddAsync(TestData.NewGroup("rename-src"));
        await harness.Db.SaveChangesAsync(CancellationToken.None);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.Clusters>();
        cut.WaitForState(() => cut.Markup.Contains("rename-src"));

        var provider = ctx.Render<MudDialogProvider>();
        var editTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "编辑分组");
        editTooltip.Find("button").Click();

        provider.WaitForState(() => provider.Markup.Contains("rename-src"), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Cluster_detail_refresh_and_tabs_render()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<k8s.KubernetesClientConfiguration, k8s.IKubernetes>>(K8sMocks.Factory(k8s));
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-tabs", status: ClusterStatus.Online));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.ClusterDetail>(
            parameters => parameters.Add(p => p.Id, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("detail-tabs"));

        var refresh = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("刷新状态"));
        refresh.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await cut.InvokeAsync(() => { });
            var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
            if (reloaded!.Status == ClusterStatus.Offline) break;
        }

        Assert.Equal(ClusterStatus.Offline, (await harness.ClusterRepo.GetByIdAsync(cluster.Id))!.Status);

        cut.FindAll(".mud-tab")[1].Click();
        cut.WaitForState(() => cut.Markup.Contains("端点"));

        cut.FindAll(".mud-tab")[2].Click();
        cut.WaitForState(() => cut.Markup.Contains("节点"));
    }

    [Fact]
    public async Task Cluster_detail_delete_confirm_navigates_to_list()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<k8s.KubernetesClientConfiguration, k8s.IKubernetes>>(K8sMocks.Factory(k8s));
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-delete"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Pages.ClusterDetail>(
            parameters => parameters.Add(p => p.Id, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("detail-delete"));

        var provider = ctx.Render<MudDialogProvider>();
        var deleteButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        deleteButton.Find("button").Click();

        provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

        var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        confirm.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await provider.InvokeAsync(() => { });
            if (await harness.ClusterRepo.GetByIdAsync(cluster.Id) is null) break;
        }

        Assert.Null(await harness.ClusterRepo.GetByIdAsync(cluster.Id));
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/clusters", navigationManager.Uri);
    }
}
