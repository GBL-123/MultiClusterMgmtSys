using System.ComponentModel;
using Microsoft.Extensions.Logging.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure.Helm;

namespace MultiClusterMgmtSys.Tests.Infrastructure.Helm;

public class HelmCliRunnerTests
{
    [Fact]
    public async Task RunAsync_materializes_files_and_resolves_placeholders()
    {
        CapturedInvocation? captured = null;
        Dictionary<string, byte[]>? filesAtRun = null;
        var executor = new StubExecutor((invocation, _) =>
        {
            captured = invocation;
            filesAtRun = Directory.EnumerateFiles(invocation.WorkingDirectory)
                .ToDictionary(path => Path.GetFileName(path), File.ReadAllBytes);
            return Task.FromResult(new ProcessExecutionResult(0, "ok", "", false));
        });
        var runner = CreateRunner(executor);
        var chartContent = new byte[] { 1, 2, 3 };

        var result = await runner.RunAsync(new HelmCliInvocation
        {
            Arguments = ["install", "nginx", HelmCliPlaceholders.ChartPackage, "-n", "web", "--kubeconfig", HelmCliPlaceholders.KubeConfig],
            Files = [new HelmCliFile(HelmCliFileNames.ChartPackage, chartContent)],
            Cluster = NewTokenCluster(),
            Timeout = TimeSpan.FromSeconds(30)
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(captured);
        Assert.Equal("helm", captured!.FileName);
        Assert.Equal(TimeSpan.FromSeconds(30), captured.Timeout);
        Assert.Equal(2, captured.Arguments.Count(Path.IsPathRooted));
        Assert.Equal(chartContent, filesAtRun![HelmCliFileNames.ChartPackage]);
        Assert.Contains("token: \"tok\"", System.Text.Encoding.UTF8.GetString(filesAtRun[HelmCliFileNames.KubeConfig]));
        Assert.Equal(Path.Combine(captured.WorkingDirectory, HelmCliFileNames.KubeConfig), captured.Environment["KUBECONFIG"]);
        foreach (var key in new[]
                 {
                     "HELM_CACHE_HOME", "HELM_CONFIG_HOME", "HELM_DATA_HOME",
                     "HELM_REPOSITORY_CACHE", "HELM_REPOSITORY_CONFIG", "HELM_REGISTRY_CONFIG"
                 })
        {
            Assert.StartsWith(captured.WorkingDirectory, captured.Environment[key]);
        }
        Assert.False(Directory.Exists(captured.WorkingDirectory));
    }

    [Fact]
    public async Task RunAsync_cleans_temp_directory_when_command_fails()
    {
        CapturedInvocation? captured = null;
        var executor = new StubExecutor((invocation, _) =>
        {
            captured = invocation;
            return Task.FromResult(new ProcessExecutionResult(1, "", "boom", false));
        });
        var runner = CreateRunner(executor);

        var result = await runner.RunAsync(NewInvocation(), TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("boom", result.StandardError);
        Assert.False(Directory.Exists(captured!.WorkingDirectory));
    }

    [Fact]
    public async Task RunAsync_propagates_cancel_and_cleans_temp_directory()
    {
        CapturedInvocation? captured = null;
        var started = new TaskCompletionSource();
        var executor = new StubExecutor(async (invocation, token) =>
        {
            captured = invocation;
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return new ProcessExecutionResult(0, "", "", false);
        });
        var runner = CreateRunner(executor);
        using var cancellationSource = new CancellationTokenSource();

        var runTask = runner.RunAsync(NewInvocation(), cancellationSource.Token);
        await started.Task;
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.False(Directory.Exists(captured!.WorkingDirectory));
    }

    [Fact]
    public async Task RunAsync_reports_timeout_result()
    {
        var executor = new StubExecutor((_, _) => Task.FromResult(new ProcessExecutionResult(-1, "", "", true)));
        var runner = CreateRunner(executor);

        var result = await runner.RunAsync(NewInvocation(), TestContext.Current.CancellationToken);

        Assert.True(result.TimedOut);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task RunAsync_maps_process_start_failure_to_business_exception()
    {
        var executor = new StubExecutor((_, _) => Task.FromException<ProcessExecutionResult>(new Win32Exception("not found")));
        var runner = CreateRunner(executor, cliPath: "/missing/helm");

        var exception = await Assert.ThrowsAsync<HelmOperationException>(
            () => runner.RunAsync(NewInvocation(), TestContext.Current.CancellationToken));

        Assert.Contains("Helm CLI 不可用", exception.UserMessage);
        Assert.Contains("/missing/helm", exception.UserMessage);
    }

    private static HelmCliRunner CreateRunner(IProcessExecutor executor, string cliPath = "helm")
        => new(executor, new HelmOptions { CliPath = cliPath }, NullLogger<HelmCliRunner>.Instance);

    private static ClusterInfo NewTokenCluster() => new()
    {
        Id = 7,
        Name = "prod",
        ConnectionType = ConnectionType.Token,
        ApiServer = "https://api.example:6443",
        Token = "tok",
        SkipTlsVerify = true
    };

    private static HelmCliInvocation NewInvocation() => new()
    {
        Arguments = ["list", "-A", "-o", "json", "--kubeconfig", HelmCliPlaceholders.KubeConfig],
        Cluster = NewTokenCluster(),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private sealed class StubExecutor(Func<CapturedInvocation, CancellationToken, Task<ProcessExecutionResult>> handler) : IProcessExecutor
    {
        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            IReadOnlyDictionary<string, string> environment,
            string workingDirectory,
            TimeSpan timeout,
            CancellationToken cancellationToken)
            => handler(new CapturedInvocation(fileName, arguments, environment, workingDirectory, timeout), cancellationToken);
    }

    private sealed record CapturedInvocation(
        string FileName,
        IReadOnlyList<string> Arguments,
        IReadOnlyDictionary<string, string> Environment,
        string WorkingDirectory,
        TimeSpan Timeout);
}
