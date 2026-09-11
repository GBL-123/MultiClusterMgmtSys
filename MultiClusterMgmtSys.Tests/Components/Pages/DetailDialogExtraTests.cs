using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
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
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class DetailDialogExtraTests
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
    public async Task ConfigMap_detail_page_renders_data_and_yaml_tabs()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-detail-src"));
        k8s.SetupReadConfigMap("detail-cm", "app", new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = "detail-cm", NamespaceProperty = "app", Uid = "uid-cm" },
            Data = new Dictionary<string, string> { ["db.host"] = "postgres" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMapDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "detail-cm"));

        cut.WaitForState(() => cut.Markup.Contains("db.host"));

        Assert.Contains("postgres", cut.Markup);
        Assert.Contains("YAML", cut.Markup);
    }

    [Fact]
    public async Task Svc_detail_page_renders_ports_and_endpoint_tab()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-detail2"));
        k8s.SetupReadService("detail-svc", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "detail-svc", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec
            {
                Type = "NodePort",
                ClusterIP = "10.96.0.9",
                Ports = [new V1ServicePort { Port = 80, NodePort = 30080 }]
            }
        });
        k8s.SetupListEndpointSlices("app", SvcMappingExtensions.EndpointSliceServiceLabel + "=detail-svc");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Svcs.Pages.SvcDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "detail-svc"));

        cut.WaitForState(() => cut.Markup.Contains("detail-svc"));

        var tabs = cut.FindAll(".mud-tab");
        tabs[1].Click();
        cut.WaitForState(() => cut.Markup.Contains("30080"), TimeSpan.FromSeconds(5));
        Assert.Contains("30080", cut.Markup);
    }

    [Fact]
    public async Task Create_svc_dialog_submit_creates_service()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-create"));
        ctx.Services.AddScoped(_ => TestHttpContext.For("admin", "Admin").Object);
        var yamlTemplate = new Mock<IYamlTemplateService>();
        yamlTemplate.Setup(s => s.GetTemplateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("apiVersion: v1\nkind: Service\nmetadata:\n  name: new-svc\n  namespace: app\nspec:\n  type: ClusterIP\n  ports:\n    - port: 80\n");
        ctx.Services.AddSingleton<IYamlTemplateService>(yamlTemplate.Object);

        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Svcs.Shared.CreateSvcDialog>(
                "新建 Service",
                new DialogParameters { { "ClusterId", cluster.Id } });

        provider.WaitForState(() => provider.Markup.Contains("创建"), TimeSpan.FromSeconds(5));

        k8s.SetupCreateService("app");
        var yamlField = provider.FindComponents<MudTextField<string>>().First();
        var yaml = """
            apiVersion: v1
            kind: Service
            metadata:
              name: new-svc
              namespace: app
            spec:
              type: ClusterIP
              ports:
                - port: 80
            """;
        await provider.InvokeAsync(async () => await yamlField.Instance.ValueChanged!.InvokeAsync(yaml));

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("创建"));
        submit.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await provider.InvokeAsync(() => { });
            if (harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Service && a.Action == AuditAction.Create)) break;
        }

        Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Service && a.Action == AuditAction.Create));
    }

    [Fact]
    public async Task Workload_list_row_click_navigates_to_detail()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = SetupCore(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("nav-src"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListDeployments(new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "nav-target", NamespaceProperty = "app" },
            Spec = new V1DeploymentSpec { Replicas = 1 },
            Status = new V1DeploymentStatus { ReadyReplicas = 1, UpdatedReplicas = 1, ObservedGeneration = 1 }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.Deployments>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("nav-target"));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("nav-target")).Click();

        await cut.InvokeAsync(() => { });

        var navigationManager = ctx.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        Assert.Contains("nav-target", navigationManager.Uri);
        Assert.Contains("/app/", navigationManager.Uri);
    }

    [Fact]
    public async Task Cluster_sync_background_execute_loop_refreshes_and_stops()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("bg-sync", status: ClusterStatus.Online));
        await harness.Db.AppSettings.AddAsync(TestData.NewSetting("ClusterSync:Enabled", "true"));
        await harness.Db.AppSettings.AddAsync(TestData.NewSetting("ClusterSync:IntervalMinutes", "1"));
        await harness.Db.SaveChangesAsync(CancellationToken.None);

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddSingleton(harness.Db);
        services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build());
        services.AddScoped(_ => new MultiClusterMgmtSys.Data.Repositories.AppSettingRepository(harness.Db));
        services.AddScoped(_ => harness.ClusterRepo);
        services.AddScoped(_ => harness.Audit);
        services.AddScoped(_ => TestHttpContext.For("admin", "Admin").Object);
        services.AddScoped<ClusterNodeService>();
        services.AddScoped<ClusterService>();
        services.AddScoped<ClusterSyncSettingService>();
        services.AddScoped<ClusterSyncBackgroundService>();
        var provider = services.BuildServiceProvider();

        var background = provider.GetRequiredService<ClusterSyncBackgroundService>();
        using var cts = new CancellationTokenSource();
        await background.StartAsync(cts.Token);

        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(50);
            var reloaded = await harness.ClusterRepo.GetByIdAsync(cluster.Id);
            if (reloaded!.Status == ClusterStatus.Offline) break;
        }

        Assert.Equal(ClusterStatus.Offline, (await harness.ClusterRepo.GetByIdAsync(cluster.Id))!.Status);

        cts.Cancel();
        await background.StopAsync(CancellationToken.None);
    }
}

