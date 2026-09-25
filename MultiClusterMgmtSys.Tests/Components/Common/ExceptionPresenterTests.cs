using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Web.Components.Common;

namespace MultiClusterMgmtSys.Tests.Components.Common;

public class ExceptionPresenterTests
{
    private readonly Mock<ISnackbar> _snackbar = new();
    private readonly Mock<ILogger<ExceptionPresenter>> _logger = new();
    private readonly ExceptionPresenter _presenter;

    public ExceptionPresenterTests()
    {
        _presenter = new ExceptionPresenter(_snackbar.Object, _logger.Object);
    }

    [Fact]
    public async Task HandleAsync_conflict_shows_warning_with_user_message()
    {
        await _presenter.HandleAsync(new ConflictException("已被占用"), "操作");

        _snackbar.Verify(s => s.Add(
            "已被占用",
            Severity.Warning,
            It.IsAny<Action<SnackbarOptions>>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_not_found_shows_error_with_user_message()
    {
        await _presenter.HandleAsync(new NotFoundException("集群不存在"), "打开集群");

        _snackbar.Verify(s => s.Add(
            "集群不存在",
            Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_validation_shows_error_with_user_message()
    {
        await _presenter.HandleAsync(new ValidationException("备注超长"), "保存");

        _snackbar.Verify(s => s.Add(
            "备注超长",
            Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_non_business_shows_generic_message_and_logs_error()
    {
        await _presenter.HandleAsync(new InvalidOperationException("boom"), "刷新集群");

        _snackbar.Verify(s => s.Add(
            "刷新集群失败,请稍后重试",
            Severity.Error,
            It.IsAny<Action<SnackbarOptions>>(),
            It.IsAny<string>()), Times.Once);

        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_business_exception_does_not_log()
    {
        await _presenter.HandleAsync(new NotFoundException("不存在"), "操作");

        _logger.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, type) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }
}
