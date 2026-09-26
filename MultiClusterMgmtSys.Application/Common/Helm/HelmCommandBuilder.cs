using System.Globalization;
using System.Text.RegularExpressions;
using MultiClusterMgmtSys.Application.Abstractions;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>
/// Helm 命令参数构建(纯函数):列表/详情/历史/values/manifest 读取与安装/升级/回滚/卸载的参数表。
/// 文件参数一律以占位符表达(<see cref="HelmCliPlaceholders"/>),由运行器物化为临时目录中的绝对路径;
/// 命令按 Helm 4 的 flag 名构建(契约见 helm-cli-runtime spec)。
/// </summary>
public static partial class HelmCommandBuilder
{
    /// <summary>构建 `helm list -A -o json` 参数表(全部命名空间)。</summary>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildList() => WithKubeConfig(["list", "-A", "-o", "json"]);

    /// <summary>构建 `helm status` 参数表(指定 release 与命名空间,JSON 输出)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildStatus(string releaseName, string namespaceName)
        => WithKubeConfig(["status", releaseName, "-n", namespaceName, "-o", "json"]);

    /// <summary>构建 `helm history` 参数表(指定 release 与命名空间,JSON 输出)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildHistory(string releaseName, string namespaceName)
        => WithKubeConfig(["history", releaseName, "-n", namespaceName, "-o", "json"]);

    /// <summary>构建 `helm get values` 参数表(用户提供的 values,以 YAML 文本输出)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildGetValues(string releaseName, string namespaceName)
        => WithKubeConfig(["get", "values", releaseName, "-n", namespaceName, "-o", "yaml"]);

    /// <summary>构建 `helm get manifest` 参数表(输出渲染后的 manifest 文本)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildGetManifest(string releaseName, string namespaceName)
        => WithKubeConfig(["get", "manifest", releaseName, "-n", namespaceName]);

    /// <summary>构建 `helm install` 参数表;chart 包与(可选)values 以占位符传入。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="includeValues">是否附加 values 文件(-f)。</param>
    /// <param name="createNamespace">是否附加 --create-namespace。</param>
    /// <param name="wait">是否等待就绪(--wait --timeout)。</param>
    /// <param name="timeoutSeconds">等待超时秒数。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildInstall(
        string releaseName,
        string namespaceName,
        bool includeValues,
        bool createNamespace,
        bool wait,
        int timeoutSeconds)
    {
        var arguments = new List<string> { "install", releaseName, HelmCliPlaceholders.ChartPackage, "-n", namespaceName };
        if (includeValues)
        {
            arguments.Add("-f");
            arguments.Add(HelmCliPlaceholders.Values);
        }
        if (createNamespace)
        {
            arguments.Add("--create-namespace");
        }
        AppendWait(arguments, wait, timeoutSeconds);
        return WithKubeConfig(arguments);
    }

    /// <summary>构建 `helm upgrade` 参数表;values 模式为二选一:`-f`(重新编辑)或 `--reuse-values`(沿用现存)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="includeValues">是否附加 values 文件(-f)。</param>
    /// <param name="reuseValues">是否沿用现存 values(--reuse-values)。</param>
    /// <param name="wait">是否等待就绪(--wait --timeout)。</param>
    /// <param name="timeoutSeconds">等待超时秒数。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildUpgrade(
        string releaseName,
        string namespaceName,
        bool includeValues,
        bool reuseValues,
        bool wait,
        int timeoutSeconds)
    {
        var arguments = new List<string> { "upgrade", releaseName, HelmCliPlaceholders.ChartPackage, "-n", namespaceName };
        if (includeValues)
        {
            arguments.Add("-f");
            arguments.Add(HelmCliPlaceholders.Values);
        }
        if (reuseValues)
        {
            arguments.Add("--reuse-values");
        }
        AppendWait(arguments, wait, timeoutSeconds);
        return WithKubeConfig(arguments);
    }

    /// <summary>构建 `helm rollback` 参数表(指定目标 revision)。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="revision">目标 revision。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildRollback(string releaseName, string namespaceName, int revision)
        => WithKubeConfig(["rollback", releaseName, revision.ToString(CultureInfo.InvariantCulture), "-n", namespaceName]);

    /// <summary>构建 `helm uninstall` 参数表。</summary>
    /// <param name="releaseName">release 名称。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="keepHistory">是否保留历史(--keep-history)。</param>
    /// <returns>参数表。</returns>
    public static IReadOnlyList<string> BuildUninstall(string releaseName, string namespaceName, bool keepHistory)
    {
        var arguments = new List<string> { "uninstall", releaseName, "-n", namespaceName };
        if (keepHistory)
        {
            arguments.Add("--keep-history");
        }
        return WithKubeConfig(arguments);
    }

    /// <summary>校验 release 名称符合 DNS-1123(小写字母/数字/连字符,首尾为字母或数字,长度不超过 53)。</summary>
    /// <param name="name">release 名称。</param>
    /// <returns>是否合法。</returns>
    public static bool IsValidReleaseName(string name)
        => !string.IsNullOrEmpty(name) && name.Length <= 53 && Dns1123Pattern().IsMatch(name);

    /// <summary>校验命名空间名称符合 DNS-1123(长度不超过 63)。</summary>
    /// <param name="name">命名空间名称。</param>
    /// <returns>是否合法。</returns>
    public static bool IsValidNamespace(string name)
        => !string.IsNullOrEmpty(name) && name.Length <= 63 && Dns1123Pattern().IsMatch(name);

    private static void AppendWait(List<string> arguments, bool wait, int timeoutSeconds)
    {
        if (!wait)
        {
            return;
        }
        arguments.Add("--wait");
        arguments.Add("--timeout");
        arguments.Add($"{timeoutSeconds}s");
    }

    private static IReadOnlyList<string> WithKubeConfig(List<string> arguments)
    {
        arguments.Add("--kubeconfig");
        arguments.Add(HelmCliPlaceholders.KubeConfig);
        return arguments;
    }

    [GeneratedRegex("^[a-z0-9]([-a-z0-9]*[a-z0-9])?$")]
    private static partial Regex Dns1123Pattern();
}
