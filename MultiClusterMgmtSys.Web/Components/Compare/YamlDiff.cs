namespace MultiClusterMgmtSys.Web.Components.Compare;

/// <summary>
/// 双栏对照中一行的差异分类。
/// </summary>
public enum CompareDiffRowKind
{
    /// <summary>两侧一致的行。</summary>
    Same = 0,

    /// <summary>两侧都存在但内容不同的行(同一行位)。</summary>
    Changed = 1,

    /// <summary>仅源侧(左栏)存在的行。</summary>
    LeftOnly = 2,

    /// <summary>仅对照侧(右栏)存在的行。</summary>
    RightOnly = 3
}

/// <summary>
/// 对照差异中的一行:分类与两侧文本(不存在的一侧为 null)。
/// </summary>
/// <param name="Kind">行分类。</param>
/// <param name="LeftText">源侧(左栏)文本。</param>
/// <param name="RightText">对照侧(右栏)文本。</param>
public sealed record CompareDiffRow(CompareDiffRowKind Kind, string? LeftText, string? RightText);

/// <summary>
/// 纯函数 YAML 行级差异计算(最长公共子序列),不改既有契约、不含外部依赖;富客户端演示按行分类高亮。
/// </summary>
public static class YamlDiff
{
    /// <summary>比较两份 YAML 文本,产出逐行差异序列(空串或缺失的侧按空清单处理)。</summary>
    /// <param name="leftYaml">源侧(左栏)原始 YAML;null 或空视为不存在。</param>
    /// <param name="rightYaml">对照侧(右栏)原始 YAML;null 或空视为不存在。</param>
    /// <returns>逐行差异序列。</returns>
    public static IReadOnlyList<CompareDiffRow> Compute(string? leftYaml, string? rightYaml)
    {
        var left = SplitLines(leftYaml);
        var right = SplitLines(rightYaml);

        var rows = new List<CompareDiffRow>(left.Length + right.Length);
        int prevL = 0, prevR = 0;
        foreach (var (li, lj) in WalkCommonSubsequence(left, right))
        {
            AppendGap(rows, left, prevL, li, right, prevR, lj);
            rows.Add(new CompareDiffRow(CompareDiffRowKind.Same, left[li], right[lj]));
            prevL = li + 1;
            prevR = lj + 1;
        }

        AppendGap(rows, left, prevL, left.Length, right, prevR, right.Length);

        return rows;
    }

    /// <summary>统计差异行数(Same 以外的行全部计为差异)。</summary>
    /// <param name="rows">差异序列。</param>
    /// <returns>差异行数。</returns>
    public static int CountChanges(IReadOnlyList<CompareDiffRow> rows)
        => rows.Count(r => r.Kind != CompareDiffRowKind.Same);

    /// <summary>输出两个公共行锚点之间的间隙:变化行按行位配成「变化行对」,单侧剩余行按各自分类输出。</summary>
    /// <param name="rows">输出行序列。</param>
    /// <param name="left">左侧行。</param>
    /// <param name="leftStart">左侧间隙起点(含)。</param>
    /// <param name="leftEnd">左侧间隙终点(不含)。</param>
    /// <param name="right">右侧行。</param>
    /// <param name="rightStart">右侧间隙起点(含)。</param>
    /// <param name="rightEnd">右侧间隙终点(不含)。</param>
    private static void AppendGap(
        List<CompareDiffRow> rows,
        string[] left, int leftStart, int leftEnd,
        string[] right, int rightStart, int rightEnd)
    {
        var paired = Math.Min(leftEnd - leftStart, rightEnd - rightStart);
        for (int k = 0; k < paired; k++)
        {
            rows.Add(new CompareDiffRow(CompareDiffRowKind.Changed, left[leftStart + k], right[rightStart + k]));
        }

        for (int i = leftStart + paired; i < leftEnd; i++)
        {
            rows.Add(new CompareDiffRow(CompareDiffRowKind.LeftOnly, left[i], null));
        }

        for (int j = rightStart + paired; j < rightEnd; j++)
        {
            rows.Add(new CompareDiffRow(CompareDiffRowKind.RightOnly, null, right[j]));
        }
    }

    /// <summary>切割为行(统一换行符,去掉文末多余空行)。</summary>
    /// <param name="yaml">原始 YAML 文本。</param>
    /// <returns>行数组。</returns>
    private static string[] SplitLines(string? yaml)
    {
        if (string.IsNullOrEmpty(yaml))
        {
            return [];
        }

        var normalized = yaml.Replace("\r\n", "\n");
        if (normalized.EndsWith('\n'))
        {
            normalized = normalized[..^1];
        }

        return normalized.Split('\n');
    }

    /// <summary>基于最长公共子序列按顺序回访两侧对齐的行位(i 是左侧行号,j 是右侧行号,均为升序)。</summary>
    /// <param name="left">左侧行。</param>
    /// <param name="right">右侧行。</param>
    /// <returns>公共行的 (左行号, 右行号) 序列。</returns>
    private static IEnumerable<(int Li, int Lj)> WalkCommonSubsequence(string[] left, string[] right)
    {
        var lengths = new int[left.Length + 1, right.Length + 1];
        for (int i = left.Length - 1; i >= 0; i--)
        {
            for (int j = right.Length - 1; j >= 0; j--)
            {
                lengths[i, j] = StringComparer.Ordinal.Equals(left[i], right[j])
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        int x = 0, y = 0;
        while (x < left.Length && y < right.Length)
        {
            if (StringComparer.Ordinal.Equals(left[x], right[y]))
            {
                yield return (x, y);
                x++;
                y++;
            }
            else if (lengths[x + 1, y] >= lengths[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }
    }
}
