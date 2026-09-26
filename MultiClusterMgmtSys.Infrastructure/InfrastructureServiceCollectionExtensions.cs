using k8s;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Identity;
using MultiClusterMgmtSys.Application.Services.Identity;
using MultiClusterMgmtSys.Infrastructure.Helm;
using MultiClusterMgmtSys.Infrastructure.Identity;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Infrastructure.Sync;
using MultiClusterMgmtSys.Infrastructure.Templates;

namespace MultiClusterMgmtSys.Infrastructure;

/// <summary>
/// Infrastructure 层注册扩展:宿主组合根调用,集中登记持久化、Identity、k8s 客户端与后台同步;
/// 新增基础设施实现时在此登记,不散落到 Program.cs。
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Infrastructure 层实现:SQLite(连接字符串相对路径锚定到内容根)、仓库端口实现、
    /// ASP.NET Identity(kint 主键 + 中文错误描述)、k8s 客户端工厂与缓存、YAML 模板读取、定时同步后台服务。
    /// </summary>
    /// <param name="services">DI 容器。</param>
    /// <param name="configuration">应用配置(读取 DefaultConnection 连接字符串)。</param>
    /// <param name="contentRootPath">内容根路径,用于解析相对 SQLite 路径。</param>
    /// <returns>同一容器,便于链式调用。</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        // 相对路径的 SQLite 库一律锚定到内容根解析(工作目录随 dotnet run 的调用位置漂移),
        // 并确保父目录存在——SQLite 只建文件不建目录,目录缺失时报 SQLite Error 14
        var sqliteBuilder = new SqliteConnectionStringBuilder(rawConnectionString);
        if (!string.IsNullOrEmpty(sqliteBuilder.DataSource) && !Path.IsPathRooted(sqliteBuilder.DataSource))
        {
            var dbPath = Path.GetFullPath(Path.Combine(contentRootPath, sqliteBuilder.DataSource));
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            sqliteBuilder.DataSource = dbPath;
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(sqliteBuilder.ToString()));

        services.AddScoped<IClusterRepository, ClusterRepository>();
        services.AddScoped<IClusterHealthRepository, ClusterHealthRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IAccountQueryRepository, AccountQueryRepository>();
        services.AddScoped<IHelmReleaseOwnershipRepository, HelmReleaseOwnershipRepository>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddErrorDescriber<ChineseIdentityErrorDescriber>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddSingleton(HelmOptions.FromValues(
            configuration["Helm:CliPath"],
            configuration["Helm:MaxPackageBytes"]));

        services.AddSingleton<IProcessExecutor, ProcessExecutor>();
        services.AddSingleton<IHelmCliRunner, HelmCliRunner>();
        services.AddHostedService<HelmTempCleanupHostedService>();

        services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(
            config => new k8s.Kubernetes(config));
        services.AddSingleton<IClusterClientCache, ClusterClientCache>();
        services.AddSingleton<IYamlTemplateService, YamlTemplateService>();
        services.AddHostedService<ClusterSyncBackgroundService>();

        return services;
    }
}
