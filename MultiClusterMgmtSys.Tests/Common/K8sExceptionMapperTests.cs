using System.Net;
using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Common;

public class K8sExceptionMapperTests
{
    [Theory]
    [InlineData(404, typeof(NotFoundException))]
    [InlineData(409, typeof(ConflictException))]
    [InlineData(403, typeof(PermissionException))]
    [InlineData(401, typeof(PermissionException))]
    public void Translate_maps_status_code_to_business_exception(int code, Type expected)
    {
        var translated = K8sExceptionMapper.Translate(K8sMocks.K8sError(code), "操作");

        Assert.IsType(expected, translated);
        Assert.IsAssignableFrom<BusinessException>(translated);
    }

    [Fact]
    public void Translate_404_uses_chinese_user_message()
    {
        var translated = K8sExceptionMapper.Translate(K8sMocks.K8sError(404), "删除集群");

        var business = Assert.IsType<NotFoundException>(translated);
        Assert.Equal("删除集群:资源不存在或已被删除", business.UserMessage);
    }

    [Fact]
    public void Translate_400_uses_api_message_when_present()
    {
        var translated = K8sExceptionMapper.Translate(
            K8sMocks.K8sError(400, "spec.replicas must be positive"), "扩缩容");

        var business = Assert.IsType<ValidationException>(translated);
        Assert.Equal("spec.replicas must be positive", business.UserMessage);
    }

    [Fact]
    public void Translate_400_falls_back_to_operation_when_api_message_empty()
    {
        var translated = K8sExceptionMapper.Translate(K8sMocks.K8sError(400, "  "), "创建");

        var business = Assert.IsType<ValidationException>(translated);
        Assert.Equal("创建:请求参数不合法", business.UserMessage);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    public void Translate_returns_original_for_server_errors(int code)
    {
        var original = K8sMocks.K8sError(code);

        var translated = K8sExceptionMapper.Translate(original, "操作");

        Assert.Same(original, translated);
    }

    [Fact]
    public void Translate_returns_original_for_unknown_status()
    {
        var original = K8sMocks.K8sError(418);

        var translated = K8sExceptionMapper.Translate(original, "操作");

        Assert.Same(original, translated);
    }

    [Fact]
    public void Translate_maps_task_canceled_to_unreachable()
    {
        var translated = K8sExceptionMapper.Translate(
            new TaskCanceledException("timeout"), "连接");

        var business = Assert.IsType<ClusterUnreachableException>(translated);
        Assert.Contains("超时", business.UserMessage);
    }

    [Fact]
    public void Translate_maps_http_request_exception_to_unreachable()
    {
        var translated = K8sExceptionMapper.Translate(
            new HttpRequestException("connection refused"), "连接");

        Assert.IsType<ClusterUnreachableException>(translated);
    }

    [Fact]
    public void Translate_maps_http_operation_response_status()
    {
        var original = new HttpOperationException("Get")
        {
            Response = new HttpResponseMessageWrapper(
                new HttpResponseMessage(HttpStatusCode.Conflict), "")
        };

        var translated = K8sExceptionMapper.Translate(original, "更新");

        var business = Assert.IsType<ConflictException>(translated);
        Assert.IsAssignableFrom<BusinessException>(business);
    }

    [Fact]
    public void Translate_passes_through_unrelated_exception()
    {
        var original = new InvalidOperationException("boom");

        var translated = K8sExceptionMapper.Translate(original, "操作");

        Assert.Same(original, translated);
    }

    [Fact]
    public void KubernetesException_without_status_passes_through()
    {
        var original = new KubernetesException("no status");

        var translated = K8sExceptionMapper.Translate(original, "操作");

        Assert.Same(original, translated);
    }
}
