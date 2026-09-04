using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Components.Common;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ExceptionPresenterTests
{
    private readonly Mock<ISnackbar> snackbar = new();
    private readonly ExceptionPresenter presenter;

    public ExceptionPresenterTests()
    {
        presenter = new ExceptionPresenter(snackbar.Object, NullLogger<ExceptionPresenter>.Instance);
    }

    [Fact]
    public async Task HandleAsync_BusinessException_ShowsUserMessage_AsError()
    {
        var ex = new NotFoundException("集群 5 不存在");

        await presenter.HandleAsync(ex, "删除集群");

        snackbar.Verify(s => s.Add("集群 5 不存在", Severity.Error, null), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConflictException_ShowsWarning()
    {
        var ex = new ConflictException("资源已被他人修改,请刷新后重试");

        await presenter.HandleAsync(ex, "保存配置");

        snackbar.Verify(s => s.Add("资源已被他人修改,请刷新后重试", Severity.Warning, null), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnknownException_ShowsGenericMessage()
    {
        var ex = new InvalidOperationException("secret internal detail");

        await presenter.HandleAsync(ex, "保存");

        snackbar.Verify(s => s.Add("保存失败,请稍后重试", Severity.Error, null), Times.Once);
        snackbar.Verify(s => s.Add(It.Is<string>(m => m.Contains("secret")), It.IsAny<Severity>(), null), Times.Never);
    }
}