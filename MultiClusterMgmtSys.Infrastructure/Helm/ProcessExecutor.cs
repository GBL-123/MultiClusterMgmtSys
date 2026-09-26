using System.ComponentModel;
using System.Diagnostics;

namespace MultiClusterMgmtSys.Infrastructure.Helm;

/// <summary>
/// 基于 <see cref="Process"/> 的真实进程执行器:参数表启动(无 shell 拼接)、异步读取标准输出与标准错误、
/// 超时或取消时终止整个进程树;进程无法启动时抛 <see cref="Win32Exception"/> 交由上层翻译。
/// </summary>
public sealed class ProcessExecutor : IProcessExecutor
{
    /// <inheritdoc />
    public async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        string workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new Win32Exception($"无法启动进程:{fileName}");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            await TryWaitForExitAsync(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            return new ProcessExecutionResult(-1, await SafeReadAsync(standardOutputTask), await SafeReadAsync(standardErrorTask), TimedOut: true);
        }

        return new ProcessExecutionResult(process.ExitCode, await standardOutputTask, await standardErrorTask, TimedOut: false);
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // 进程可能已退出;终止失败不掩盖超时结论
        }
    }

    private static async Task TryWaitForExitAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync();
        }
        catch
        {
            // 等待已被终止的进程退出失败无须上抛
        }
    }

    private static async Task<string> SafeReadAsync(Task<string> readTask)
    {
        try
        {
            return await readTask;
        }
        catch
        {
            return "";
        }
    }
}
