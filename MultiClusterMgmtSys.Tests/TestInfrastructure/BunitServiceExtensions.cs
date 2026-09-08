using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;
using k8s;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class BunitServiceExtensions
{
    public static ServiceHarness AddClusterStack(this BunitContext ctx, string actor = "admin")
    {
        var roles = actor == "admin" ? new[] { "Admin" } : Array.Empty<string>();
        var harness = new ServiceHarness(actor, roles);

        var k8sMock = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8sMock));
        ctx.Services.AddScoped(_ => harness.ClusterRepo);
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped<ClusterNodeService>();
        ctx.Services.AddScoped<ClusterService>();
        ctx.Services.AddScoped<ExceptionPresenter>();
        ctx.Services.AddSingleton(NullLoggerFactory.Instance);
        return harness;
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
        ctx.Services.AddSingleton(_ => new AppSettingRepository(harness.Db));
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped<GroupRepository>(_ => new GroupRepository(harness.Db));
        ctx.Services.AddScoped<GroupService>();
        ctx.Services.AddScoped<ClusterSyncSettingService>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        ctx.Services.AddScoped(_ => TestHttpContext.For(actor).Object);
        ctx.Services.AddScoped<RedirectManager>();
    }

    public static void AddWorkloadServices(this BunitContext ctx, Mock<IKubernetes> k8s, ServiceHarness harness, string actor = "admin")
    {
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped(_ => harness.Audit);
        ctx.Services.AddScoped(_ => TestHttpContext.For(actor).Object);
        ctx.Services.AddScoped<RedirectManager>();
        ctx.Services.AddScoped<ClusterSelectionState>();
    }
}
