using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// bUnit 宿主:注册 MudBlazor 服务 + 真实服务链(SQLite 内存库)。
/// 断言只针对"组件如何使用 MudBlazor",不碰 MudBlazor 内部 DOM。
/// </summary>
public static class BunitHost
{
    public static TestContext Create(ApplicationDbContext db)
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(TimeProvider.System);

        ctx.Services.AddMudServices();
        ctx.Services.AddScoped(_ => db);
        ctx.Services.AddScoped<ClusterRepository>();
        ctx.Services.AddScoped<GroupRepository>();
        ctx.Services.AddScoped<AuditLogRepository>();
        ctx.Services.AddScoped<ClusterSelectionState>();
        ctx.Services.AddScoped<AuditService>(sp =>
            new AuditService(sp.GetRequiredService<AuditLogRepository>(), new TestServices.NullHttpContextAccessor(), NullLogger<AuditService>.Instance));
        ctx.Services.AddScoped<ClusterNodeService>(sp =>
            new ClusterNodeService(
                sp.GetRequiredService<ClusterRepository>(),
                sp.GetRequiredService<AuditService>(),
                NullLogger<ClusterNodeService>.Instance,
                TestServices.ThrowingFactory()));
        ctx.Services.AddScoped<ClusterService>(sp =>
            new ClusterService(
                sp.GetRequiredService<ClusterRepository>(),
                sp.GetRequiredService<ClusterNodeService>(),
                sp.GetRequiredService<AuditService>(),
                NullLogger<ClusterService>.Instance,
                TestServices.ThrowingFactory()));
        ctx.Services.AddScoped<GroupService>(sp =>
            new GroupService(
                sp.GetRequiredService<GroupRepository>(),
                sp.GetRequiredService<ClusterRepository>(),
                sp.GetRequiredService<AuditService>(),
                NullLogger<GroupService>.Instance));
        ctx.Services.AddScoped<ConfigMapService>(sp =>
            new ConfigMapService(
                sp.GetRequiredService<ClusterRepository>(),
                sp.GetRequiredService<AuditService>(),
                NullLogger<ConfigMapService>.Instance,
                TestServices.ThrowingFactory()));
        ctx.Services.AddScoped<ExceptionPresenter>();
        ctx.Services.AddScoped<RedirectManager>();
        ctx.Services.AddScoped<AccountService>(sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var (userManager, roleManager, _) = SeedUser.CreateIdentityAsync(db).GetAwaiter().GetResult();
            return new AccountService(
                userManager,
                roleManager,
                db,
                sp.GetRequiredService<AuditService>(),
                new TestServices.NullHttpContextAccessor(),
                NullLogger<AccountService>.Instance);
        });

        return ctx;
    }
}