using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Templates;
using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Web.Components.Common;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Services;
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
        ctx.Services.AddScoped(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<IHelmReleaseOwnershipRepository>(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped<ResourceOwnershipGuard>();
        ctx.Services.AddSingleton<IYamlValidator, YamlValidator>();
        ctx.Services.AddScoped<ClusterNodeService>();
        ctx.Services.AddSingleton(_ => RetentionStubs.For(harness.ClusterHealthRepo));
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
        ctx.Services.AddScoped<AlertSettingService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped<RedirectManager>();
    }

    public static void AddWorkloadServices(this BunitContext ctx, Mock<IKubernetes> k8s, ServiceHarness harness, string actor = "admin")
    {
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ConfigMapService>();
        ctx.Services.AddScoped<SvcService>();
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<IHelmReleaseOwnershipRepository>(_ => harness.OwnershipRepo);
        ctx.Services.AddScoped<ResourceOwnershipGuard>();
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

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddSecretStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<SecretService>();
        ctx.AddYamlTemplates();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddStorageStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<StorageService>();
        ctx.AddYamlTemplates();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddCompareStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ConfigMapService>();
        ctx.Services.AddScoped<ClusterCompareService>();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddTopologyStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        ctx.AddGroupAndSyncStack(harness, actor);
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<TopologyService>();
        ctx.Services.AddScoped<PodService>();
        ctx.Services.AddScoped<SvcService>();
        return (harness, k8s);
    }

    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddAlertStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        ctx.Services.AddSingleton<IConfiguration>(configuration);
        ctx.Services.AddSingleton<IAppSettingRepository>(_ => new AppSettingRepository(harness.Db));
        ctx.Services.AddScoped<IAlertRepository>(_ => new AlertRepository(harness.Db));
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped<ClusterSyncSettingService>();
        ctx.Services.AddScoped<AlertSettingService>();
        ctx.Services.AddScoped<AlertService>();
        return (harness, new Mock<IKubernetes>());
    }

    /// <summary>舰队模板页所需服务栈:复用集群栈,叠加工作负载 / 配置 / 舰队模板服务(舰队下发经逐作用域解析它们)。</summary>
    public static (ServiceHarness Harness, Mock<IKubernetes> K8s) AddFleetStack(this BunitContext ctx, string actor = "admin")
    {
        var harness = ctx.AddClusterStack(actor);
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        ctx.Services.AddScoped(_ => TestHttpContext.ForIdentity(actor, 7, roles).Object);
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ConfigMapService>();
        ctx.Services.AddScoped<FleetTemplateService>();
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        return (harness, k8s);
    }
}