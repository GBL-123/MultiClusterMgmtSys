using MultiClusterMgmtSys.Application.Abstractions;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>Helm CLI 运行器假实现:记录每次调用并按处理器返回结果,供 Helm 服务层单测使用;</summary>
public sealed class FakeHelmCliRunner : IHelmCliRunner
{
    /// <summary>按顺序记录的调用(含参数表、集群与超时)。</summary>
    public List<HelmCliInvocation> Invocations { get; } = [];

    /// <summary>结果处理器;默认返回成功且无输出。</summary>
    public Func<HelmCliInvocation, HelmCliResult> Handler { get; set; } = _ => Succeeded();

    /// <inheritdoc />
    public Task<HelmCliResult> RunAsync(HelmCliInvocation invocation, CancellationToken cancellationToken = default)
    {
        Invocations.Add(invocation);
        return Task.FromResult(Handler(invocation));
    }

    /// <summary>构造成功结果。</summary>
    /// <param name="standardOutput">标准输出。</param>
    /// <returns>退出码为 0 的结果。</returns>
    public static HelmCliResult Succeeded(string standardOutput = "") => new()
    {
        ExitCode = 0,
        StandardOutput = standardOutput
    };

    /// <summary>构造失败结果。</summary>
    /// <param name="standardError">标准错误。</param>
    /// <param name="exitCode">退出码。</param>
    /// <returns>非零退出码的结果。</returns>
    public static HelmCliResult Failed(string standardError, int exitCode = 1) => new()
    {
        ExitCode = exitCode,
        StandardError = standardError
    };
}
