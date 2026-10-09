using Bunit;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using MultiClusterMgmtSys.Web.Components.Compare;
using MultiClusterMgmtSys.Web.Components.Compare.Shared;

namespace MultiClusterMgmtSys.Tests.Components.Compare;

public class CompareDiffViewTests
{
    private const string LeftYaml = "a: 1\nb: 1\n";

    private const string RightYaml = "a: 2\nb: 1\n";

    private static ComparePairViewModel Pair(bool sourceExists = true, bool targetExists = true, bool hasDifference = true)
        => new()
        {
            Namespace = "app",
            Name = "web",
            SourceClusterName = "alpha",
            TargetClusterName = "beta",
            SourceExists = sourceExists,
            SourceYaml = sourceExists ? LeftYaml : "",
            TargetExists = targetExists,
            TargetYaml = targetExists ? RightYaml : "",
            HasDifference = hasDifference
        };

    private static IRenderedComponent<CompareDiffView> RenderView(BunitContext ctx, ComparePairViewModel pair, IReadOnlyList<CompareDiffRow>? rows = null)
    {
        ctx.AddAuthorization();
        return ctx.Render<CompareDiffView>(parameters => parameters
            .Add(p => p.Pair, pair)
            .Add(p => p.Rows, rows ?? YamlDiff.Compute(pair.SourceYaml, pair.TargetYaml)));
    }

    [Fact]
    public async Task Renders_highlighted_rows_and_legend_by_default()
    {
        await using var ctx = new BunitHost();
        var cut = RenderView(ctx, Pair());

        Assert.Multiple(
            () => Assert.Equal(2, cut.FindAll(".compare-diff-row").Count),
            () => Assert.Equal(2, cut.FindAll(".compare-diff-row > span.is-changed").Count),
            () => Assert.Equal("a: 1", cut.Find(".compare-diff-row > span.is-changed").TextContent),
            () => Assert.Equal(3, cut.FindAll(".compare-diff-swatch").Count),
            () => Assert.Contains("仅看差异", cut.Markup),
            () => Assert.Contains("仅源", cut.Markup),
            () => Assert.Contains("仅对照", cut.Markup));
    }

    [Fact]
    public async Task Only_changed_toggle_hides_same_rows()
    {
        await using var ctx = new BunitHost();
        var cut = RenderView(ctx, Pair());

        cut.InvokeAsync(() => cut.Find("input.mud-switch-input").Change(true));

        var rows = cut.FindAll(".compare-diff-row");
        Assert.Single(rows);
        Assert.Contains("a: 2", cut.Markup);
        Assert.DoesNotContain("b: 1", cut.Markup);
    }

    [Fact]
    public async Task Identical_sides_show_notice_without_grid()
    {
        await using var ctx = new BunitHost();
        var cut = RenderView(ctx, Pair(hasDifference: false), YamlDiff.Compute(LeftYaml, LeftYaml));

        Assert.Contains("两侧内容一致", cut.Markup);
        Assert.DoesNotContain("compare-diff-grid", cut.Markup);
    }

    [Fact]
    public async Task Missing_target_shows_source_and_empty_state()
    {
        await using var ctx = new BunitHost();
        var cut = RenderView(ctx, Pair(targetExists: false, hasDifference: false));

        Assert.Contains("（不存在）", cut.Markup);
        Assert.Single(cut.FindAll(".empty-state.is-compact"));
        Assert.Contains("a: 1", cut.Markup);
        Assert.DoesNotContain("compare-diff-grid", cut.Markup);
    }

    [Fact]
    public async Task Missing_source_shows_empty_state_and_target()
    {
        await using var ctx = new BunitHost();
        var cut = RenderView(ctx, Pair(sourceExists: false, hasDifference: false));

        Assert.Contains("（不存在）", cut.Markup);
        Assert.Single(cut.FindAll(".empty-state.is-compact"));
        Assert.Contains("a: 2", cut.Markup);
    }
}
