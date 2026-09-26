using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmErrorTranslatorTests
{
    [Fact]
    public void Translate_maps_conflict_output()
    {
        var translated = HelmErrorTranslator.Translate(Failed(HelmFixtures.ConflictError));

        var conflict = Assert.IsType<ConflictException>(translated);
        Assert.Contains("已存在同名 release", conflict.UserMessage);
        Assert.DoesNotContain("cannot re-use", conflict.UserMessage);
    }

    [Fact]
    public void Translate_maps_not_found_output()
    {
        var translated = HelmErrorTranslator.Translate(Failed(HelmFixtures.NotFoundError));

        var notFound = Assert.IsType<NotFoundException>(translated);
        Assert.Contains("Release 不存在", notFound.UserMessage);
    }

    [Fact]
    public void Translate_maps_permission_output()
    {
        var translated = HelmErrorTranslator.Translate(Failed(HelmFixtures.ForbiddenError));

        var permission = Assert.IsType<PermissionException>(translated);
        Assert.Contains("没有权限", permission.UserMessage);
    }

    [Fact]
    public void Translate_maps_unreachable_output()
    {
        var translated = HelmErrorTranslator.Translate(Failed(HelmFixtures.UnreachableError));

        var unreachable = Assert.IsType<ClusterUnreachableException>(translated);
        Assert.Contains("无法连接目标集群", unreachable.UserMessage);
        Assert.DoesNotContain("dial tcp", unreachable.UserMessage);
    }

    [Fact]
    public void Translate_maps_unclassified_output_to_helm_operation_exception()
    {
        var translated = HelmErrorTranslator.Translate(Failed(HelmFixtures.TemplateError));

        var operation = Assert.IsType<HelmOperationException>(translated);
        Assert.Contains("Helm 操作失败", operation.UserMessage);
        Assert.DoesNotContain("nil pointer", operation.UserMessage);
    }

    [Fact]
    public void Translate_maps_timeout_result()
    {
        var translated = HelmErrorTranslator.Translate(new HelmCliResult { ExitCode = -1, TimedOut = true });

        var unreachable = Assert.IsType<ClusterUnreachableException>(translated);
        Assert.Contains("超时", unreachable.UserMessage);
    }

    [Fact]
    public void Translate_searches_standard_output_too()
    {
        var translated = HelmErrorTranslator.Translate(new HelmCliResult
        {
            ExitCode = 1,
            StandardOutput = HelmFixtures.NotFoundError
        });

        Assert.IsType<NotFoundException>(translated);
    }

    private static HelmCliResult Failed(string standardError) => new()
    {
        ExitCode = 1,
        StandardError = standardError
    };
}
