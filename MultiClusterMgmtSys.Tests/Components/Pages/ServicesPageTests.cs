using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class ServicesPageTests
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

    private static V1Service NewService(string name, string ns)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                ClusterIP = "10.96.0.10",
                Ports = [new V1ServicePort { Port = 80 }]
            }
        };

    [Fact]
    public async Task Services_page_shows_hint_when_no_cluster()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        Setup(ctx, harness);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.Services>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Services_page_offline_shows_unreachable()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("svc-offline", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.Services>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));
    }

    [Fact]
    public async Task Services_page_lists_services()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-page-src"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListServices(NewService("page-svc", "app"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.Services>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("page-svc"));

        Assert.Contains("10.96.0.10", cut.Markup);
    }

    [Fact]
    public async Task Service_detail_page_renders_ports_and_endpoints()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-detail-src"));

        k8s.SetupReadService("web", "app", NewService("web", "app"));
        k8s.SetupListEndpointSlices("app", SvcMappingExtensions.EndpointSliceServiceLabel + "=web");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.ServiceDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.Markup.Contains("web"));

        var tabHeaders = cut.FindAll(".mud-tab");
        tabHeaders[2].Click();
        cut.WaitForState(() => cut.Markup.Contains("暂无后端"));
        Assert.Contains("暂无后端", cut.Markup);
        Assert.Contains("YAML", cut.Markup);
    }

    [Fact]
    public async Task Service_yaml_edit_page_loads_yaml()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-yaml-src"));

        k8s.SetupReadService("web", "app", NewService("web", "app"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.EditServiceYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea") || cut.Markup.Contains("编辑 YAML"));

        Assert.Contains("保存", cut.Markup);
    }

    [Fact]
    public async Task Services_page_without_cluster_hint_and_offline_covered()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("svc-detail-off", status: ClusterStatus.Offline));

        var detail = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.ServiceDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        detail.WaitForState(() => detail.Markup.Contains("不存在或已被删除") || detail.Markup.Contains("不可达"));

        var yamlEdit = ctx.Render<MultiClusterMgmtSys.Components.Services.Pages.EditServiceYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        yamlEdit.WaitForState(() => yamlEdit.Markup.Contains("不存在或已被删除") || yamlEdit.Markup.Contains("不可达"));
    }
}

