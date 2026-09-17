using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MultiClusterMgmtSys.Web.Components.Common;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Common;

public class DisplayComponentTests
{
    [Fact]
    public async Task StackedText_renders_primary_and_secondary()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render<StackedText>(p => p
            .Add(x => x.Primary, "就绪")
            .Add(x => x.Secondary, "Ready"));

        Assert.Contains("就绪", cut.Markup);
        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("stacked-text-secondary", cut.Markup);
    }

    [Fact]
    public async Task StackedText_omits_secondary_when_empty()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render<StackedText>(p => p.Add(x => x.Primary, "就绪"));

        Assert.Contains("就绪", cut.Markup);
        Assert.DoesNotContain("stacked-text-secondary", cut.Markup);
    }

    [Fact]
    public async Task StatusBadge_renders_primary_and_raw_secondary()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render<StatusBadge>(p => p
            .Add(x => x.Text, "就绪")
            .Add(x => x.CssClass, "online")
            .Add(x => x.Raw, "Ready"));

        Assert.Contains("status-badge online", cut.Markup);
        Assert.Contains("status-dot", cut.Markup);
        Assert.Contains("就绪", cut.Markup);
        Assert.Contains("Ready", cut.Markup);
        Assert.Contains("status-badge-raw", cut.Markup);
    }

    [Fact]
    public async Task StatusBadge_omits_raw_line_when_absent()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render<StatusBadge>(p => p
            .Add(x => x.Text, "在线")
            .Add(x => x.CssClass, "online"));

        Assert.Contains("在线", cut.Markup);
        Assert.DoesNotContain("status-badge-raw", cut.Markup);
    }

    [Fact]
    public async Task TextTooltip_renders_mono_content_after_hover()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TextTooltip>(1);
            builder.AddAttribute(2, nameof(TextTooltip.Text), "3800m");
            builder.AddAttribute(3, nameof(TextTooltip.Mono), true);
            builder.AddAttribute(4, nameof(TextTooltip.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, "<span>3.8 核</span>")));
            builder.CloseComponent();
        });

        var tooltip = cut.FindComponent<MudTooltip>();
        Assert.True(tooltip.Instance.Arrow);

        cut.Find(".mud-tooltip-root").TriggerEvent("onpointerenter", new PointerEventArgs());
        cut.WaitForAssertion(() => Assert.True(tooltip.Instance.Visible));
        cut.WaitForAssertion(() => Assert.Contains("3800m", cut.Markup));
        Assert.Contains("font-mono", cut.Markup);
        Assert.Contains("3.8 核", cut.Markup);
    }

    [Fact]
    public async Task TextTooltip_plain_content_has_no_mono_class()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<TextTooltip>(1);
            builder.AddAttribute(2, nameof(TextTooltip.Text), "内置管理员不可修改");
            builder.AddAttribute(3, nameof(TextTooltip.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, "<span>锁定</span>")));
            builder.CloseComponent();
        });

        var tooltip = cut.FindComponent<MudTooltip>();
        cut.Find(".mud-tooltip-root").TriggerEvent("onpointerenter", new PointerEventArgs());
        cut.WaitForAssertion(() => Assert.True(tooltip.Instance.Visible));
        cut.WaitForAssertion(() => Assert.Contains("内置管理员不可修改", cut.Markup));
        Assert.DoesNotContain("font-mono", cut.Markup);
    }
}
