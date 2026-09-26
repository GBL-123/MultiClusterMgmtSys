namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>一次 Helm CLI 调用的结果;非零退出码不做翻译,由上层错误翻译器处理。</summary>
public sealed record HelmCliResult
{
    /// <summary>进程退出码;被终止时为终止符对应的非零值。</summary>
    public required int ExitCode { get; init; }

    /// <summary>标准输出。</summary>
    public string StandardOutput { get; init; } = "";

    /// <summary>标准错误。</summary>
    public string StandardError { get; init; } = "";

    /// <summary>是否因进程级超时被终止。</summary>
    public bool TimedOut { get; init; }

    /// <summary>调用是否成功(退出码为 0 且未超时)。</summary>
    public bool Succeeded => ExitCode == 0 && !TimedOut;
}
