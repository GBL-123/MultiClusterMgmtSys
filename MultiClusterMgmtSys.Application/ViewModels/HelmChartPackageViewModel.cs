namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>上传 chart 包的解析结果(安装/升级对话框展示与预填用)。</summary>
public class HelmChartPackageViewModel
{
    /// <summary>chart 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>chart 版本。</summary>
    public string Version { get; set; } = "";

    /// <summary>chart 的 appVersion,可为空字符串。</summary>
    public string AppVersion { get; set; } = "";

    /// <summary>chart 描述,可为空字符串。</summary>
    public string Description { get; set; } = "";

    /// <summary>Chart.yaml 声明的依赖名称列表。</summary>
    public IReadOnlyList<string> Dependencies { get; set; } = [];

    /// <summary>包内 values.yaml 文本(编辑器初值);缺省时为空字符串。</summary>
    public string ValuesYaml { get; set; } = "";

    /// <summary>解析警告(如声明依赖但未打包 charts/);无警告时为空列表。</summary>
    public IReadOnlyList<string> Warnings { get; set; } = [];

    /// <summary>建议的 release 名称(默认取 chart 名称)。</summary>
    public string SuggestedReleaseName { get; set; } = "";
}
