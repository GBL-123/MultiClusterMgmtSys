using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Dialogs;

public class ProfilePageTests
{
    private static void Authorize(BunitHost ctx, string name, string role)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized(name);
        auth.SetRoles(role);
    }

    [Fact]
    public async Task Admin_profile_shows_role_badge_and_sync_settings()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin", "Admin");
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);

        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "admin", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, harness.Audit,
                TestHttpContext.ForIdentity("admin", userId: 1, "Admin").Object,
                NullLogger<AccountService>.Instance));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Profile.Pages.Profile>();

            cut.WaitForState(() => cut.Markup.Contains("账号信息"));

            Assert.Contains("个人资料", cut.Markup);
            Assert.Contains("admin", cut.Markup);
            Assert.Contains("role-badge admin", cut.Markup);
            Assert.Contains("定时同步", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Member_profile_hides_sync_settings()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "member", "Member");
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness, "member");

        var identity = TestIdentity.Create("member", "Member");
        try
        {
            await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Member", NormalizedName = "MEMBER" });
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "member", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            var audit = new AuditService(
                new AuditLogRepository(identity.Db),
                TestHttpContext.For("member", "Member").Object,
                NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("member", userId: 2).Object,
                NullLogger<AccountService>.Instance));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Profile.Pages.Profile>(
                parameters => parameters.AddCascadingValue(
                    new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = ctx.Services }));

            cut.WaitForState(() => cut.Markup.Contains("账号信息"));

            Assert.Contains("role-badge member", cut.Markup);
            Assert.DoesNotContain("定时同步", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Profile_shows_recent_operations()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin", "Admin");
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        await harness.Audit.LogAsync(AuditCategory.Cluster, AuditAction.Create, "集群: recent-op");

        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "admin", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, harness.Audit,
                TestHttpContext.ForIdentity("admin", userId: 1, "Admin").Object,
                NullLogger<AccountService>.Instance));

            var cut = ctx.Render<MultiClusterMgmtSys.Components.Profile.Pages.Profile>(
                parameters => parameters.AddCascadingValue(
                    new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = ctx.Services }));

            cut.WaitForState(() => cut.Markup.Contains("recent-op"));

            Assert.Contains("最近操作", cut.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }
}

public class ChangePasswordDialogTests
{
    private static async Task<(BunitHost Ctx, IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> Open(
        AccountService accountService, ISnackbar snackbar)
    {
        var ctx = new BunitHost();
        ctx.Services.AddSingleton<ISnackbar>(snackbar);
        ctx.Services.AddSingleton(accountService);
        ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));
        var provider = ctx.Render<MudDialogProvider>();

