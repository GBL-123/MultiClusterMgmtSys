using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class AccountsPageFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static TestIdentity Register(BunitHost ctx, ServiceHarness harness)
    {
        var identity = TestIdentity.Create("admin", "Admin");
        identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Admin", NormalizedName = "ADMIN" }).Wait();
        identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" }).Wait();
        var audit = new AuditService(
            new AuditLogRepository(identity.Db),
            TestHttpContext.ForIdentity("admin", userId: 99, "Admin").Object,
            NullLogger<AuditService>.Instance);
        var accountService = new AccountService(
            identity.Users, identity.Roles, identity.Db, audit,
            TestHttpContext.ForIdentity("admin", userId: 99, "Admin").Object,
            NullLogger<AccountService>.Instance);
        ctx.Services.AddSingleton(accountService);
        return identity;
    }

    [Fact]
    public async Task Reset_password_via_dialog_changes_password()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var identity = Register(ctx, harness);
        try
        {
            var svc = ctx.Services.GetRequiredService<AccountService>();
            await svc.CreateAccountAsync(new AccountCreateRequest("reset-me", "Passw0rd1", "Admin"));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Pages.Accounts>();
            cut.WaitForState(() => cut.Markup.Contains("reset-me"));

            var provider = ctx.Render<MudDialogProvider>();
            var resetButton = cut.FindAll("button")
                .First(b => (b.GetAttribute("aria-label") ?? "").Contains("重置") || (b.GetAttribute("aria-label") ?? "").Contains("密码"));
            resetButton.Click();

            provider.WaitForState(() => provider.Markup.Contains("新密码"));

            var fields = provider.FindComponents<MudTextField<string>>();
            await provider.InvokeAsync(async () => await fields[0].Instance.ValueChanged!.InvokeAsync("NewPass12"));
            await provider.InvokeAsync(async () => await fields[1].Instance.ValueChanged!.InvokeAsync("NewPass12"));

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
            submit.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                var user = await identity.Users.FindByNameAsync("reset-me");
                if (user is not null && await identity.Users.CheckPasswordAsync(user, "NewPass12"))
                {
                    break;
                }
            }

            var finalUser = await identity.Users.FindByNameAsync("reset-me");
            Assert.True(await identity.Users.CheckPasswordAsync(finalUser!, "NewPass12"));
        }
        finally
        {
            identity.Dispose();
        }
    }
}
