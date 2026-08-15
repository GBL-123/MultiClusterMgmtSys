using MultiClusterMgmtSys.Components.Clusters.Requests;
using MultiClusterMgmtSys.Components.Clusters.ViewModels;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Components.Clusters.ViewModels.Mappings;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.AuditLogs.Services;

namespace MultiClusterMgmtSys.Components.Clusters.Services;

public class GroupService(
    GroupRepository repo,
    ClusterRepository clusterRepo,
    AuditService auditService,
    ILogger<GroupService> logger)
{
    private readonly GroupRepository repo = repo;
    private readonly ClusterRepository clusterRepo = clusterRepo;
    private readonly AuditService auditService = auditService;
    private readonly ILogger<GroupService> logger = logger;

    public async Task<List<ClusterGroupViewModel>> GetGroupsAsync()
    {
        logger.LogInformation("GetGroups");
        var groups = await repo.GetAllAsync();
        var vms = groups.Select(g => g.ToViewModel()).ToList();
        logger.LogInformation("GetGroups returned {Count} groups", vms.Count);
        return vms;
    }

    public async Task<ClusterGroupViewModel> AddGroupAsync(GroupCreateRequest request)
    {
        logger.LogInformation("AddGroup name={Name}", request.Name);
        var entity = new ClusterGroup
        {
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        logger.LogInformation("AddGroup created id={GroupId}", entity.Id);
        await auditService.LogAsync(AuditCategory.Group, AuditAction.Create, $"分组: {entity.Name}");
        return entity.ToViewModel();
    }

    public async Task DeleteGroupAsync(int id)
    {
        logger.LogInformation("DeleteGroup id={GroupId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is not null)
        {
            await repo.DeleteAsync(id);
            await auditService.LogAsync(AuditCategory.Group, AuditAction.Delete, $"分组: {entity.Name}");
        }
        logger.LogInformation("DeleteGroup done id={GroupId}", id);
    }

    public async Task RenameGroupAsync(int id, string newName)
    {
        logger.LogInformation("RenameGroup id={GroupId} newName={NewName}", id, newName);
        var existing = await repo.GetByIdAsync(id);
        if (existing is null)
        {
            logger.LogWarning("RenameGroup id={GroupId} not found", id);
            throw new InvalidOperationException($"Group {id} not found");
        }

        await repo.RenameAsync(id, newName);
        logger.LogInformation("RenameGroup done id={GroupId}", id);
        await auditService.LogAsync(AuditCategory.Group, AuditAction.Rename, $"分组: {existing.Name}");
    }

    public async Task<int> MoveClustersToGroupAsync(IEnumerable<int> clusterIds, int? targetGroupId)
    {
        if (targetGroupId == 0)
        {
            logger.LogWarning("MoveClustersToGroup rejected targetGroupId=0 (sentinel must be translated to null before service call)");
            throw new ArgumentException("target group id must be a real id or null for ungrouped", nameof(targetGroupId));
        }

        var ids = clusterIds.ToList();
        logger.LogInformation("MoveClustersToGroup count={Count} targetGroupId={TargetGroupId}", ids.Count, targetGroupId);

        var affected = await clusterRepo.SetGroupIdForClustersAsync(ids, targetGroupId);
        logger.LogInformation("MoveClustersToGroup affected={Affected} targetGroupId={TargetGroupId}", affected, targetGroupId);
        if (affected > 0)
        {
            var groupName = targetGroupId is null
                ? "未分组"
                : (await repo.GetByIdAsync(targetGroupId.Value))?.Name ?? $"#{targetGroupId}";
            await auditService.LogAsync(AuditCategory.Group, AuditAction.Move, $"集群 {affected} 个 → {groupName}");
        }
        return affected;
    }

    public async Task<int> GetUngroupedClusterCountAsync()
    {
        var count = await clusterRepo.CountUngroupedAsync();
        logger.LogInformation("GetUngroupedClusterCount count={Count}", count);
        return count;
    }
}