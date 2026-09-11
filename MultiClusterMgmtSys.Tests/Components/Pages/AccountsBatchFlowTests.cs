using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class AccountsBatchFlowTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static TestIdentity Register(BunitHost ctx)
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
    public async Task Batch_mode_toggle_shows_and_hides_checkboxes()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var identity = Register(ctx);
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "row-user", CreatedAt = DateTime.UtcNow }, "Passw0rd1");

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Pages.Accounts>();
            cut.WaitForState(() => cut.Markup.Contains("row-user"));

            Assert.Empty(cut.FindComponents<MudCheckBox<bool>>());

            var batchButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("批量操作"));
            await cut.InvokeAsync(async () => await batchButton.Instance.OnClick.InvokeAsync());

            Assert.Contains("退出批量", cut.Markup);
            Assert.NotEmpty(cut.FindComponents<MudCheckBox<bool>>());

            var exitButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("退出批量"));
            await cut.InvokeAsync(async () => await exitButton.Instance.OnClick.InvokeAsync());

            Assert.DoesNotContain("退出批量", cut.Markup);
            Assert.Empty(cut.FindComponents<MudCheckBox<bool>>());
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Batch_delete_via_confirm_removes_selected_user()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var identity = Register(ctx);
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "victim-user", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            var victim = await identity.Users.FindByNameAsync("victim-user");

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Account.Pages.Accounts>();
            cut.WaitForState(() => cut.Markup.Contains("victim-user"));

            var batchButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("批量操作"));
            await cut.InvokeAsync(async () => await batchButton.Instance.OnClick.InvokeAsync());

            var checkbox = cut.FindComponents<MudCheckBox<bool>>().First();
            await cut.InvokeAsync(async () => await checkbox.Instance.ValueChanged!.InvokeAsync(true));

            cut.WaitForState(() => cut.Markup.Contains("个账号已选"), TimeSpan.FromSeconds(5));

            var provider = ctx.Render<MudDialogProvider>();
            var batchDeleteButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("批量删除"));
            batchDeleteButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                if (await identity.Users.FindByIdAsync(victim!.Id.ToString()) is null) break;
            }

            Assert.Null(await identity.Users.FindByIdAsync(victim!.Id.ToString()));
            Assert.True(identity.Db.AuditLogs.Any(a => a.Category == AuditCategory.Account && a.Action == AuditAction.Delete));
        }
        finally
        {
            identity.Dispose();
        }
    }
}
