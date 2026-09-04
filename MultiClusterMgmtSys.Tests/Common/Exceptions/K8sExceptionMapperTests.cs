using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Common.Exceptions;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Common.Exceptions;

public class K8sExceptionMapperTests
{
    private static Exception StatusException(int code, string? message = null)
    {
        return new KubernetesException(new V1Status
        {
            Code = code,
            Reason = "TestReason",
            Message = message
        });
    }

    [Theory]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(403)]
    [InlineData(401)]
    [InlineData(400)]
    public void Translate_MapsKnownStatusCodes_ToBusinessExceptions(int code)
    {
        var result = K8sExceptionMapper.Translate(StatusException(code), "测试操作");

        Assert.True(result is BusinessException);
    }

    [Fact]
    public void Translate_404_BecomesNotFound_WithOperationMessage()
    {
        var result = K8sExceptionMapper.Translate(StatusException(404), "删除配置");

        var ex = Assert.IsType<NotFoundException>(result);
        Assert.Contains("删除配置", ex.UserMessage);
        Assert.Contains("资源不存在或已被删除", ex.UserMessage);
    }

    [Fact]
    public void Translate_409_BecomesConflict()
    {
        var result = K8sExceptionMapper.Translate(StatusException(409), "保存配置");

        var ex = Assert.IsType<ConflictException>(result);
        Assert.Contains("资源已被他人修改", ex.UserMessage);
    }

    [Fact]
    public void Translate_403_BecomesPermission()
    {
        var result = K8sExceptionMapper.Translate(StatusException(403), "删除配置");

        Assert.IsType<PermissionException>(result);
    }

    [Fact]
    public void Translate_401_BecomesPermission()
    {
        var result = K8sExceptionMapper.Translate(StatusException(401), "删除配置");

        Assert.IsType<PermissionException>(result);
    }

    [Fact]
    public void Translate_400_UsesApiMessage_WhenPresent()
    {
        var result = K8sExceptionMapper.Translate(StatusException(400, "字段 label 必填"), "创建配置");

        var ex = Assert.IsType<ValidationException>(result);
        Assert.Equal("字段 label 必填", ex.UserMessage);
    }

    [Fact]
    public void Translate_400_FallsBackToGeneric_WhenApiMessageMissing()
    {
        var result = K8sExceptionMapper.Translate(StatusException(400), "创建配置");

        var ex = Assert.IsType<ValidationException>(result);
        Assert.Contains("请求参数不合法", ex.UserMessage);
    }

    [Fact]
    public void Translate_5xx_ReturnsOriginal_AsSystemException()
    {
        var original = StatusException(500, "internal error");

        var result = K8sExceptionMapper.Translate(original, "刷新状态");

        Assert.Same(original, result);
        Assert.IsNotType<BusinessException>(result);
    }

    [Fact]
    public void Translate_UnknownStatus_ReturnsOriginal()
    {
        var original = StatusException(999);

        var result = K8sExceptionMapper.Translate(original, "刷新状态");

        Assert.Same(original, result);
    }

    [Fact]
    public void Translate_Timeout_BecomesClusterUnreachable()
    {
        var result = K8sExceptionMapper.Translate(new TaskCanceledException(), "加载节点列表");

        var ex = Assert.IsType<ClusterUnreachableException>(result);
        Assert.Contains("集群连接失败或超时", ex.UserMessage);
    }

    [Fact]
    public void Translate_HttpRequestException_BecomesClusterUnreachable()
    {
        var result = K8sExceptionMapper.Translate(new HttpRequestException("connection refused"), "加载节点列表");

        Assert.IsType<ClusterUnreachableException>(result);
    }

    [Fact]
    public void Translate_NonK8sException_ReturnsOriginal()
    {
        var original = new InvalidOperationException("boom");

        var result = K8sExceptionMapper.Translate(original, "测试");

        Assert.Same(original, result);
    }
}