using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MultiClusterMgmtSys.Components;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Components.Theme;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ClusterRepository>();
builder.Services.AddScoped<GroupRepository>();
builder.Services.AddScoped<ClusterService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<ThemeManager>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PasswordHasher<string>>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MultiClusterMgmtSys.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

app.MapPost("/api/login", async (HttpContext ctx, [Microsoft.AspNetCore.Mvc.FromServices] AccountService accountService) =>
{
    var form = ctx.Request.Form;
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "true" || form["rememberMe"] == "True";
    var returnUrl = string.IsNullOrEmpty(form["returnUrl"]) ? "/clusters" : form["returnUrl"].ToString();

    var account = await accountService.ValidateCredentialsAsync(username, password);
    if (account is null)
        return Results.Redirect($"/login?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");

    var principal = accountService.CreateClaimsPrincipal(account);
    var props = new AuthenticationProperties { IsPersistent = rememberMe };
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    return Results.LocalRedirect(returnUrl);
}).DisableAntiforgery();

app.MapGet("/api/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("/login");
});

app.Run();
