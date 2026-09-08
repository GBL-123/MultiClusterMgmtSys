using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public sealed class TestIdentity : IDisposable
{
    private readonly StaticHttpContextAccessor accessor = new();

    public ApplicationDbContext Db { get; }

    public IHttpContextAccessor Accessor => accessor;

    public ServiceProvider Provider { get; }

    public UserManager<ApplicationUser> Users { get; }

    public RoleManager<IdentityRole<int>> Roles { get; }

    public SignInManager<ApplicationUser> SignIn { get; }

    private TestIdentity()
    {
        Db = SqliteDbFactory.CreateContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Db);
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole<int>>()
            .AddUserStore<UserStore<ApplicationUser, IdentityRole<int>, ApplicationDbContext, int>>()
            .AddRoleStore<RoleStore<IdentityRole<int>, ApplicationDbContext, int>>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.AddAuthentication().AddIdentityCookies();

        Provider = services.BuildServiceProvider();
        Users = Provider.GetRequiredService<UserManager<ApplicationUser>>();
        Roles = Provider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        SignIn = Provider.GetRequiredService<SignInManager<ApplicationUser>>();

        var context = new DefaultHttpContext { RequestServices = Provider };
        accessor.HttpContext = context;
    }

    public static TestIdentity Create(string actorName = "admin", params string[] roles)
    {
        var identity = new TestIdentity();
        identity.Context.User = TestPrincipal(actorName, roles);
        return identity;
    }

    public HttpContext Context => accessor.HttpContext!;

    private static ClaimsPrincipal TestPrincipal(string name, string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public void Dispose()
    {
        Provider.Dispose();
        Db.Dispose();
    }

    private sealed class StaticHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
