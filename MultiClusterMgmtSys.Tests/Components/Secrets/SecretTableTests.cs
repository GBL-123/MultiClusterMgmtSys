using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Secrets;

public class SecretListTableTests
{
    private static SecretListViewModel Item(string name, string ns = "app", string? type = "Opaque", int keys = 1, bool canOperate = true)
        => new()
        {
            Name = name,
            Namespace = ns,
            Type = type,
            KeyCount = keys,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CanOperate = canOperate
        };

    [Fact]
    public async Task Renders_items_with_type_and_counts()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretListTable>(
            parameters => parameters
                .Add(p => p.Items, new[]
                {
                    Item("sec-a"),
                    Item("sec-b", "kube-system", "kubernetes.io/tls", 2)
                }));

        Assert.Contains("sec-a", cut.Markup);
        Assert.Contains("app", cut.Markup);
        Assert.Contains("Opaque", cut.Markup);
        Assert.Contains("kube-system", cut.Markup);
        Assert.Contains("kubernetes.io/tls", cut.Markup);
        Assert.Contains("2026-01-01 00:00", cut.Markup);
    }

    [Fact]
    public async Task Missing_type_renders_placeholder()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretListTable>(
            parameters => parameters
                .Add(p => p.Items, new[] { Item("sec-a", type: null) }));

        Assert.Contains("—", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_shown_when_no_items()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretListTable>(
            parameters => parameters.Add(p => p.Items, Array.Empty<SecretListViewModel>()));

        Assert.Contains("暂无密钥", cut.Markup);
    }

    [Fact]
    public async Task Mutating_buttons_follow_can_operate_projection()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretListTable>(
            parameters => parameters
                .Add(p => p.Items, new[] { Item("owned"), Item("unowned", canOperate: false) })
                .Add(p => p.OnNavigateDetail, _ => Task.CompletedTask));

        Assert.Equal(2, cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除"));

        auth.SetRoles("Member");
        cut.Render();
        Assert.Equal(2, cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除"));
    }

    [Fact]
    public async Task Name_click_navigates_to_detail()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        (string ns, string name)? navigated = null;
        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretListTable>(
            parameters => parameters
                .Add(p => p.Items, new[] { Item("sec-a") })
                .Add(p => p.OnNavigateDetail, args => { navigated = args; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("sec-a")).Click();

        Assert.Equal(("app", "sec-a"), navigated!.Value);
    }
}

public class SecretDataViewCardTests
{
    private static SecretKeyItemViewModel TextEntry(string key, string plaintext)
        => new()
        {
            Key = key,
            ByteCount = plaintext.Length,
            IsText = true,
            Base64 = "",
            Value = null
        };

    private static SecretKeyItemViewModel BinaryEntry(string key, string base64, int byteCount)
        => new()
        {
            Key = key,
            ByteCount = byteCount,
            IsText = false,
            Base64 = base64,
            Value = null
        };

    [Fact]
    public async Task Text_key_masked_and_binary_shows_base64()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretDataViewCard>(
            parameters => parameters
                .Add(p => p.Entries, new[]
                {
                    TextEntry("password", "old-pass"),
                    BinaryEntry("blob", "kEyB0aW5yYQ==", 8)
                }));

        Assert.Contains("password", cut.Markup);
        Assert.Contains("secret-masked-value", cut.Markup);
        Assert.DoesNotContain("old-pass", cut.Markup);
        Assert.Contains("kEyB0aW5yYQ==", cut.Markup);
        Assert.Contains("二进制 · 8 字节", cut.Markup);
        Assert.Contains("文本 · 8 字节", cut.Markup);
    }

    [Fact]
    public async Task Reveal_shows_plaintext_then_hide_restores_mask()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretDataViewCard>(
            parameters => parameters
                .Add(p => p.Entries, new[] { TextEntry("password", "old-pass") })
                .Add(p => p.OnReveal, key => { Assert.Equal("password", key); return Task.FromResult<string?>("old-pass"); }));

        var reveal = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "查看明文");
        await cut.InvokeAsync(() => reveal.Find("button").Click());

        cut.WaitForState(() => cut.Markup.Contains("secret-revealed-value"));
        Assert.Contains("old-pass", cut.Markup);

        var hide = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "隐藏");
        await cut.InvokeAsync(() => hide.Find("button").Click());

        cut.WaitForState(() => !cut.Markup.Contains("secret-revealed-value"));
        Assert.DoesNotContain("old-pass", cut.Markup);
    }

    [Fact]
    public async Task Reveal_failure_keeps_mask_and_shows_business_message()
    {
        await using var ctx = new BunitHost();
        var snackbar = new Mock<ISnackbar>();
        ctx.Services.AddSingleton(snackbar.Object);
        ctx.Services.AddSingleton(NullLoggerFactory.Instance);
        ctx.Services.AddScoped<Web.Components.Common.ExceptionPresenter>();

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretDataViewCard>(
            parameters => parameters
                .Add(p => p.Entries, new[] { TextEntry("password", "old-pass") })
                .Add(p => p.OnReveal, _ => throw new ValidationException("「password」的占位符已失效，请刷新后重试")));

        var reveal = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "查看明文");
        await cut.InvokeAsync(() => reveal.Find("button").Click());

        cut.WaitForState(() => cut.Markup.Contains("secret-masked-value"));
        Assert.DoesNotContain("old-pass", cut.Markup);
        snackbar.Verify(s => s.Add("「password」的占位符已失效，请刷新后重试", Severity.Error, null, null), Times.Once);
    }

    [Fact]
    public async Task Empty_entries_shows_empty_state()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<Web.Components.Secrets.Shared.SecretDataViewCard>(
            parameters => parameters.Add(p => p.Entries, Array.Empty<SecretKeyItemViewModel>()));

        Assert.Contains("暂无键值", cut.Markup);
    }
}
