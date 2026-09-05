using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Services;

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

    public async Task<ClusterGroupViewModel> AddGroupAsync(string groupName)
    {
        logger.LogInformation("AddGroup name={Name}", groupName);
        var entity = new ClusterGroup
        {
            Name = groupName,
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

    public async Task RenameGroupAsync(GroupRenameRequest request)
    {
        logger.LogInformation("RenameGroup id={GroupId} newName={NewName}", request.Id, request.NewName);
        var existing = await repo.GetByIdAsync(request.Id);
        if (existing is null)
        {
            logger.LogWarning("RenameGroup id={GroupId} not found", request.Id);
            throw new NotFoundException($"分组 {request.Id} 不存在");
        }

        await repo.RenameAsync(request.Id, request.NewName);
        logger.LogInformation("RenameGroup done id={GroupId}", request.Id);
        await auditService.LogAsync(AuditCategory.Group, AuditAction.Rename, $"分组: {existing.Name}");
    }

    public async Task<int> MoveClustersToGroupAsync(MoveClustersRequest request)
    {
        if (request.TargetGroupId == 0)
        {
            logger.LogWarning("MoveClustersToGroup rejected targetGroupId=0 (sentinel must be translated to null before service call)");
            throw new ValidationException("目标分组无效,请刷新后重试");
        }

        var ids = request.ClusterIds.ToList();
        logger.LogInformation("MoveClustersToGroup count={Count} targetGroupId={TargetGroupId}", ids.Count, request.TargetGroupId);

        var affected = await clusterRepo.SetGroupIdForClustersAsync(ids, request.TargetGroupId);
        logger.LogInformation("MoveClustersToGroup affected={Affected} targetGroupId={TargetGroupId}", affected, request.TargetGroupId);
        if (affected > 0)
        {
            var groupName = request.TargetGroupId is null
                ? "未分组"
                : (await repo.GetByIdAsync(request.TargetGroupId.Value))?.Name ?? $"#{request.TargetGroupId}";
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
