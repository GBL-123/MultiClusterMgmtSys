using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// Helm CLI 执行端口:把一次 helm 调用(参数表 + 待物化文件 + 目标集群)交给基础设施层执行。
/// 实现负责临时目录、凭据物化、环境隔离、进程级超时与取消;调用方只关心参数与结果,
/// 非零退出码原样返回,由上层错误翻译器处理(契约见 helm-cli-runtime spec)。
/// </summary>
public interface IHelmCliRunner
{
    /// <summary>执行一次 helm 调用并返回退出码与输出;进程启动失败(二进制缺失或不可执行)抛 <see cref="HelmOperationException"/>。</summary>
    /// <param name="invocation">参数表、待物化文件、目标集群与进程级超时。</param>
    /// <param name="cancellationToken">取消令牌;取消时终止进程树并上抛取消。</param>
    /// <returns>退出码、标准输出、标准错误与是否超时。</returns>
    Task<HelmCliResult> RunAsync(HelmCliInvocation invocation, CancellationToken cancellationToken = default);
}
