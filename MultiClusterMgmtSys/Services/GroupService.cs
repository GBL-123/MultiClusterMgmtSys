using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群分组服务:分组的增删改名,以及集群在分组间批量移动(含移出为未分组)。
/// 删除分组后其下集群由数据库外键 SetNull 规则自动变为未分组。
/// </summary>
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

    /// <summary>查询全部分组列表,每项含分组内集群数量。</summary>
    public async Task<List<ClusterGroupViewModel>> GetGroupsAsync()
    {
        logger.LogInformation("GetGroups");
        var groups = await repo.GetAllAsync();
        var vms = groups.Select(g => g.ToViewModel()).ToList();
        logger.LogInformation("GetGroups returned {Count} groups", vms.Count);
        return vms;
    }

    /// <summary>创建指定名称的分组,成功后写创建审计。</summary>
    /// <param name="groupName">分组名称。</param>
    /// <returns>新建分组的视图(含 Id)。</returns>
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

    /// <summary>删除分组;分组不存在时静默返回,删除成功后写删除审计,其下集群自动变为未分组(外键 SetNull)。</summary>
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

    /// <summary>重命名分组;分组不存在抛 <see cref="NotFoundException"/>,成功后写重命名审计(审计目标记录原名)。</summary>
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

    /// <summary>批量移动集群到目标分组;TargetGroupId 为 null 表示移出为未分组,为 0(未翻译的哨兵值)抛 <see cref="ValidationException"/>。移动数大于 0 时写审计。</summary>
    /// <returns>实际移动的集群数量。</returns>
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

    /// <summary>统计未分组集群的数量,供分组页提示与校验使用。</summary>
    public async Task<int> GetUngroupedClusterCountAsync()
    {
        var count = await clusterRepo.CountUngroupedAsync();
        logger.LogInformation("GetUngroupedClusterCount count={Count}", count);
        return count;
    }
}
