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

public class ConfigMapsPageTests
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

    [Fact]
    public async Task Configmaps_page_lists_configmaps_for_selected_cluster()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cm-page"));
        SetupProbe(k8s);

        k8s.SetupListNamespaces("app");
        k8s.SetupListConfigMaps(new V1ConfigMap
        {
            Metadata = new V1ObjectMeta { Name = "page-cm", NamespaceProperty = "app" },
            Data = new Dictionary<string, string> { ["k1"] = "v1" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("page-cm"));

        Assert.Contains("配置管理", cut.Markup);
        Assert.Contains("k1, k2".Length > 0 ? "k1" : "k1", cut.Markup);
    }

    [Fact]
    public async Task Configmaps_page_offline_cluster_shows_unreachable()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("cm-offline", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));
    }

    [Fact]
    public async Task Configmaps_page_without_cluster_shows_hint()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        Setup(ctx, harness);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMaps>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
    }
}

public class NodesPageTests
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
        ctx.Services.AddScoped<ClusterNodeService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        return k8s;
    }

    private static V1Node RichNode(string name, bool ready)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = ready ? "True" : "False" }],
                Addresses = [new V1NodeAddress { Type = "InternalIP", Address = "10.0.0.9" }],
                NodeInfo = new V1NodeSystemInfo { KubeletVersion = "v1.30.2" }
            }
        };

    [Fact]
    public async Task Nodes_page_lists_nodes_for_selected_cluster()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("node-page"));

        k8s.SetupListNodes(RichNode("n1", true), RichNode("n2", false));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Pages.Nodes>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("n1"));

        Assert.Contains("节点管理", cut.Markup);
        Assert.Contains("v1.30.2", cut.Markup);
    }

    [Fact]
    public async Task Node_detail_page_renders_full_view()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("node-detail-src"));

        k8s.SetupReadNode("n1", new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions =
                [
                    new V1NodeCondition { Type = "Ready", Status = "True", Reason = "KubeletReady" },
                    new V1NodeCondition { Type = "MemoryPressure", Status = "False" }
                ],
                Addresses =
                [
                    new V1NodeAddress { Type = "InternalIP", Address = "10.0.0.9" },
                    new V1NodeAddress { Type = "Hostname", Address = "n1-host" }
                ],
                Capacity = new Dictionary<string, ResourceQuantity> { ["cpu"] = new ResourceQuantity("4") },
                Allocatable = new Dictionary<string, ResourceQuantity> { ["cpu"] = new ResourceQuantity("3800m") },
                NodeInfo = new V1NodeSystemInfo { Architecture = "amd64", KubeletVersion = "v1.30.2" },
                Phase = "Running"
            },
            Spec = new V1NodeSpec
            {
                Unschedulable = false,
                PodCIDR = "10.244.1.0/24",
                Taints = [new V1Taint { Key = "k", Value = "v", Effect = "NoSchedule" }]
            }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Pages.NodeDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.NodeName, "n1"));

        cut.WaitForState(() => cut.Markup.Contains("n1"));

        Assert.Contains("10.244.1.0/24", cut.Markup);
        Assert.Contains("NoSchedule", cut.Markup);

        cut.FindAll(".mud-tab")[1].Click();
        cut.WaitForState(() => cut.Markup.Contains("3800m"));
        Assert.Contains("3800m", cut.Markup);

        cut.FindAll(".mud-tab")[2].Click();
        cut.WaitForState(() => cut.Markup.Contains("KubeletReady"));
        Assert.Contains("KubeletReady", cut.Markup);

        cut.FindAll(".mud-tab")[4].Click();
        cut.WaitForState(() => cut.Markup.Contains("amd64"));
        Assert.Contains("amd64", cut.Markup);
    }

    [Fact]
    public async Task Node_detail_page_offline_cluster_short_circuits()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = Setup(ctx, harness);
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("node-detail-off", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Nodes.Pages.NodeDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.NodeName, "n1"));

        cut.WaitForState(() => cut.Markup.Contains("节点不存在或已删除") || cut.Markup.Contains("不可达"));
    }
}
