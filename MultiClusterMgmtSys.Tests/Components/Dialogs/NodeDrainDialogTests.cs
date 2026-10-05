using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class NodeDrainDialogTests
{
    private static readonly Microsoft.AspNetCore.Components.RendererInfo RendererInfo = new("bunit", true);

    private static V1Node PlainNode(string name) => new()
    {
        Metadata = new V1ObjectMeta { Name = name }
    };

    private static V1Pod Pod(string ns, string name, string? ownerKind)
    {
        var meta = new V1ObjectMeta { Name = name, NamespaceProperty = ns };
        if (ownerKind is not null)
        {
            meta.OwnerReferences = [new V1OwnerReference { Kind = ownerKind, Name = $"{ownerKind}-{name}", ApiVersion = "apps/v1", Uid = $"uid-{name}" }];
        }

        return new V1Pod { Metadata = meta };
    }

    private static async Task<(Mock<IKubernetes> K8s, ClusterInfo Cluster)> WireAsync(
        BunitHost ctx,
        ServiceHarness harness,
        string clusterName,
        string actor = "admin",
        string role = "Admin")
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized(actor);
        auth.SetRoles(role);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.Services.AddSingleton(new ClusterNodeService(
            harness.ClusterRepo,
            harness.Audit,
            NullLogger<ClusterNodeService>.Instance,
            K8sMocks.Cache(k8s),
            TestHttpContext.For(actor, role).Object));
        ctx.Renderer.SetRendererInfo(RendererInfo);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster(clusterName));
        return (k8s, cluster);
    }

    private static async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(
        BunitHost ctx,
        int clusterId,
        string nodeName = "node-a")
    {
        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Web.Components.Nodes.Shared.NodeDrainDialog>(
                $"排空节点: {nodeName}",
                new DialogParameters
                {
                    { "ClusterId", clusterId },
                    { "NodeName", nodeName }
                });
        return (provider, reference);
    }

    [Fact]
    public async Task Preflight_lists_migratable_daemonset_and_bare_pods()
    {
        await using var ctx = new BunitHost();
        var harness = ctx.AddClusterStack();

        var (k8s, cluster) = await WireAsync(ctx, harness, "drain-list");
        k8s.SetupListPods(
            Pod("app", "web-app", "ReplicaSet"),
            Pod("kube-system", "logs-agent", "DaemonSet"),
            Pod("default", "bare-one", null));
        var (provider, _) = await ShowAsync(ctx, cluster.Id);

        try
        {
            provider.WaitForState(() => provider.Markup.Contains("迁移清单"), TimeSpan.FromSeconds(5));

            Assert.Contains("web-app", provider.Markup);
            Assert.Contains("ReplicaSet", provider.Markup);
            Assert.Contains("跳过 DaemonSet Pod 1 个", provider.Markup);
            Assert.Contains("kube-system/logs-agent", provider.Markup);
            Assert.Contains("个裸 Pod", provider.Markup);
            Assert.Contains("default/bare-one", provider.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Drain_executes_and_closes_with_report()
    {
        await using var ctx = new BunitHost();
        var harness = ctx.AddClusterStack();

        var (k8s, cluster) = await WireAsync(ctx, harness, "drain-run");
        k8s.SetupListPods(Pod("app", "web-app", "ReplicaSet"), Pod("kube-system", "logs-agent", "DaemonSet"));
        k8s.SetupReadNode("node-a", PlainNode("node-a"));
        k8s.SetupPatchNode("node-a", PlainNode("node-a"));
        k8s.SetupEvictPod("app", "web-app");
        var (provider, reference) = await ShowAsync(ctx, cluster.Id);

        try
        {
            provider.WaitForState(() => provider.Markup.Contains("迁移清单"), TimeSpan.FromSeconds(5));

            var drain = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("开始排空"));
            await provider.InvokeAsync(async () => await drain.Instance.OnClick!.InvokeAsync());

            provider.WaitForState(() => provider.Markup.Contains("排空完成"), TimeSpan.FromSeconds(5));

            Assert.Contains("成功 1 / 跳过 1 / 阻塞 0", provider.Markup);
            K8sMocks.VerifyNodePatched(k8s, "node-a", Times.Once());
            K8sMocks.VerifyPodEvicted(k8s, "app", "web-app", Times.Once());

            var close = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("关闭"));
            await provider.InvokeAsync(async () => await close.Instance.OnClick!.InvokeAsync());

            var result = await reference.Result;
            Assert.False(result.Canceled);
            var report = Assert.IsType<NodeDrainReportViewModel>(result.Data);
            Assert.Equal(1, report.Evicted);
            Assert.Equal(1, report.Skipped);
            Assert.Equal(0, report.Blocked);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Preflight_failure_shows_message_and_keeps_dialog_open()
    {
        await using var ctx = new BunitHost();
        var harness = ctx.AddClusterStack();

        var (k8s, cluster) = await WireAsync(ctx, harness, "drain-fail");
        k8s.SetupListPodsThrows(K8sMocks.K8sError(404, "pods not found"));
        var (provider, reference) = await ShowAsync(ctx, cluster.Id);

        try
        {
            provider.WaitForState(() => provider.Markup.Contains("排空预检失败"), TimeSpan.FromSeconds(5));

            Assert.DoesNotContain("迁移清单", provider.Markup);
            Assert.False(reference.Result.IsCompleted);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Zero_migratable_pods_drains_with_skip_only()
    {
        await using var ctx = new BunitHost();
        var harness = ctx.AddClusterStack();

        var (k8s, cluster) = await WireAsync(ctx, harness, "drain-zero");
        k8s.SetupListPods(Pod("kube-system", "logs-agent", "DaemonSet"));
        k8s.SetupReadNode("node-a", PlainNode("node-a"));
        k8s.SetupPatchNode("node-a", PlainNode("node-a"));
        var (provider, reference) = await ShowAsync(ctx, cluster.Id);

        try
        {
            provider.WaitForState(() => provider.Markup.Contains("没有需要迁移的 Pod"), TimeSpan.FromSeconds(5));

            var drain = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("开始排空"));
            await provider.InvokeAsync(async () => await drain.Instance.OnClick!.InvokeAsync());

            provider.WaitForState(() => provider.Markup.Contains("排空完成"), TimeSpan.FromSeconds(5));

            Assert.Contains("成功 0 / 跳过 1 / 阻塞 0", provider.Markup);

            var close = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("关闭"));
            await provider.InvokeAsync(async () => await close.Instance.OnClick!.InvokeAsync());

            var result = await reference.Result;
            var report = Assert.IsType<NodeDrainReportViewModel>(result.Data);
            Assert.Equal(1, report.Skipped);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Member_cannot_start_drain_preflight()
    {
        await using var ctx = new BunitHost();
        var harness = ctx.AddClusterStack();

        var (k8s, cluster) = await WireAsync(ctx, harness, "drain-member", actor: "member", role: "Member");
        var (provider, reference) = await ShowAsync(ctx, cluster.Id);

        try
        {
            provider.WaitForState(() => provider.Markup.Contains("排空预检失败"), TimeSpan.FromSeconds(5));

            Assert.Contains("节点维护操作仅管理员可用", provider.Markup);
            Assert.False(reference.Result.IsCompleted);
            _ = k8s;
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}
