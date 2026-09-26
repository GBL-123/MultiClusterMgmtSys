using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Helm;

/// <summary>
/// Helm CLI 失败翻译:按进程结果中的模式映射到既有业务异常类别(冲突/未找到/权限/不可达/超时),
/// 无法归类时返回 <see cref="HelmOperationException"/>;原始输出不进入用户可见消息(仅由调用方记日志)。
/// </summary>
public static class HelmErrorTranslator
{
    /// <summary>翻译一次失败的 Helm 调用(非零退出码或进程级超时)。</summary>
    /// <param name="result">Helm CLI 结果。</param>
    /// <returns>可直接 throw 的业务异常。</returns>
    public static BusinessException Translate(HelmCliResult result)
    {
        if (result.TimedOut)
        {
            return new ClusterUnreachableException("Helm 操作超时,请检查目标集群状态");
        }

        var output = $"{result.StandardError}\n{result.StandardOutput}";
        if (ContainsAny(output, "cannot re-use", "already exists", "another release", "another operation"))
        {
            return new ConflictException("目标命名空间已存在同名 release,请更换名称或改用升级");
        }
        if (ContainsAny(output, "not found", "no releases found"))
        {
            return new NotFoundException("Release 不存在或已被删除");
        }
        if (ContainsAny(output, "forbidden", "unauthorized", "permission denied", "cannot create resource", "cannot patch resource", "cannot get resource"))
        {
            return new PermissionException("没有权限执行该操作(集群返回权限错误)");
        }
        if (ContainsAny(output, "unreachable", "connection refused", "no such host", "i/o timeout", "tls handshake", "dial tcp", "connection reset"))
        {
            return new ClusterUnreachableException("无法连接目标集群,请检查集群连通性与凭据");
        }
        return new HelmOperationException("Helm 操作失败,请检查 chart 包与目标集群状态");
    }

    private static bool ContainsAny(string text, params string[] patterns)
        => patterns.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
}
