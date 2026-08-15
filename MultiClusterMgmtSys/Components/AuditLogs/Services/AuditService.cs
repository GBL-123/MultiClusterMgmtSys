using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.ViewModels;
using MultiClusterMgmtSys.Components.AuditLogs.Requests;
using MultiClusterMgmtSys.Components.AuditLogs.ViewModels;
using MultiClusterMgmtSys.Components.AuditLogs.ViewModels.Mappings;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;

namespace MultiClusterMgmtSys.Components.AuditLogs.Services;

public class AuditService(
    AuditLogRepository repo,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditService> logger)
{
    private readonly AuditLogRepository repo = repo;
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private readonly ILogger<AuditService> logger = logger;

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

    public async Task<PagedResult<AuditLogViewModel>> GetPagedAsync(
        TableState state,
        AuditLogQueryRequest query,
        string? currentUserName,
        bool isAdmin)
    {
        logger.LogInformation("GetAuditLogs page={Page} isAdmin={IsAdmin}", state.Page, isAdmin);
        var (items, total) = await repo.GetPagedAsync(state, query, currentUserName, isAdmin);
        logger.LogInformation("GetAuditLogs returned {Count} of {Total}", items.Count, total);
        return new PagedResult<AuditLogViewModel>([.. items.Select(l => l.ToAuditLogViewModel())], total);
    }
}
