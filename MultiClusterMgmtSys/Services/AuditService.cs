using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 审计服务:记录操作日志并提供"最近记录"与分页查询。
/// 写入失败仅记录警告、静默降级,不影响主业务流程。
/// </summary>
public class AuditService(
    AuditLogRepository repo,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger)
{
    private readonly AuditLogRepository repo = repo;

    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    private readonly ILogger<AuditService> logger = logger;

    /// <summary>写一条审计日志,操作者默认取当前登录用户名;写入失败静默降级(记录警告日志,不向调用方抛异常)。</summary>
    /// <param name="category">业务类别,如集群/账号/工作负载。</param>
    /// <param name="action">具体动作,如创建/删除/更新。</param>
    /// <param name="target">目标对象的中文描述,如"集群: prod"。</param>
    /// <param name="userName">显式指定操作者用户名;缺省时从当前 HTTP 上下文解析。</param>
    public async Task LogAsync(AuditCategory category, AuditAction action, string target, string? userName = null)
    {
        try
        {
            var actor = userName ?? httpContextAccessor.HttpContext?.User.Identity?.Name;
            await repo.AddAsync(new AuditLog
            {
                UserName = actor,
                Category = category,
                Action = action,
                Target = target,
                CreatedAt = DateTime.UtcNow
            });
            logger.LogInformation("Audit logged actor={Actor} category={Category} action={Action} target={Target}",
                actor, category, action, target);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Audit log write failed category={Category} action={Action} target={Target}",
                category, action, target);
        }
    }

    /// <summary>查询当前登录用户最近 count 条审计记录;无登录身份返回空列表。</summary>
    /// <param name="count">返回条数上限。</param>
    /// <returns>按时间倒序的最近审计记录视图列表。</returns>
    public async Task<List<AuditLogViewModel>> GetRecentAsync(int count)
    {
        var userName = httpContextAccessor.HttpContext?.User.Identity?.Name;
        logger.LogInformation("GetRecentAuditLogs user={UserName} count={Count}", userName, count);
        if (string.IsNullOrEmpty(userName))
        {
            return [];
        }
        var items = await repo.GetRecentForUserAsync(userName, count);
        logger.LogInformation("GetRecentAuditLogs returned {Count} for user={UserName}", items.Count, userName);
        return [.. items.Select(l => l.ToAuditLogViewModel())];
    }

    /// <summary>分页查询审计日志;管理员可见全部记录,普通用户只能看到自己的操作记录。</summary>
    /// <param name="query">分页与过滤条件。</param>
    public async Task<PagedResult<AuditLogViewModel>> GetPagedAsync(AuditLogQueryRequest query)
    {
        var currentUserName = httpContextAccessor.HttpContext?.User.Identity?.Name;
        var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;
        logger.LogInformation("GetAuditLogs page={Page} isAdmin={IsAdmin}", query.Page, isAdmin);
        var (items, total) = await repo.GetPagedAsync(query, currentUserName, isAdmin);
        logger.LogInformation("GetAuditLogs returned {Count} of {Total}", items.Count, total);
        return new PagedResult<AuditLogViewModel>([.. items.Select(l => l.ToAuditLogViewModel())], total);
    }
}
