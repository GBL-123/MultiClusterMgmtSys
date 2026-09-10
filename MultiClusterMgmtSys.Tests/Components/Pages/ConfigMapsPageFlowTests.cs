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

public class ConfigMapsPageFlowTests
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
        ctx.Services.AddScoped<ConfigMapService>();
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

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, IRenderedComponent<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps> Cut)> RenderListAsync()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-flow"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListConfigMaps(new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = "target-cm", NamespaceProperty = "app" },
            Data = new Dictionary<string, string> { ["k"] = "v" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("target-cm"));
        return (ctx, harness, k8s, cut);
    }

    [Fact]
    public async Task Delete_via_tooltip_flow_audits_delete()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var deleteTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "删除");
            deleteTooltip.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

            k8s.SetupDeleteConfigMap("target-cm", "app");

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                if (harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete)) break;
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_dialog_opens_from_page()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var createButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建 ConfigMap"));
            createButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));

            Assert.Contains("创建", provider.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}
