using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MultiClusterMgmtSys.Components;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Services.Identity;
using MultiClusterMgmtSys.Components.Theme;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// ASP.NET Core Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddPasswordValidator<AlphanumericPasswordValidator>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "MultiClusterMgmtSys.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Events.OnRedirectToLogin = ctx =>
        {
            var returnUrl = ctx.Request.Path + ctx.Request.QueryString;
            if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/")
            {
                returnUrl = "/clusters";
            }
            ctx.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.Name = "MultiClusterMgmtSys.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<ClusterRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<ClusterNodeService>();
builder.Services.AddScoped<ConfigMapService>();
builder.Services.AddScoped<ClusterService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<ThemeManager>();
builder.Services.AddScoped<AccountService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var accountService = scope.ServiceProvider.GetRequiredService<AccountService>();
    await accountService.SeedAccountsAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/api/login", async (HttpContext ctx, SignInManager<ApplicationUser> signInManager) =>
{
    var form = ctx.Request.Form;
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "true" || form["rememberMe"] == "True";
    var returnUrl = string.IsNullOrEmpty(form["returnUrl"]) ? "/clusters" : form["returnUrl"].ToString();

    var result = await signInManager.PasswordSignInAsync(username, password, isPersistent: rememberMe, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}");
    }

    return Results.LocalRedirect(returnUrl);
}).DisableAntiforgery();

app.MapGet("/api/logout", async (HttpContext ctx, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/login");
});

app.MapPost("/api/register", async (HttpContext ctx, AccountService accountService) =>
{
    var form = ctx.Request.Form;
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();
    var displayName = form["displayName"].ToString().Trim();
    var returnUrl = string.IsNullOrEmpty(form["returnUrl"]) ? "/" : form["returnUrl"].ToString();

    if (password != confirmPassword)
    {
        return Results.Redirect($"/register?error=mismatch&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}");
    }

    var result = await accountService.RegisterAsync(username, password, string.IsNullOrEmpty(displayName) ? null : displayName);
    if (!result.Succeeded)
    {
        var code = result.Errors.FirstOrDefault()?.Code ?? "unknown";
        var mapped = code switch
        {
            "DuplicateUserName" => "duplicate",
            "PasswordTooWeak" or "PasswordTooShort" or "PasswordRequiresLetter" or "PasswordRequiresDigit" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresUpper" or "PasswordRequiresLower" => "weakpwd",
            _ => "unknown"
        };
        return Results.Redirect($"/register?error={mapped}&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}");
    }

    // Auto sign-in after successful registration
    var signIn = await accountService.PasswordSignInAsync(username, password, isPersistent: false);
    if (!signIn.Succeeded)
    {
        return Results.LocalRedirect("/login");
    }
    return Results.LocalRedirect(returnUrl);
}).DisableAntiforgery();

app.Run();
