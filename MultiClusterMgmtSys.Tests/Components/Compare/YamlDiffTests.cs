using MultiClusterMgmtSys.Web.Components.Compare;

namespace MultiClusterMgmtSys.Tests.Components.Compare;

/// <summary>Web 层纯函数 YamlDiff 的行为回归。</summary>
public class YamlDiffTests
{
    [Fact]
    public void Compute_identical_lines_are_same()
    {
        var rows = YamlDiff.Compute("a\nb", "a\nb");

        Assert.All(rows, r => Assert.Equal(CompareDiffRowKind.Same, r.Kind));
        Assert.Equal(0, YamlDiff.CountChanges(rows));
    }

    [Fact]
    public void Compute_changed_lines_align_as_changed_pairs()
    {
        var rows = YamlDiff.Compute("a: 1\nb: 1", "a: 2\nb: 1");

        Assert.Equal(2, rows.Count);
        Assert.Equal(CompareDiffRowKind.Changed, rows[0].Kind);
        Assert.Equal("a: 1", rows[0].LeftText);
        Assert.Equal("a: 2", rows[0].RightText);
        Assert.Equal(CompareDiffRowKind.Same, rows[1].Kind);
        Assert.Equal(1, YamlDiff.CountChanges(rows));
    }

    [Fact]
    public void Compute_missing_side_marks_only_rows()
    {
        var rows = YamlDiff.Compute(null, "x\ny");

        Assert.All(rows, r => Assert.Equal(CompareDiffRowKind.RightOnly, r.Kind));
        Assert.Equal(2, YamlDiff.CountChanges(rows));
    }

    [Fact]
    public void Compute_inserted_and_removed_lines_are_positional()
    {
        var rows = YamlDiff.Compute("a\nb\nc", "a\nz\nc");

        Assert.Equal(CompareDiffRowKind.Same, rows[0].Kind);
        Assert.Equal(CompareDiffRowKind.Changed, rows[1].Kind);
        Assert.Equal("b", rows[1].LeftText);
        Assert.Equal("z", rows[1].RightText);
        Assert.Equal(CompareDiffRowKind.Same, rows[2].Kind);
    }

    [Fact]
    public void Compute_crlf_and_trailing_newline_normalize()
    {
        var rows = YamlDiff.Compute("a\r\nb\r\n", "a\nb");

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(CompareDiffRowKind.Same, r.Kind));
    }
}
