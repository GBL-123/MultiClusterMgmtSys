using k8s;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Services;
using System.Security.Claims;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>基于真实 Identity + SQLite 的用户/角色种子工具。</summary>
public static class SeedUser
{
    public static async Task<(UserManager<ApplicationUser> UserManager, RoleManager<IdentityRole<int>> RoleManager, SignInManager<ApplicationUser> SignInManager)> CreateIdentityAsync(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddHttpContextAccessor();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager();

        services.AddScoped(_ => db);
        var provider = services.BuildServiceProvider();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var signInManager = provider.GetRequiredService<SignInManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("Member"))
        {
            await roleManager.CreateAsync(new IdentityRole<int>("Member"));
        }

        return (userManager, roleManager, signInManager);
    }

    public static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> userManager, string userName, string password, string? role = null)
    {
        var user = new ApplicationUser { UserName = userName };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (role is not null)
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}

/// <summary>服务构造助手:按服务真实依赖装配,注入可替换的 K8s 工厂。</summary>
public static class TestServices
{
    public sealed class NullHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    /// <summary>
    /// 可构造身份的 HttpContext:按用户名/用户 ID/角色注入 Claims。
    /// userName 为 null 时代表"无身份"(等价 NullHttpContextAccessor)。
    /// </summary>
    public sealed class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public FakeHttpContextAccessor(string? userName, int? userId = null, params string[] roles)
        {
            var claims = new List<Claim>();
            if (userName is not null) claims.Add(new Claim(ClaimTypes.Name, userName));
            if (userId.HasValue) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            };
        }

        public HttpContext? HttpContext { get; set; }
    }

    public static AuditService Audit(ApplicationDbContext db, string? userName = null)
    {
        IHttpContextAccessor accessor = userName is null
            ? new NullHttpContextAccessor()
            : new FakeHttpContextAccessor(userName);
        return new(new AuditLogRepository(db), accessor, NullLogger<AuditService>.Instance);
    }

    public static ClusterSyncSettingService SyncSetting(ApplicationDbContext db, IConfiguration configuration, string? userName = null, string? role = null)
    {
        IHttpContextAccessor accessor = userName is null
            ? new NullHttpContextAccessor()
            : new FakeHttpContextAccessor(userName, roles: role is null ? [] : [role]);
        return new(new AppSettingRepository(db), configuration, accessor, Audit(db, userName), NullLogger<ClusterSyncSettingService>.Instance);
    }

    public static ClusterNodeService NodeService(ApplicationDbContext db, Func<KubernetesClientConfiguration, IKubernetes> factory)
        => new(new ClusterRepository(db), Audit(db), NullLogger<ClusterNodeService>.Instance, factory);

    public static ClusterService Cluster(ApplicationDbContext db, Func<KubernetesClientConfiguration, IKubernetes> factory)
        => new(new ClusterRepository(db), NodeService(db, factory), Audit(db), NullLogger<ClusterService>.Instance, factory);

    public static ConfigMapService ConfigMap(ApplicationDbContext db, Func<KubernetesClientConfiguration, IKubernetes> factory)
        => new(new ClusterRepository(db), Audit(db), NullLogger<ConfigMapService>.Instance, factory);

    public static WorkloadService Workload(ApplicationDbContext db, Func<KubernetesClientConfiguration, IKubernetes> factory)
        => new(new ClusterRepository(db), Audit(db), NullLogger<WorkloadService>.Instance, factory);

    public static GroupService Group(ApplicationDbContext db)
        => new(new GroupRepository(db), new ClusterRepository(db), Audit(db), NullLogger<GroupService>.Instance);

    /// <summary>
    /// 返回惰性 mock 的工厂:工厂调用本身不抛错(与生产一致);
    /// 若测试路径真的走到 K8s 调用,惰性 mock 会因未配置成员而失败,从而暴露"不该走到这里"。
    /// </summary>
    public static Func<KubernetesClientConfiguration, IKubernetes> ThrowingFactory()
        => _ => Mock.Of<IKubernetes>();
}