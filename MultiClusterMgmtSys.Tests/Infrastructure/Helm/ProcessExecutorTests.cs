using System.ComponentModel;
using MultiClusterMgmtSys.Infrastructure.Helm;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Helm;

public class ProcessExecutorTests
{
    [Fact]
    public async Task RunAsync_captures_output_and_exit_code()
    {
        var executor = new ProcessExecutor();

        var result = await executor.RunAsync(
            "dotnet",
            ["--version"],
            new Dictionary<string, string>(),
            Path.GetTempPath(),
            TimeSpan.FromSeconds(60),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(".", result.StandardOutput);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_terminates_process_tree_on_timeout()
    {
        var (fileName, arguments) = SleepCommand();
        var executor = new ProcessExecutor();

        var result = await executor.RunAsync(
            fileName,
            arguments,
            new Dictionary<string, string>(),
            Path.GetTempPath(),
            TimeSpan.FromMilliseconds(300),
            TestContext.Current.CancellationToken);

        Assert.True(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_propagates_cancellation()
    {
        var (fileName, arguments) = SleepCommand();
        var executor = new ProcessExecutor();
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.RunAsync(
            fileName,
            arguments,
            new Dictionary<string, string>(),
            Path.GetTempPath(),
            TimeSpan.FromSeconds(60),
            cancellationSource.Token));
    }

    [Fact]
    public async Task RunAsync_throws_win32_exception_when_executable_missing()
    {
        var executor = new ProcessExecutor();

        await Assert.ThrowsAsync<Win32Exception>(() => executor.RunAsync(
            "mcm-definitely-missing-binary",
            [],
            new Dictionary<string, string>(),
            Path.GetTempPath(),
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken));
    }

    private static (string FileName, IReadOnlyList<string> Arguments) SleepCommand()
        => OperatingSystem.IsWindows()
            ? ("powershell", ["-NoProfile", "-Command", "Start-Sleep -Seconds 30"])
            : ("/bin/sleep", ["30"]);
}
