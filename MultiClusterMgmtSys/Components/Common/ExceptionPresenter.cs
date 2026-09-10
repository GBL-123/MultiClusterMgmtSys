using MudBlazor;
using MultiClusterMgmtSys.Common.Exceptions;

namespace MultiClusterMgmtSys.Components.Common;

/// <summary>
/// UI 层异常统一呈现:业务异常显示其中文 UserMessage(冲突用 Warning,其余 Error);
/// 非业务异常记录 Error 日志并显示通用文案,不向用户泄漏技术细节。
/// </summary>
public class ExceptionPresenter(ISnackbar snackbar, ILogger<ExceptionPresenter> logger)
{
    private readonly ISnackbar snackbar = snackbar;

    private readonly ILogger<ExceptionPresenter> logger = logger;

    /// <summary>
    /// 统一处理并呈现异常:业务异常弹其 UserMessage,系统异常弹通用文案并记录日志。
    /// </summary>
    /// <param name="ex">捕获到的异常。</param>
    /// <param name="fallbackMessage">操作描述,非业务异常时拼入"xxx失败,请稍后重试"。</param>
    public Task HandleAsync(Exception ex, string fallbackMessage)
    {
        if (ex is BusinessException business)
        {
            var severity = business is ConflictException ? Severity.Warning : Severity.Error;
            snackbar.Add(business.UserMessage, severity);
            return Task.CompletedTask;
        }

        logger.LogError(ex, "Unhandled exception during {Operation}", fallbackMessage);
        snackbar.Add($"{fallbackMessage}失败,请稍后重试", Severity.Error);
        return Task.CompletedTask;
    }
}
