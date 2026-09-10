using Bunit;
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

public class WorkloadDetailFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static Mock<k8s.IKubernetes> Setup(BunitHost ctx, ServiceHarness harness)
    {
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.Services.AddScoped(_ => harness.ClusterRepo);
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        return k8s;
    }

    private static V1Deployment HealthyDeployment(string name)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = "app", Uid = "uid-flow" },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus { ReadyReplicas = 3, UpdatedReplicas = 3, ObservedGeneration = 1 }
        };

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, int ClusterId, IRenderedComponent<MultiClusterMgmtSys.Components.Workloads.Pages.DeploymentDetail> Cut)> RenderDetailAsync()
    {
        var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-flow"));
        k8s.SetupReadDeployment("web", "app", HealthyDeployment("web"));
        k8s.SetupListEndpointSlices("app");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.DeploymentDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.Markup.Contains("uid-flow"));
        return (ctx, harness, k8s, cluster.Id, cut);
    }

    [Fact]
    public async Task Scale_via_toolbar_dialog_updates_replicas_and_audits()
    {
        var (ctx, harness, k8s, clusterId, cut) = await RenderDetailAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var scaleButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("扩缩容"));
            scaleButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认"), TimeSpan.FromSeconds(5));

            k8s.SetupReadDeploymentScale("web", "app", currentReplicas: 3);
            k8s.SetupReplaceDeploymentScale("web", "app");

            var plusButton = provider.FindComponents<MudIconButton>().Last();
            await provider.InvokeAsync(async () => await plusButton.Instance.OnClick.InvokeAsync());

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("确认"));
            await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

            for (var i = 0; i < 10 && !harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Scale); i++)
            {
                await provider.InvokeAsync(() => { });
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Scale));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Restart_via_confirm_dialog_audits_restart()
    {
        var (ctx, harness, k8s, clusterId, cut) = await RenderDetailAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var restartButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重启"));
            restartButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("Pod 会逐步替换") || provider.Markup.Contains("确认"), TimeSpan.FromSeconds(5));

            k8s.SetupPatchDeployment("web", "app");

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10 && !harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Restart); i++)
            {
                await provider.InvokeAsync(() => { });
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Restart));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Delete_via_confirm_dialog_audits_delete()
    {
        var (ctx, harness, k8s, clusterId, cut) = await RenderDetailAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var deleteButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            deleteButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

            k8s.SetupDeleteDeployment("web", "app");

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10 && !harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete); i++)
            {
                await provider.InvokeAsync(() => { });
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}
