using Microsoft.Extensions.Hosting;
using MultiClusterMgmtSys.Application.Services;

namespace MultiClusterMgmtSys.Infrastructure.Sync;

/// <summary>
/// 告警评估后台服务:独立计时的周期评估器,纯读 SQLite 评估告警规则并维护 open → resolved 状态机。
/// 恒运行、无启用开关(停用定时同步 ≠ 停用值守,断流规则自身按「停用不触发」排除);
/// 每轮通过新 DI 作用域执行评估,整轮失败仅记录错误,不影响下一轮调度。
/// 不挂定时同步的轮末,也不依赖任何页面访问触发——同步本身停摆的场景才最需要被评估到。
/// </summary>
public class AlertEvaluationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AlertEvaluationBackgroundService> logger) : BackgroundService
{
    /// <summary>评估轮询间隔(硬编码常量,不暴露配置面;纯 SQLite 读极便宜)。</summary>
    private static readonly TimeSpan EvaluationInterval = TimeSpan.FromMinutes(1);

    /// <summary>主循环:每轮独立评估一次告警,随后休眠固定间隔,直至停机令牌取消。</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AlertEvaluationBackgroundService started");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "告警评估整轮失败");
                }

                await Task.Delay(EvaluationInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("AlertEvaluationBackgroundService stopped");
        }
    }

    /// <summary>立即执行一轮告警评估;内部通过新作用域解析 <see cref="AlertService"/>,返回评估结果(开立数/解析数)。</summary>
    /// <param name="cancellationToken">停机令牌。</param>
    /// <returns>(本轮开立条数, 本轮解析条数)。</returns>
    public async Task<(int Opened, int Resolved)> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var alertService = scope.ServiceProvider.GetRequiredService<AlertService>();
        var (opened, resolved) = await alertService.EvaluateAsync();
        logger.LogInformation("AlertEvaluation run once done opened={Opened} resolved={Resolved}", opened, resolved);
        return (opened, resolved);
    }
}
