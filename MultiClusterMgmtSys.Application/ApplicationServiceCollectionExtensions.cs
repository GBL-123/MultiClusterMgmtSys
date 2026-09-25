using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Services;

namespace MultiClusterMgmtSys.Application;

/// <summary>
/// Application 层服务注册扩展:宿主组合根调用,集中登记用例服务;
/// 新增用例服务时在此登记,不散落到 Program.cs。
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>注册 Application 层用例服务(集群/分组/审计/账号/认证/工作负载等)。</summary>
    /// <param name="services">DI 容器。</param>
    /// <returns>同一容器,便于链式调用。</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ClusterNodeService>();
        services.AddScoped<ConfigMapService>();
        services.AddScoped<SvcService>();
        services.AddScoped<EventService>();
        services.AddScoped<PodService>();
        services.AddScoped<NamespaceService>();
        services.AddScoped<WorkloadService>();
        services.AddScoped<ClusterService>();
        services.AddScoped<GroupService>();
        services.AddScoped<AuditService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();
        services.AddScoped<ClusterSyncSettingService>();
        services.AddScoped<DashboardService>();
        services.AddSingleton<IYamlValidator, YamlValidator>();
        return services;
    }
}
