using System.Net;
using k8s;
using k8s.Autorest;
using k8s.Exceptions;

namespace MultiClusterMgmtSys.Common.Exceptions;

/// <summary>
/// 将 Kubernetes 客户端异常翻译为业务异常。
/// 仅按明确状态码(400/401/403/404/409)映射;超时/连接失败映射为集群不可达;
/// 5xx 与未知状态原样返回,交由上层按系统异常处理。
/// </summary>
public static class K8sExceptionMapper
{
    public static Exception Translate(Exception ex, string operation)
    {
        // KubernetesClient 19 将带状态码的错误抛为 KubernetesException(V1Status)。
        if (ex is KubernetesException k8s && k8s.Status?.Code is int code)
        {
            return MapStatus(code, k8s.Status.Message, operation, ex);
        }

        // 旧式 Autorest HTTP 包装(Response.StatusCode)。
        if (ex is HttpOperationException http && http.Response is not null)
        {
            return MapStatus((int)http.Response.StatusCode, null, operation, ex);
        }

        if (ex is TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            return new ClusterUnreachableException("集群连接失败或超时,请稍后重试");
        }

        return ex;
    }

    private static Exception MapStatus(int code, string? apiMessage, string operation, Exception original) => code switch
    {
        404 => new NotFoundException($"{operation}:资源不存在或已被删除"),
        409 => new ConflictException("资源已被他人修改,请刷新后重试"),
        403 => new PermissionException("没有权限执行该操作"),
        401 => new PermissionException("认证失效,请重新登录"),
        400 => new ValidationException(FirstNonEmpty(apiMessage, $"{operation}:请求参数不合法")),
        _ => original,
    };

    private static string FirstNonEmpty(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}