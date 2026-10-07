using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using MultiClusterMgmtSys.Web.Components.Helm.Shared;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class FleetDeployHelmDialogTests
{
    [Fact]
    public async Task Form_renders_clusters_with_status_badges()
    {
        await using var ctx = new BunitHost();
        var (harness, _, _) = ctx.AddHelmStack(userId: 7);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod", status: ClusterStatus.Online));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging", status: ClusterStatus.Offline));
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);

        Assert.Contains("目标集群", provider.Markup);
        Assert.Contains("prod", provider.Markup);
        Assert.Contains("staging", provider.Markup);
        Assert.Contains("在线", provider.Markup);
        Assert.Contains("离线", provider.Markup);
        Assert.Contains("yaml-textarea", provider.Markup);
    }

    [Fact]
    public async Task Upload_prefills_release_name_and_values()
    {
        await using var ctx = new BunitHost();
        var (harness, _, _) = ctx.AddHelmStack(userId: 7);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);
        await UploadChartAsync(ctx, provider);

        Assert.Contains("demo 1.0.0", provider.Markup);
        var fields = provider.FindComponents<MudTextField<string>>();
        Assert.Equal(2, fields.Count);
        Assert.Equal("demo", fields[0].Instance.Value);
        Assert.Contains("replicaCount: 1", provider.Markup);
    }

    [Fact]
    public async Task Submit_without_package_is_blocked_without_helm_calls()
    {
        await using var ctx = new BunitHost();
        var (harness, runner, _) = ctx.AddHelmStack(userId: 7);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);
        await ClickStartAsync(provider);

        Assert.Empty(runner.Invocations);
        Assert.DoesNotContain("正在下发到", provider.Markup);
    }

    [Fact]
    public async Task Submit_without_cluster_selection_is_blocked()
    {
        await using var ctx = new BunitHost();
        var (harness, runner, _) = ctx.AddHelmStack(userId: 7);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);
        await UploadChartAsync(ctx, provider);
        await FillFieldsAsync(provider, releaseName: "demo", namespaceName: "web");
        await ClickStartAsync(provider);

        Assert.Empty(runner.Invocations);
        Assert.DoesNotContain("批量下发完成", provider.Markup);
    }

    [Fact]
    public async Task Submit_with_invalid_namespace_is_blocked()
    {
        await using var ctx = new BunitHost();
        var (harness, runner, _) = ctx.AddHelmStack(userId: 7);
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);
        await UploadChartAsync(ctx, provider);
        await FillFieldsAsync(provider, releaseName: "demo", namespaceName: "Web");
        await ClickStartAsync(provider);

        Assert.Empty(runner.Invocations);
        Assert.DoesNotContain("批量下发完成", provider.Markup);
    }

    [Fact]
    public async Task Submit_dispatches_and_shows_result_rows_and_summary()
    {
        await using var ctx = new BunitHost();
        var (harness, runner, _) = ctx.AddHelmStack(userId: 7);
        var first = await harness.ClusterRepo.AddAsync(TestData.NewCluster("prod"));
        var second = await harness.ClusterRepo.AddAsync(TestData.NewCluster("staging"));
        runner.Handler = invocation => invocation.Arguments[0] == "status"
            ? FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError)
            : FakeHelmCliRunner.Succeeded();
        var snackbarMock = new Mock<ISnackbar>();
        var messages = new List<string>();
        snackbarMock.Setup(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()))
            .Callback<string, Severity, Action<SnackbarOptions>, string>((message, _, _, _) => messages.Add(message))
            .Returns((Snackbar?)null);
        ctx.Services.AddSingleton<ISnackbar>(snackbarMock.Object);
        var provider = ctx.Render<MudDialogProvider>();

        await OpenAsync(ctx, provider);
        await UploadChartAsync(ctx, provider);
        await FillFieldsAsync(provider, releaseName: "demo", namespaceName: "web");
        await ToggleClusterCheckboxesAsync(provider, 2, 3);
        await ClickStartAsync(provider);

        await WaitForDoneAsync(provider);
        Assert.Contains("批量下发完成", provider.Markup);
        Assert.Contains("成功 2 / 失败 0", provider.Markup);
        Assert.Contains("prod", provider.Markup);
        Assert.Contains("staging", provider.Markup);
        Assert.Contains("安装", provider.Markup);
        Assert.Equal(2, runner.Invocations.Count(invocation => invocation.Arguments[0] == "install"));
        Assert.NotNull(await harness.OwnershipRepo.GetAsync(first.Id, "web", "demo"));
        Assert.NotNull(await harness.OwnershipRepo.GetAsync(second.Id, "web", "demo"));
    }

    private static async Task OpenAsync(BunitContext ctx, IRenderedComponent<MudDialogProvider> provider)
        => await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<FleetDeployHelmDialog>("批量下发 Chart 包");

    private static async Task UploadChartAsync(BunitContext ctx, IRenderedComponent<MudDialogProvider> provider)
    {
        var input = provider.FindComponent<InputFile>();
        var package = HelmTestPackages.Create(
            ("demo/Chart.yaml", "apiVersion: v2\nname: demo\nversion: 1.0.0\nappVersion: \"1.2.3\""),
            ("demo/values.yaml", "replicaCount: 1"));
        var file = new FakeBrowserFile("demo-1.0.0.tgz", package);
        await provider.InvokeAsync(async () => await input.Instance.OnChange.InvokeAsync(
            new InputFileChangeEventArgs([file])));
    }

    private static async Task FillFieldsAsync(IRenderedComponent<MudDialogProvider> provider, string releaseName, string namespaceName)
    {
        var fields = provider.FindComponents<MudTextField<string>>();
        await provider.InvokeAsync(async () => await fields[0].Instance.ValueChanged!.InvokeAsync(releaseName));
        await provider.InvokeAsync(async () => await fields[1].Instance.ValueChanged!.InvokeAsync(namespaceName));
    }

    private static async Task ToggleClusterCheckboxesAsync(IRenderedComponent<MudDialogProvider> provider, params int[] indexes)
    {
        var checkboxes = provider.FindComponents<MudCheckBox<bool>>();
        // 前两个为「自动创建命名空间」与「--wait」选项,其后为目标集群复选框。
        foreach (var index in indexes)
        {
            await provider.InvokeAsync(async () => await checkboxes[index].Instance.ValueChanged!.InvokeAsync(true));
        }
    }

    private static async Task ClickStartAsync(IRenderedComponent<MudDialogProvider> provider)
    {
        var buttons = provider.FindComponents<MudButton>()
            .First(button => button.Markup.Contains("开始下发"));
        await provider.InvokeAsync(async () => await buttons.Instance.OnClick.InvokeAsync());
        await provider.InvokeAsync(() => { });
    }

    private static async Task WaitForDoneAsync(IRenderedComponent<MudDialogProvider> provider)
    {
        for (var attempt = 0; attempt < 100 && !provider.Markup.Contains("批量下发完成"); attempt++)
        {
            await provider.InvokeAsync(() => { });
            await Task.Delay(20, Xunit.TestContext.Current.CancellationToken);
        }
        Assert.Contains("批量下发完成", provider.Markup);
    }

    private sealed class FakeBrowserFile(string name, byte[] content) : IBrowserFile
    {
        public string Id => name;

        public string Name => name;

        public long Size => content.LongLength;

        public string ContentType => "application/gzip";

        public DateTimeOffset LastModified { get; } = new(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);

        public Stream OpenReadStream(long maxAllowedSize = 5242880, CancellationToken cancellationToken = default)
            => new MemoryStream(content);
    }
}
