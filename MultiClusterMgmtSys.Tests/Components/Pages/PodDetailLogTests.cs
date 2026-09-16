using Bunit;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class PodDetailLogTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static async Task<(
        BunitHost Ctx,
        ServiceHarness Harness,
        Mock<IKubernetes> K8s,
        IRenderedComponent<MultiClusterMgmtSys.Components.Pods.Pages.PodDetail> Cut)> RenderDetailAsync(string podName = "web-1")
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddPodStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("log-cluster", status: ClusterStatus.Online));
        k8s.SetupReadPod(podName, "app", K8sMocks.NewPod(podName, "app"));

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Pods.Pages.PodDetail>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Name, podName));
        cut.WaitForState(() => cut.Markup.Contains(podName));
        return (ctx, harness, k8s, cut);
    }

    private static void VerifyLogRequest(Mock<IKubernetes> k8s, Times times, string? container = null, int? tailLines = null)
        => k8s.Verify(x => x.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(
            It.Is<string>(n => n == "web-1"),
            It.Is<string>(n => n == "app"),
            It.Is<string?>(c => container == null || c == container),
            It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<int?>(), It.IsAny<string?>(),
            It.Is<int?>(t => tailLines == null || t == tailLines),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), times);

    [Fact]
    public async Task Detail_page_does_not_load_logs_on_entry()
    {
        var (ctx, harness, k8s, cut) = await RenderDetailAsync();
        try
        {
            await using var _ = ctx;

            Assert.Contains("概览", cut.Markup);
            VerifyLogRequest(k8s, Times.Never());
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Log_tab_activation_loads_and_shows_content()
    {
        var (ctx, harness, k8s, cut) = await RenderDetailAsync();
        try
        {
            await using var _ = ctx;
            k8s.SetupReadPodLog("web-1", "app", "line-1\nline-2\n");

            await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[2].Click());
            cut.WaitForState(() => cut.Markup.Contains("line-1"));

            Assert.Contains("共 2 行", cut.Markup);
            VerifyLogRequest(k8s, Times.Once(), container: "app", tailLines: 500);
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Log_tail_change_reloads_with_new_parameters()
    {
        var (ctx, harness, k8s, cut) = await RenderDetailAsync();
        try
        {
            await using var _ = ctx;
            k8s.SetupReadPodLog("web-1", "app", "some log");

            await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[2].Click());
            cut.WaitForState(() => cut.Markup.Contains("some log"));

            await cut.InvokeAsync(() => cut.FindComponent<MudSelect<int>>().Instance.ValueChanged.InvokeAsync(1000));
            cut.WaitForState(() =>
            {
                try
                {
                    VerifyLogRequest(k8s, Times.Once(), tailLines: 1000);
                    return true;
                }
                catch (MockException)
                {
                    return false;
                }
            });
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Empty_log_shows_empty_state()
    {
        var (ctx, harness, k8s, cut) = await RenderDetailAsync();
        try
        {
            await using var _ = ctx;
            k8s.SetupReadPodLog("web-1", "app", "");

            await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[2].Click());
            cut.WaitForState(() => cut.Markup.Contains("暂无日志"));
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Log_load_failure_shows_retry_and_recovers()
    {
        var (ctx, harness, k8s, cut) = await RenderDetailAsync();
        try
        {
            await using var _ = ctx;
            k8s.SetupReadPodLogThrows("web-1", "app", K8sMocks.K8sError(500));

            await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[2].Click());
            cut.WaitForState(() => cut.Markup.Contains("加载日志失败"));

            k8s.SetupReadPodLog("web-1", "app", "recovered log");
            await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("重试")).Click());
            cut.WaitForState(() => cut.Markup.Contains("recovered log"));
        }
        finally
        {
            harness.Dispose();
        }
    }
}
