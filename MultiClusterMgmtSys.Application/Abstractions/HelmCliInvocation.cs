using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>一次 Helm CLI 调用的输入。</summary>
public sealed record HelmCliInvocation
{
    /// <summary>helm 子命令与参数表(不含可执行文件本身);与 <see cref="HelmCliPlaceholders"/> 常量完全相等的参数会被实现替换为本次临时目录中的绝对路径。</summary>
    public required IReadOnlyList<string> Arguments { get; init; }

    /// <summary>需要物化到本次临时目录的文件(约定名见 <see cref="HelmCliFileNames"/>);kubeconfig 由实现按 <see cref="Cluster"/> 自动物化,不需要在此提供。</summary>
    public IReadOnlyList<HelmCliFile> Files { get; init; } = [];

    /// <summary>目标集群,供实现物化 kubeconfig。</summary>
    public required ClusterInfo Cluster { get; init; }

    /// <summary>进程级超时上限;超过即终止进程树并返回超时结果。</summary>
    public required TimeSpan Timeout { get; init; }
}

/// <summary>需要物化到本次临时目录的文件。</summary>
/// <param name="Name">约定文件名,取值见 <see cref="HelmCliFileNames"/>。</param>
/// <param name="Content">文件内容(二进制安全)。</param>
public sealed record HelmCliFile(string Name, byte[] Content);

/// <summary>物化文件的约定名;参数表经占位符引用其绝对路径。</summary>
public static class HelmCliFileNames
{
    /// <summary>kubeconfig 文件名(由运行器自动物化)。</summary>
    public const string KubeConfig = "kubeconfig";

    /// <summary>上传的 chart 包(.tgz)文件名。</summary>
    public const string ChartPackage = "chart.tgz";

    /// <summary>values 文件名。</summary>
    public const string Values = "values.yaml";
}

/// <summary>参数表中引用物化文件的占位符:与占位符完全相等的参数会被替换为对应文件的绝对路径。</summary>
public static class HelmCliPlaceholders
{
    /// <summary>kubeconfig 绝对路径占位符。</summary>
    public const string KubeConfig = "{kubeconfig}";

    /// <summary>chart 包绝对路径占位符。</summary>
    public const string ChartPackage = "{chart}";

    /// <summary>values 文件绝对路径占位符。</summary>
    public const string Values = "{values}";
}
