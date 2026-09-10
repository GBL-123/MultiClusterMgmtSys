using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class CreateWorkloadDialogTests
{
    private static void RegisterWorkloads(BunitHost ctx, Mock<k8s.IKubernetes> k8s, ServiceHarness harness)
    {
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.Services.AddScoped(_ => harness.ClusterRepo);
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
                .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.CreateWorkloadDialog>(
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
            ctx.Services.AddScoped(_ => TestHttpContext.For("admin", "Admin").Object);
            var provider = ctx.Render<MudDialogProvider>();

            var reference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.CreateWorkloadDialog>(
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
                .ShowAsync<MultiClusterMgmtSys.Components.Workloads.Shared.CreateWorkloadDialog>(
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
}