        var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Components.Profile.Shared.ChangePasswordDialog>("修改密码");
        provider.WaitForState(() => provider.Markup.Contains("修改密码"));
        return (ctx, provider, dialogReference);
    }

    private static async Task FillAsync(BunitHost ctx, IRenderedComponent<MudDialogProvider> provider, string current, string newPwd, string confirm)
    {
        var fields = provider.FindComponents<MudTextField<string>>();
        await provider.InvokeAsync(async () => await fields[0].Instance.ValueChanged!.InvokeAsync(current));
        await provider.InvokeAsync(async () => await fields[1].Instance.ValueChanged!.InvokeAsync(newPwd));
        await provider.InvokeAsync(async () => await fields[2].Instance.ValueChanged!.InvokeAsync(confirm));
    }

    [Fact]
    public async Task Mismatch_shows_error_and_keeps_dialog_open()
    {
        var identity = TestIdentity.Create("admin", "Admin");
        var db = identity.Db;
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "admin" }, "Passw0rd1");
            var snackbarMock = new Mock<ISnackbar>();
            var accountService = new AccountService(
                identity.Users, identity.Roles, db, new AuditService(
                    new AuditLogRepository(db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance),
                TestHttpContext.For("admin", "Admin").Object, NullLogger<AccountService>.Instance);

            var (ctx, provider, dialogReference) = await Open(accountService, snackbarMock.Object);
            try
            {
                await FillAsync(ctx, provider, "Passw0rd1", "NewPass12", "Different1");

                var submit = provider.FindComponents<MudButton>()
                    .First(b => b.Markup.Contains("修改密码"));
                await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

                snackbarMock.Verify(s => s.Add(
                    "两次输入的密码不一致", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
                Assert.False(dialogReference.Result.IsCompleted);
            }
            finally
            {
                await ctx.DisposeAsync();
            }
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Success_closes_dialog_with_ok()
    {
        var identity = TestIdentity.Create("admin", "Admin");
        var db = identity.Db;
        try
        {
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "admin" }, "Passw0rd1");
            var snackbarMock = new Mock<ISnackbar>();
            var accountService = new AccountService(
                identity.Users, identity.Roles, db, new AuditService(
                    new AuditLogRepository(db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance),
                TestHttpContext.For("admin", "Admin").Object, NullLogger<AccountService>.Instance);

            var (ctx, provider, dialogReference) = await Open(accountService, snackbarMock.Object);
            try
            {
                await FillAsync(ctx, provider, "Passw0rd1", "Changed_9x", "Changed_9x");

                var submit = provider.FindComponents<MudButton>()
                    .First(b => b.Markup.Contains("修改密码"));
                await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

                var result = await dialogReference.Result;
                Assert.False(result.Canceled);
                Assert.True((bool)result.Data!);
            }
            finally
            {
                await ctx.DisposeAsync();
            }
        }
        finally
        {
            identity.Dispose();
        }
    }
}

public class AccountEditDialogTests
{
    [Fact]
    public async Task Create_mode_renders_password_field_and_create_button()
    {
        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await using var ctx = new BunitHost();
            var audit = new AuditService(new AuditLogRepository(identity.Db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("admin", userId: 9, "Admin").Object,
                NullLogger<AccountService>.Instance));
            ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));
            var provider = ctx.Render<MudDialogProvider>();

            await ctx.Services.GetRequiredService<IDialogService>().ShowAsync<MultiClusterMgmtSys.Components.Account.Shared.AccountEditDialog>(
                "创建账号",
                new DialogParameters { { "EditingAccount", (MultiClusterMgmtSys.ViewModels.AccountViewModel?)null } });

            provider.WaitForState(() => provider.Markup.Contains("创建"));

            Assert.Contains("用户名", provider.Markup);
            Assert.Contains("初始密码", provider.Markup);
            Assert.Contains("创建", provider.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }

    [Fact]
    public async Task Edit_mode_prefills_user_name_and_save_button()
    {
        var identity = TestIdentity.Create("admin", "Admin");
        try
        {
            await using var ctx = new BunitHost();
            var audit = new AuditService(new AuditLogRepository(identity.Db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance);
            ctx.Services.AddSingleton(new AccountService(
                identity.Users, identity.Roles, identity.Db, audit,
                TestHttpContext.ForIdentity("admin", userId: 9, "Admin").Object,
                NullLogger<AccountService>.Instance));
            ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));
            var provider = ctx.Render<MudDialogProvider>();

            await ctx.Services.GetRequiredService<IDialogService>().ShowAsync<MultiClusterMgmtSys.Components.Account.Shared.AccountEditDialog>(
                "编辑账号",
                new DialogParameters
                {
                    { "EditingAccount", new MultiClusterMgmtSys.ViewModels.AccountViewModel { Id = 5, UserName = "edit-me", RoleName = "Admin" } }
                });

            provider.WaitForState(() => provider.Markup.Contains("edit-me"));

            Assert.Contains("edit-me", provider.Markup);
            Assert.Contains("保存", provider.Markup);
            Assert.DoesNotContain("初始密码", provider.Markup);
        }
        finally
        {
            identity.Dispose();
        }
    }
}

public class ResetPasswordDialogTests
{
    [Fact]
    public async Task Mismatch_shows_snackbar_error()
    {
        var identity = TestIdentity.Create("admin", "Admin");
        var db = identity.Db;
        try
        {
            await identity.Roles.CreateAsync(new IdentityRole<int> { Name = "Admin", NormalizedName = "ADMIN" });
            await identity.Users.CreateAsync(new ApplicationUser { UserName = "target", CreatedAt = DateTime.UtcNow }, "Passw0rd1");
            var audit = new AuditService(new AuditLogRepository(db), TestHttpContext.Anonymous().Object, NullLogger<AuditService>.Instance);
            var accountService = new AccountService(
                identity.Users, identity.Roles, db, audit,
                TestHttpContext.ForIdentity("admin", userId: 8, "Admin").Object,
                NullLogger<AccountService>.Instance);

            await using var ctx = new BunitHost();
            var snackbarMock = new Mock<ISnackbar>();
            ctx.Services.AddSingleton<ISnackbar>(snackbarMock.Object);
            ctx.Services.AddSingleton(accountService);
            ctx.Renderer.SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("bunit", true));
            var provider = ctx.Render<MudDialogProvider>();

            var dialogReference = await ctx.Services.GetRequiredService<IDialogService>()
                .ShowAsync<MultiClusterMgmtSys.Components.Account.Shared.ResetPasswordDialog>(
                    "重置密码",
                    new DialogParameters { { "AccountId", 2 } });

            provider.WaitForState(() => provider.Markup.Contains("重置密码"));

            var fields = provider.FindComponents<MudTextField<string>>();
            await provider.InvokeAsync(async () => await fields[0].Instance.ValueChanged!.InvokeAsync("NewPass12"));
            await provider.InvokeAsync(async () => await fields[1].Instance.ValueChanged!.InvokeAsync("Different12"));

            var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
            await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add(
                "两次输入的密码不一致", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
            Assert.False(dialogReference.Result.IsCompleted);
        }
        finally
        {
            identity.Dispose();
        }
    }
}


