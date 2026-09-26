using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Bunit;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Microsoft.AspNetCore.Components.Authorization;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Microsoft.AspNetCore.Hosting;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Microsoft.Extensions.Configuration;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using Moq;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MudBlazor;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Web.Components.Common;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using k8s;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class TestPaths
{
    public static string RepoWwwRoot => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MultiClusterMgmtSys.Web", "wwwroot"));
}

public static class BunitServiceExtensions
{
    public static void AddClientCache(this BunitContext ctx)
    {
        ctx.Services.AddSingleton<IClusterClientCache>(sp => new ClusterClientCache(
            sp.GetRequiredService<Func<KubernetesClientConfiguration, IKubernetes>>(),
            NullLogger<ClusterClientCache>.Instance));
    }

    public static ServiceHarness AddClusterStack(this BunitContext ctx, string actor = "admin")
    {
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        var harness = new ServiceHarness(actor, roles);

        var k8sMock = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8sMock));
        ctx.AddClientCache();
        ctx.Services.AddScoped<IClusterRepository>(_ => harness.ClusterRepo);
        ctx.Services.AddScoped<IClusterHealthRepository>(_ => harness.ClusterHealthRepo);
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddSingleton<IYamlValidator, YamlValidator>();
        ctx.Services.AddScoped<ClusterNodeService>();
        ctx.Services.AddScoped<ClusterService>();
        ctx.Services.AddScoped<ExceptionPresenter>();
        ctx.Services.AddSingleton(NullLoggerFactory.Instance);
        return harness;
    }

    public static void AddYamlTemplates(this BunitContext ctx)
    {
        var wwwroot = TestPaths.RepoWwwRoot;
        ctx.Services.AddSingleton<IWebHostEnvironment>(_ => Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == wwwroot));
        ctx.Services.AddSingleton<IYamlTemplateService, YamlTemplateService>();
        ctx.Services.AddSingleton<IYamlValidator, YamlValidator>();
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddWorkloadStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);

        var k8sMock = ctx.Services.BuildServiceProvider().GetRequiredService<Func<KubernetesClientConfiguration, IKubernetes>>();
        return (harness, new Mock<IKubernetes>());
    }

    public static void AddGroupAndSyncStack(this BunitContext ctx, ServiceHarness harness, string actor = "admin")
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        ctx.Services.AddSingleton<IConfiguration>(configuration);
        ctx.Services.AddSingleton<IAppSettingRepository>(_ => new AppSettingRepository(harness.Db));
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped<IGroupRepository>(_ => new GroupRepository(harness.Db));
        ctx.Services.AddScoped<GroupService>();
        ctx.Services.AddScoped<ClusterSyncSettingService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        ctx.Services.AddScoped(_ => TestHttpContext.For(actor).Object);
        ctx.Services.AddScoped<RedirectManager>();
    }

    public static void AddWorkloadServices(this BunitContext ctx, Mock<IKubernetes> k8s, ServiceHarness harness, string actor = "admin")
    {
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped(_ => TestHttpContext.For(actor).Object);
        ctx.Services.AddScoped<RedirectManager>();
        ctx.Services.AddScoped<ClusterSelectionState>();
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddNamespaceStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<NamespaceService>();
        ctx.AddYamlTemplates();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddEventStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<EventService>();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddPodStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<PodService>();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, FakeHelmCliRunner Runner, Mock<IKubernetes> K8s) AddHelmStack(this BunitContext ctx, string actor = "admin", int? userId = null)
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, userId, roles).Object);
        ctx.Services.AddSingleton(new HelmOptions());
        var runner = new FakeHelmCliRunner();
        ctx.Services.AddScoped<IHelmCliRunner>(_ => runner);
        ctx.Services.AddScoped(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<IHelmReleaseOwnershipRepository>(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<HelmService>();
        return (harness, runner, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddDashboardStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<DashboardService>();
        return (harness, k8s);
    }
}
