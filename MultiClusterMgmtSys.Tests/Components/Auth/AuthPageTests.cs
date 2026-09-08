using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Auth;

public class AuthPageTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Login_page_renders_form()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var identity = TestIdentity.Create("anonymous");
        try
        {
            ctx.Services.AddSingleton<IHttpContextAccessor>(identity.Accessor);
            var audit = new AuditService(
                new AuditLogRepository(identity.Db),
                TestHttpContext.Anonymous().Object,
                NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AuthService(
                identity.Users, identity.SignIn, audit, identity.Accessor, NullLogger<AuthService>.Instance));
            ctx.Services.AddScoped<RedirectManager>();
            ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Auth.Pages.Login>(
                parameters => parameters.AddCascadingValue(new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = ctx.Services }));
            cut.WaitForState(() => cut.Markup.Contains("登录"));
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Register_page_renders_form()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var identity = TestIdentity.Create("anonymous");
        try
        {
            ctx.Services.AddSingleton<IHttpContextAccessor>(identity.Accessor);
            var audit = new AuditService(
                new AuditLogRepository(identity.Db),
                TestHttpContext.Anonymous().Object,
                NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AuthService(
                identity.Users, identity.SignIn, audit, identity.Accessor,
                NullLogger<AuthService>.Instance));
            ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Auth.Pages.Register>();

            Assert.Contains("注册", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }
}

