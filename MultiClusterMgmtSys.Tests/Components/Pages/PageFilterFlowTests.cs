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

public class PageFilterFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static Mock<k8s.IKubernetes> SetupCore(BunitHost ctx, ServiceHarness harness)
    {
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.Services.AddScoped(_ => harness.ClusterRepo);
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped<ConfigMapService>();
        ctx.Services.AddScoped<SvcService>();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        return k8s;
    }

    private static void SetupProbe(Mock<k8s.IKubernetes> k8s)
    {
        k8s.SetupListNodes(new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });
        k8s.SetupGetVersion("v1.30.2");
    }

    [Fact]
    public async Task Svcs_page_query_filters_and_reset_restores()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-filter"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListServices(
            new V1Service
            {
                Metadata = new V1ObjectMeta { Name = "web-alpha", NamespaceProperty = "app" },
                Spec = new V1ServiceSpec { Type = "ClusterIP", ClusterIP = "10.96.0.1" }
            },
            new V1Service
            {
                Metadata = new V1ObjectMeta { Name = "api-beta", NamespaceProperty = "app" },
                Spec = new V1ServiceSpec { Type = "ClusterIP", ClusterIP = "10.96.0.2" }
            });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Pages.Svcs>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("web-alpha"));

        var nameField = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "名称");
        await cut.InvokeAsync(async () => await nameField.Instance.ValueChanged!.InvokeAsync("web"));

        var query = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(async () => await query.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => !cut.Markup.Contains("api-beta"), TimeSpan.FromSeconds(5));

        var reset = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(async () => await reset.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => cut.Markup.Contains("api-beta"), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Svcs_page_delete_flow_audits_and_reloads()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-delete"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListServices(new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "doomed-svc", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { Type = "ClusterIP", ClusterIP = "10.96.0.3" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Pages.Svcs>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("doomed-svc"));

        var provider = ctx.Render<MudDialogProvider>();
        var deleteTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "删除");
        deleteTooltip.Find("button").Click();

        provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

        k8s.SetupDeleteService("doomed-svc", "app");

        var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        confirm.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await provider.InvokeAsync(() => { });
            if (harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Service && a.Action == AuditAction.Delete)) break;
        }

        Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Service && a.Action == AuditAction.Delete));
    }

    [Fact]
    public async Task ConfigMaps_page_query_filters_and_reset_restores()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-filter"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListConfigMaps(
            new V1ConfigMap
            {
                Metadata = new V1ObjectMeta { Name = "alpha-cm", NamespaceProperty = "app" },
                Data = new Dictionary<string, string> { ["k"] = "v" }
            },
            new V1ConfigMap
            {
                Metadata = new V1ObjectMeta { Name = "beta-cm", NamespaceProperty = "app" },
                Data = new Dictionary<string, string> { ["k"] = "v" }
            });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("alpha-cm"));

        var nameField = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "名称");
        await cut.InvokeAsync(async () => await nameField.Instance.ValueChanged!.InvokeAsync("alpha"));

        var query = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(async () => await query.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => !cut.Markup.Contains("beta-cm"), TimeSpan.FromSeconds(5));

        var reset = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(async () => await reset.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => cut.Markup.Contains("beta-cm"), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Workloads_page_query_filters_and_reset_restores()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("wl-filter"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListDeployments(
            new V1Deployment
            {
                Metadata = new V1ObjectMeta { Name = "dep-alpha", NamespaceProperty = "app" },
                Spec = new V1DeploymentSpec { Replicas = 1 },
                Status = new V1DeploymentStatus { ReadyReplicas = 1, UpdatedReplicas = 1, ObservedGeneration = 1 }
            },
            new V1Deployment
            {
                Metadata = new V1ObjectMeta { Name = "dep-beta", NamespaceProperty = "app" },
                Spec = new V1DeploymentSpec { Replicas = 1 },
                Status = new V1DeploymentStatus { ReadyReplicas = 1, UpdatedReplicas = 1, ObservedGeneration = 1 }
            });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.Deployments>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("dep-alpha"));

        var nameField = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "名称");
        await cut.InvokeAsync(async () => await nameField.Instance.ValueChanged!.InvokeAsync("alpha"));

        var query = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(async () => await query.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => !cut.Markup.Contains("dep-beta"), TimeSpan.FromSeconds(5));

        var reset = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(async () => await reset.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => cut.Markup.Contains("dep-beta"), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Yaml_edit_page_invalid_yaml_save_stays_and_no_audit()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("yaml-invalid"));
        SetupProbe(k8s);
        k8s.SetupReadDeployment("web", "app", new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1DeploymentSpec { Replicas = 1 }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.DeploymentYamlEdit>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));
        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"));

        var textarea = cut.Find(".yaml-textarea");
        await textarea.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "{ broken yaml" });

        var save = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        await cut.InvokeAsync(async () => await save.Instance.OnClick.InvokeAsync());

        for (var i = 0; i < 5; i++)
        {
            await cut.InvokeAsync(() => { });
        }

        Assert.DoesNotContain(harness.Db.AuditLogs, a => a.Action == AuditAction.Update);
        Assert.Contains("yaml-textarea", cut.Markup);
    }

    [Fact]
    public async Task Node_ip_notes_dialog_submit_audits()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ip-notes"));
        ctx.Services.AddScoped<ClusterNodeService>();

        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Nodes.Shared.NodeIpNotesDialog>(
                "节点 IP 备注",
                new DialogParameters
                {
                    { "ClusterId", cluster.Id },
                    { "NodeName", "n1" },
                    {
                        "Addresses",
                        new List<MultiClusterMgmtSys.ViewModels.NodeAddressViewModel>
                        {
                            new() { Type = "InternalIP", Address = "10.0.0.1" }
                        }
                    }
                });

        provider.WaitForState(() => provider.Markup.Contains("10.0.0.1"));

        var noteField = provider.FindComponents<MudTextField<string>>().First();
        await provider.InvokeAsync(async () => await noteField.Instance.ValueChanged!.InvokeAsync("管理口"));

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        submit.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await provider.InvokeAsync(() => { });
            if (harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Node)) break;
        }

        Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Node && a.Action == AuditAction.Update));
        var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
        Assert.Single(reloaded!.NodeIpRemarks);
        Assert.Equal("管理口", reloaded.NodeIpRemarks.First().Note);
    }
}
