using System.ComponentModel;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Infrastructure.Helm;

/// <summary>
/// Helm CLI 运行器:为每次调用创建独立临时目录,自动物化 kubeconfig(chart 包与 values 由调用方提供),
/// 钉定 KUBECONFIG 与 HELM_* 到可写临时路径(容器内非 root、主目录不可写),解析参数表中的文件占位符,
/// 经进程执行器运行,并在 finally 清理目录、保证失败与取消同样回收(契约见 helm-cli-runtime spec)。
/// </summary>
public sealed class HelmCliRunner(
    IProcessExecutor executor,
    HelmOptions options,
    ILogger<HelmCliRunner> logger) : IHelmCliRunner
{
    private static readonly IReadOnlyDictionary<string, string> _placeholderFileNames = new Dictionary<string, string>
    {
        [HelmCliPlaceholders.KubeConfig] = HelmCliFileNames.KubeConfig,
        [HelmCliPlaceholders.ChartPackage] = HelmCliFileNames.ChartPackage,
        [HelmCliPlaceholders.Values] = HelmCliFileNames.Values
    };

    /// <inheritdoc />
    public async Task<HelmCliResult> RunAsync(HelmCliInvocation invocation, CancellationToken cancellationToken = default)
    {
        var workingDirectory = HelmTempDirectory.Create();
        try
        {
            var kubeConfigPath = Path.Combine(workingDirectory, HelmCliFileNames.KubeConfig);
            await File.WriteAllTextAsync(kubeConfigPath, HelmKubeConfigBuilder.Build(invocation.Cluster), cancellationToken);
            foreach (var file in invocation.Files)
            {
                await File.WriteAllBytesAsync(Path.Combine(workingDirectory, file.Name), file.Content, cancellationToken);
            }

            var arguments = ResolvePlaceholders(invocation.Arguments, workingDirectory);
            var environment = BuildEnvironment(workingDirectory, kubeConfigPath);
            var command = arguments.Count > 0 ? arguments[0] : "";
            logger.LogInformation("Run helm command={Command} clusterId={ClusterId}", command, invocation.Cluster.Id);

            ProcessExecutionResult result;
            try
            {
                result = await executor.RunAsync(
                    options.CliPath,
                    arguments,
                    environment,
                    workingDirectory,
                    invocation.Timeout,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
            {
                logger.LogWarning(ex, "Helm process failed to start cliPath={CliPath} clusterId={ClusterId}", options.CliPath, invocation.Cluster.Id);
                throw new HelmOperationException($"Helm CLI 不可用,请确认已安装 Helm 4 或正确配置 Helm:CliPath(当前:{options.CliPath})");
            }

            return new HelmCliResult
            {
                ExitCode = result.ExitCode,
                StandardOutput = result.StandardOutput,
                StandardError = result.StandardError,
                TimedOut = result.TimedOut
            };
        }
        finally
        {
            if (!HelmTempDirectory.TryDelete(workingDirectory))
            {
                logger.LogWarning("Failed to delete helm temp directory {Directory}", workingDirectory);
            }
        }
    }

    private static IReadOnlyList<string> ResolvePlaceholders(IReadOnlyList<string> arguments, string directory)
    {
        var resolved = new List<string>(arguments.Count);
        foreach (var argument in arguments)
        {
            if (_placeholderFileNames.TryGetValue(argument, out var fileName))
            {
                resolved.Add(Path.Combine(directory, fileName));
            }
            else
            {
                resolved.Add(argument);
            }
        }
        return resolved;
    }

    private static IReadOnlyDictionary<string, string> BuildEnvironment(string directory, string kubeConfigPath) =>
        new Dictionary<string, string>
        {
            ["KUBECONFIG"] = kubeConfigPath,
            ["HELM_CACHE_HOME"] = Path.Combine(directory, "cache"),
            ["HELM_CONFIG_HOME"] = Path.Combine(directory, "config"),
            ["HELM_DATA_HOME"] = Path.Combine(directory, "data"),
            ["HELM_REPOSITORY_CACHE"] = Path.Combine(directory, "repository-cache"),
            ["HELM_REPOSITORY_CONFIG"] = Path.Combine(directory, "repositories.yaml"),
            ["HELM_REGISTRY_CONFIG"] = Path.Combine(directory, "registry.json")
        };
}
