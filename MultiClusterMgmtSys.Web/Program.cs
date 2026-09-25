using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using MultiClusterMgmtSys.Web.Components;
using MultiClusterMgmtSys.Infrastructure;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Web.Components.Common;
using Serilog;
using Serilog.Events;
using MultiClusterMgmtSys.Application;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Serilog：控制台 + 按天滚动文件日志（生产环境路径由 Logging__File__Path / 挂载 ./logs 控制）
builder.Host.UseSerilog((context, _, configuration) =>
{
    var logPath = context.Configuration["Logging:File:Path"] ?? "logs/app-.log";
    var isProduction = context.HostingEnvironment.IsProduction();
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        // EF Core 的 SQL 语句日志（Executed DbCommand 等）仅在开发环境打印，生产只保留 Warning 及以上（如 SQL 执行报错）
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", isProduction ? LogEventLevel.Warning : LogEventLevel.Information)
        .WriteTo.Console()
        .WriteTo.File(logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            shared: true);
});

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddScoped<ClusterSelectionState>();
builder.Services.AddScoped<RedirectManager>();
builder.Services.AddScoped<ExceptionPresenter>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "MultiClusterMgmtSys.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Events.OnRedirectToLogin = context =>
    {
        var returnUrl = context.Request.Path + context.Request.QueryString;
        if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/")
        {
            returnUrl = "/dashboard";
        }
        context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    var accountService = scope.ServiceProvider.GetRequiredService<AccountService>();
    await accountService.CreateAdminAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// nginx 在 443 终止 TLS 并反代本应用（应用端口只在 compose 内部网络暴露）：
// 信任代理的 X-Forwarded-For/X-Forwarded-Proto，保证 HttpsRedirection、HSTS 和登录重定向等按 https 处理
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // 应用不对外监听，仅 nginx 可达，可安全信任全部代理
    KnownIPNetworks = { },
    KnownProxies = { }
});

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

[ExcludeFromCodeCoverage]
internal partial class Program
{
}
