namespace MultiClusterMgmtSys.Infrastructure.Helm;

/// <summary>
/// 进程执行薄封装:以参数表启动子进程、重定向输出、施加进程级超时并在超时/取消时终止进程树;
/// 命令构建与业务语义不在此层(便于以假实现驱动上层单测)。
/// </summary>
public interface IProcessExecutor
{
    /// <summary>执行外部命令。</summary>
    /// <param name="fileName">可执行文件路径或 PATH 中的名称。</param>
    /// <param name="arguments">参数表(不经 shell,逐个传递)。</param>
    /// <param name="environment">附加或覆盖的环境变量。</param>
    /// <param name="workingDirectory">工作目录。</param>
    /// <param name="timeout">进程级超时;超时终止进程树并返回超时结果。</param>
    /// <param name="cancellationToken">取消令牌;取消时终止进程树并上抛取消。</param>
    /// <returns>退出码、输出与是否超时。</returns>
    Task<ProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

/// <summary>一次进程执行的结果。</summary>
/// <param name="ExitCode">退出码;被终止时为终止符对应的非零值。</param>
/// <param name="StandardOutput">标准输出。</param>
/// <param name="StandardError">标准错误。</param>
/// <param name="TimedOut">是否因进程级超时被终止。</param>
public sealed record ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);
