using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Account;

public class AccountTableTests
{
    [Fact]
    public async Task Rows_render_with_role_names()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var db = SqliteDbFactory.CreateContext();
        var audit = new AuditService(new AuditLogRepository(db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance);
        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" });
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "u1", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            var member = new ApplicationUser { UserName = "m1", CreatedAt = DateTime.UtcNow };
            await identity.Users.CreateAsync(member, "Passw0rd1");
            await identity.Users.AddToRoleAsync(member, "Member");

            var accountService = new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("admin", userId: 1, "Admin").Object,
                NullLogger<AccountService>.Instance);
            ctx.Services.AddSingleton(accountService);

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Shared.AccountTable>(
                parameters => parameters.Add(p => p.Query, new AccountQueryRequest()));

            cut.WaitForState(() => cut.Markup.Contains("u1"));

            Assert.Contains("Member", cut.Markup);
        }
        finally
        {
            db.Dispose();
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Search_filter_limits_rows()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "alpha", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "beta", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            var db = SqliteDbFactory.CreateContext();
            var audit = new AuditService(new AuditLogRepository(db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance);
            var accountService = new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("admin", userId: 9, "Admin").Object,
                NullLogger<AccountService>.Instance);
            ctx.Services.AddSingleton(accountService);

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Shared.AccountTable>(
                parameters => parameters.Add(p => p.Query, new AccountQueryRequest { SearchName = "alph" }));

            cut.WaitForState(() => cut.Markup.Contains("alpha"));
            Assert.DoesNotContain("beta", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }
}

public class AuditLogFilterBarTests
{
    [Fact]
    public async Task Search_and_reset_fire_callbacks()
    {
        await using var ctx = new BunitHost();
        var fired = new List<string>();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.AuditLogs.Shared.AuditLogFilterBar>(
            parameters => parameters
                .Add(p => p.Query, new AuditLogQueryRequest { SearchName = "x" })
                .Add(p => p.OnFilterChanged, () => { fired.Add("search"); return Task.CompletedTask; })
                .Add(p => p.OnReset, () => { fired.Add("reset"); return Task.CompletedTask; }));

        foreach (var label in new[] { "查询", "重置" })
        {
            var button = cut.FindComponents<MudButton>().First(b => b.Markup.Contains(label));
            await cut.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());
        }

        Assert.Equal(["search", "reset"], fired);
    }

    [Fact]
    public async Task Reset_clears_query_fields()
    {
        await using var ctx = new BunitHost();
        var query = new AuditLogQueryRequest { SearchName = "x", Category = AuditCategory.Cluster };

        var cut = ctx.Render<MultiClusterMgmtSys.Components.AuditLogs.Shared.AuditLogFilterBar>(
            parameters => parameters
                .Add(p => p.Query, query)
                .Add(p => p.OnReset, () => Task.CompletedTask));

        var resetButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(() => resetButton.Instance.OnClick.InvokeAsync());

        Assert.Null(query.SearchName);
        Assert.Null(query.Category);
    }
}

public class AccountFilterBarTests
{
    [Fact]
    public async Task Query_button_fires()
    {
        await using var ctx = new BunitHost();
        var fired = false;

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Shared.AccountFilterBar>(
            parameters => parameters
                .Add(p => p.Query, new AccountQueryRequest())
                .Add(p => p.OnFilterChanged, () => { fired = true; return Task.CompletedTask; }));

        var search = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(() => search.Instance.OnClick.InvokeAsync());

        Assert.True(fired);
    }
}
