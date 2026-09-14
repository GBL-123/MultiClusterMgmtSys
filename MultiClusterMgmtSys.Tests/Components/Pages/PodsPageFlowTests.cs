using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Pods.Pages;
using MultiClusterMgmtSys.Components.Pods.Shared;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class PodsPageFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static void SetupProbe(Mock<IKubernetes> k8s)
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
    public async Task Pods_page_renders_rows_after_k8s_load()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("pod-page", status: ClusterStatus.Online));
        SetupProbe(k8s);
        k8s.SetupListPods(
            K8sMocks.NewPod("web-1", "app", nodeName: "node-a", podIp: "10.1.1.1"),
            K8sMocks.NewPod("db-0", "data", phase: "Running", customize: p =>
                p.Status.ContainerStatuses =
                [
                    new V1ContainerStatus
                    {
                        Name = "db",
                        Ready = false,
                        RestartCount = 3,
                        Image = "pg:16",
                        State = K8sMocks.PodStateWaiting("CrashLoopBackOff")
                    }
                ]));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("web-1"));

        Assert.Contains("崩溃循环", cut.Markup);
        Assert.Contains("CrashLoopBackOff", cut.Markup);
        Assert.Contains("运行中", cut.Markup);
        Assert.Contains("node-a", cut.Markup);
    }

    [Fact]
    public async Task Pods_page_filter_narrows_rows_without_new_k8s_calls()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("pod-filter", status: ClusterStatus.Online));
        SetupProbe(k8s);
        k8s.SetupListPods(
            K8sMocks.NewPod("web-1", "app"),
            K8sMocks.NewPod("db-0", "data"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("web-1"));

        var bar = cut.FindComponent<PodsFilterBar>();
        await cut.InvokeAsync(() => bar.Instance.KeywordChanged.InvokeAsync("db-"));
        await cut.InvokeAsync(() => { });

        Assert.Contains("db-0", cut.Markup);
        Assert.DoesNotContain("web-1", cut.Markup);
        k8s.Verify(x => x.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pods_page_reset_restores_all_rows()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("pod-reset", status: ClusterStatus.Online));
        SetupProbe(k8s);
        k8s.SetupListPods(
            K8sMocks.NewPod("web-1", "app"),
            K8sMocks.NewPod("db-0", "data"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("web-1"));

        var bar = cut.FindComponent<PodsFilterBar>();
        await cut.InvokeAsync(() => bar.Instance.KeywordChanged.InvokeAsync("db-"));
        await cut.InvokeAsync(() => bar.Instance.OnReset.InvokeAsync());
        await cut.InvokeAsync(() => { });

        Assert.Contains("web-1", cut.Markup);
        Assert.Contains("db-0", cut.Markup);
    }

    [Fact]
    public async Task Pods_page_row_name_navigates_to_detail()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("pod-nav", status: ClusterStatus.Online));
        SetupProbe(k8s);
        k8s.SetupListPods(K8sMocks.NewPod("web-1", "app"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("web-1"));

        await cut.InvokeAsync(() => cut.Find("span.link-primary").Click());

        Assert.EndsWith($"/pods/{cluster.Id}/app/web-1", ctx.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Pods_page_offline_cluster_shows_degraded_card()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("pod-offline", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));

        Assert.DoesNotContain("pods-table", cut.Markup);
        k8s.Verify(x => x.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pods_page_without_cluster_shows_selection_hint()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        ctx.AddPodStack();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.Pods>();

        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }
}
