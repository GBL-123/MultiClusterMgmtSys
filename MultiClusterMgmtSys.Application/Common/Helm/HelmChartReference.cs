namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>chart 引用(形如 `nginx-1.2.3`)的拆分结果。</summary>
/// <param name="Name">chart 名称。</param>
/// <param name="Version">chart 版本;无法识别时为空字符串。</param>
public readonly record struct HelmChartReference(string Name, string Version)
{
    /// <summary>
    /// 从 `name-version` 形式的 chart 引用拆分名称与版本:以最后一个连字符为界,
    /// 其后紧跟数字才视为版本,否则整体作为名称、版本为空。
    /// </summary>
    /// <param name="chart">chart 引用文本。</param>
    /// <returns>拆分结果。</returns>
    public static HelmChartReference Parse(string chart)
    {
        var text = chart ?? "";
        var separatorIndex = text.LastIndexOf('-');
        if (separatorIndex > 0 && separatorIndex < text.Length - 1 && char.IsAsciiDigit(text[separatorIndex + 1]))
        {
            return new HelmChartReference(text[..separatorIndex], text[(separatorIndex + 1)..]);
        }
        return new HelmChartReference(text, "");
    }
}
