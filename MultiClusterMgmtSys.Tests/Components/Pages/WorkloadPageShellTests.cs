using Bunit;
using k8s;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class WorkloadPageShellTests
{
    private static void AuthorizeAdmin(BunitContext ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Deployments_page_shows_hint_when_no_cluster_selected()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        ctx.AddWorkloadServices(new Mock<k8s.IKubernetes>(), harness);

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.Deployments>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Deployments_with_offline_cluster_shows_unreachable_card()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);
        var added = await harness.ClusterRepo.AddAsync(TestData.NewCluster("offline-src", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.Deployments>(
            parameters => parameters.Add(p => p.ClusterId, added.Id));

        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));
    }

    [Fact]
    public async Task Deployments_page_renders_table_after_k8s_load()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);

        var config = new KubernetesClientConfiguration { Host = "https://x" };
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        k8s.SetupListNamespaces("app");
        k8s.SetupListDeployments(new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus { ReadyReplicas = 3, UpdatedReplicas = 3, ObservedGeneration = 1 }
        });
        k8s.SetupListNodes(new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });
        k8s.SetupGetVersion("v1.30.2");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.Deployments>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("web"));

        Assert.Contains("部署管理", cut.Markup);
        Assert.Contains("就绪", cut.Markup);
    }

    [Theory]
    [InlineData(typeof(MultiClusterMgmtSys.Components.Workloads.Pages.StatefulSets), "有状态应用")]
    [InlineData(typeof(MultiClusterMgmtSys.Components.Workloads.Pages.DaemonSets), "守护进程")]
    [InlineData(typeof(MultiClusterMgmtSys.Components.Workloads.Pages.ReplicaSets), "副本集")]
    public async Task Thin_workload_list_pages_render_with_kind_title(Type pageType, string expectedTitle)
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        ctx.AddWorkloadServices(new Mock<k8s.IKubernetes>(), harness);

        var cut = ctx.Renderer.RenderFragment(builder =>
        {
            builder.OpenComponent(0, pageType);
            builder.CloseComponent();
        });

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Deployment_detail_page_renders_not_found_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-src", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Workloads.Pages.DeploymentDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "missing"));

        cut.WaitForState(() => cut.Markup.Contains("不存在或已被删除") || cut.Markup.Contains("集群不可达"));
    }
}
