using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class YamlEditPagesTests
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
    public async Task ConfigMap_yaml_edit_page_saves_and_audits()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-edit-src"));
        SetupProbe(k8s);

        k8s.SetupReadConfigMap("cm-a", "app", new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = "cm-a", NamespaceProperty = "app" },
            Data = new Dictionary<string, string> { ["k"] = "v" }
        });
        k8s.SetupReplaceConfigMap("cm-a", "app");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.EditConfigMapYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "cm-a"));

        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"));

        var save = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        await cut.InvokeAsync(async () => await save.Instance.OnClick.InvokeAsync());

        cut.WaitForState(() => harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Update), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task ConfigMap_yaml_edit_page_missing_shows_error_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-edit-miss"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.EditConfigMapYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "ghost"));

        cut.WaitForState(() => cut.Markup.Contains("不存在或已被删除") || cut.Markup.Contains("加载失败"));
    }

    [Fact]
    public async Task Service_yaml_edit_page_saves_and_audits()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-edit-src"));
        SetupProbe(k8s);

        k8s.SetupReadService("web", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { ClusterIP = "10.96.0.10" }
        });
        k8s.SetupReplaceService("web", "app");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.EditServiceYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"));

        var save = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        await cut.InvokeAsync(async () => await save.Instance.OnClick.InvokeAsync());

        cut.WaitForState(() => harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Update), TimeSpan.FromSeconds(10));
    }
}
