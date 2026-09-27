using Bunit;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class CreateWorkloadDialogTests
{
    private static void RegisterWorkloads(BunitHost ctx, Mock<k8s.IKubernetes> k8s, ServiceHarness harness, string actor = "admin")
    {
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<IClusterRepository>(_ => harness.ClusterRepo);
        ctx.Services.AddScoped(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<MultiClusterMgmtSys.Application.Abstractions.IHelmReleaseOwnershipRepository>(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped<ResourceOwnershipGuard>();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.AddYamlTemplates();
    }

    [Fact]
    public async Task Template_prefilled_per_kind()
    {
        var harness = new ServiceHarness();
        var k8s = new Mock<k8s.IKubernetes>();
        try
        {
            await using var ctx = new BunitHost();
            RegisterWorkloads(ctx, k8s, harness);
            var provider = ctx.Render<MudDialogProvider>();

            var reference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Web.Components.Workloads.Shared.CreateWorkloadDialog>(
                    "新建",
                    new DialogParameters { { "ClusterId", 1 }, { "Kind", WorkloadKind.StatefulSet } });

            provider.WaitForState(() => provider.Markup.Contains("kind: StatefulSet"));
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Submit_creates_workload_and_closes()
    {
        var harness = new ServiceHarness("admin", "Admin");
        var k8s = new Mock<k8s.IKubernetes>();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("create-src"));
            await using var ctx = new BunitHost();
            RegisterWorkloads(ctx, k8s, harness);
            var provider = ctx.Render<MudDialogProvider>();

            var reference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Web.Components.Workloads.Shared.CreateWorkloadDialog>(
                    "新建",
                    new DialogParameters { { "ClusterId", cluster.Id }, { "Kind", WorkloadKind.Deployment } });

            provider.WaitForState(() => provider.Markup.Contains("创建"));

            k8s.SetupCreateDeployment("app");
            var yamlField = provider.FindComponents<MudTextField<string>>().First();
            var yaml = """
                apiVersion: apps/v1
                kind: Deployment
                metadata:
                  name: created-web
                  namespace: app
                spec:
                  replicas: 2
                  selector:
                    matchLabels:
                      app: web
                  template:
                    metadata:
                      labels:
                        app: web
                    spec:
                      containers:
                        - name: web
                          image: nginx
                """;
            await provider.InvokeAsync(async () => await yamlField.Instance.ValueChanged!.InvokeAsync(yaml));

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("创建"));
            await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

            provider.WaitForState(() => reference.Result.IsCompleted, TimeSpan.FromSeconds(10));
            var result = await reference.Result;
            Assert.False(result.Canceled);
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Invalid_yaml_shows_error_and_stays_open()
    {
        var harness = new ServiceHarness("admin", "Admin");
        var k8s = new Mock<k8s.IKubernetes>();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("invalid-src"));
            await using var ctx = new BunitHost();
            RegisterWorkloads(ctx, k8s, harness);
            var provider = ctx.Render<MudDialogProvider>();

            var reference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Web.Components.Workloads.Shared.CreateWorkloadDialog>(
                    "新建",
                    new DialogParameters { { "ClusterId", cluster.Id }, { "Kind", WorkloadKind.Deployment } });

            provider.WaitForState(() => provider.Markup.Contains("创建"));

            var yamlField = provider.FindComponents<MudTextField<string>>().First();
            await provider.InvokeAsync(async () => await yamlField.Instance.ValueChanged!.InvokeAsync("{ broken"));

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("创建"));
            await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

            Assert.False(reference.Result.IsCompleted);
        }
        finally
        {
            harness.Dispose();
        }
    }

    [Fact]
    public async Task Protected_namespace_create_is_rejected_and_dialog_stays_open()
    {
        var harness = new ServiceHarness("admin", "Admin");
        var k8s = new Mock<k8s.IKubernetes>();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("protect-ns-src"));
            await using var ctx = new BunitHost();
            RegisterWorkloads(ctx, k8s, harness);
            var provider = ctx.Render<MudDialogProvider>();

            var reference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Web.Components.Workloads.Shared.CreateWorkloadDialog>(
                    "新建",
                    new DialogParameters { { "ClusterId", cluster.Id }, { "Kind", WorkloadKind.Deployment } });

            provider.WaitForState(() => provider.Markup.Contains("创建"));

            var yamlField = provider.FindComponents<MudTextField<string>>().First();
            await provider.InvokeAsync(async () => await yamlField.Instance.ValueChanged!.InvokeAsync("""
                apiVersion: apps/v1
                kind: Deployment
                metadata:
                  name: sys-web
                  namespace: kube-system
                spec:
                  replicas: 1
                  selector:
                    matchLabels:
                      app: web
                  template:
                    metadata:
                      labels:
                        app: web
                    spec:
                      containers:
                        - name: web
                          image: nginx
                """));

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("创建"));
            await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

            // 服务端黑名单拒绝:对话框保持打开,不产生 K8s 调用与审计
            Assert.False(reference.Result.IsCompleted);
            Assert.Empty(harness.Db.AuditLogs);
            k8s.Verify(x => x.AppsV1.CreateNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<V1Deployment>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<bool?>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            harness.Dispose();
        }
    }
}
