using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Security.Claims;
using MultiClusterMgmtSys.Components;
using MultiClusterMgmtSys.Daos;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Services;

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
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<PasswordHasher<string>>();
builder.Services.AddHttpClient();

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

app.MapPost("/api/login", async (HttpContext ctx, AccountService accountService, LoginRequest input) =>
{
    if (input is null)
        return Results.BadRequest();

    var account = await accountService.ValidateCredentialsAsync(input.Username, input.Password);
    if (account is null)
        return Results.Unauthorized();

    var principal = accountService.CreateClaimsPrincipal(account);
    var props = new AuthenticationProperties { IsPersistent = input.RememberMe };
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    return Results.Ok();
}).DisableAntiforgery();

app.MapPost("/api/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).DisableAntiforgery();

app.Run();
