namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>
/// Helm CLI 运行配置(appsettings.json 的 Helm 节),由组合根绑定为单例注入;
/// 两个配置项都有安全默认值,缺省或非法时回落默认,不阻止应用启动。
/// </summary>
public class HelmOptions
{
    /// <summary>helm 可执行文件路径;默认从 PATH 查找,开发机可指向本地安装路径。</summary>
    public string CliPath { get; set; } = "helm";

    /// <summary>上传 chart 包允许的最大字节数,默认 50MB;超出以中文校验错误拒绝。</summary>
    public long MaxPackageBytes { get; set; } = 52_428_800;

    /// <summary>按配置节的原始值构建选项:空白路径与非法/非正数上限回落默认值。</summary>
    /// <param name="cliPath">Helm:CliPath 原始值,可为 null。</param>
    /// <param name="maxPackageBytes">Helm:MaxPackageBytes 原始值,可为 null。</param>
    /// <returns>可直接注册为单例的选项对象。</returns>
    public static HelmOptions FromValues(string? cliPath, string? maxPackageBytes)
    {
        var options = new HelmOptions();
        if (!string.IsNullOrWhiteSpace(cliPath))
        {
            options.CliPath = cliPath;
        }
        if (long.TryParse(maxPackageBytes, out var max) && max > 0)
        {
            options.MaxPackageBytes = max;
        }
        return options;
    }
}
