using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 集群分组服务:分组的增删改名,以及集群在分组间批量移动(含移出为未分组)。
/// 删除分组后其下集群由数据库外键 SetNull 规则自动变为未分组。
/// </summary>
public class GroupService(
    IGroupRepository repo,
    IClusterRepository clusterRepo,
    AuditService auditService,
    ILogger<GroupService> logger)
{
    private readonly IGroupRepository _repo = repo;

    private readonly IClusterRepository _clusterRepo = clusterRepo;

    private readonly AuditService _auditService = auditService;

    private readonly ILogger<GroupService> _logger = logger;

    /// <summary>查询全部分组列表,每项含分组内集群数量。</summary>
    public async Task<List<ClusterGroupViewModel>> GetGroupsAsync()
    {
        _logger.LogInformation("GetGroups");
        var groups = await _repo.GetAllAsync();
        var vms = groups.Select(g => g.ToViewModel()).ToList();
        _logger.LogInformation("GetGroups returned {Count} groups", vms.Count);
        return vms;
    }

    /// <summary>创建指定名称的分组,成功后写创建审计。</summary>
    /// <param name="groupName">分组名称。</param>
    /// <returns>新建分组的视图(含 Id)。</returns>
    public async Task<ClusterGroupViewModel> AddGroupAsync(string groupName)
    {
        _logger.LogInformation("AddGroup name={Name}", groupName);
        var entity = new ClusterGroup
        {
            Name = groupName,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        _logger.LogInformation("AddGroup created id={GroupId}", entity.Id);
        await _auditService.LogAsync(AuditCategory.Group, AuditAction.Create, $"分组: {entity.Name}");
        return entity.ToViewModel();
    }

    /// <summary>删除分组;分组不存在时静默返回,删除成功后写删除审计,其下集群自动变为未分组(外键 SetNull)。</summary>
    public async Task DeleteGroupAsync(int id)
    {
        _logger.LogInformation("DeleteGroup id={GroupId}", id);
        var entity = await _repo.GetByIdAsync(id);
        if (entity is not null)
        {
            await _repo.DeleteAsync(id);
            await _auditService.LogAsync(AuditCategory.Group, AuditAction.Delete, $"分组: {entity.Name}");
        }
        _logger.LogInformation("DeleteGroup done id={GroupId}", id);
    }

    /// <summary>重命名分组;分组不存在抛 <see cref="NotFoundException"/>,成功后写重命名审计(审计目标记录原名)。</summary>
    public async Task RenameGroupAsync(GroupRenameRequest request)
    {
        _logger.LogInformation("RenameGroup id={GroupId} newName={NewName}", request.Id, request.NewName);
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing is null)
        {
            _logger.LogWarning("RenameGroup id={GroupId} not found", request.Id);
            throw new NotFoundException($"分组 {request.Id} 不存在");
        }

        await _repo.RenameAsync(request.Id, request.NewName);
        _logger.LogInformation("RenameGroup done id={GroupId}", request.Id);
        await _auditService.LogAsync(AuditCategory.Group, AuditAction.Rename, $"分组: {existing.Name}");
    }

    /// <summary>批量移动集群到目标分组;TargetGroupId 为 null 表示移出为未分组,为 0(未翻译的哨兵值)抛 <see cref="ValidationException"/>。移动数大于 0 时写审计。</summary>
    /// <returns>实际移动的集群数量。</returns>
    public async Task<int> MoveClustersToGroupAsync(MoveClustersRequest request)
    {
        if (request.TargetGroupId == 0)
        {
            _logger.LogWarning("MoveClustersToGroup rejected targetGroupId=0 (sentinel must be translated to null before service call)");
            throw new ValidationException("目标分组无效,请刷新后重试");
        }

        var ids = request.ClusterIds.ToList();
        _logger.LogInformation("MoveClustersToGroup count={Count} targetGroupId={TargetGroupId}", ids.Count, request.TargetGroupId);

        var affected = await _clusterRepo.SetGroupIdForClustersAsync(ids, request.TargetGroupId);
        _logger.LogInformation("MoveClustersToGroup affected={Affected} targetGroupId={TargetGroupId}", affected, request.TargetGroupId);
        if (affected > 0)
        {
            var groupName = request.TargetGroupId is null
                ? "未分组"
                : (await _repo.GetByIdAsync(request.TargetGroupId.Value))?.Name ?? $"#{request.TargetGroupId}";
            await _auditService.LogAsync(AuditCategory.Group, AuditAction.Move, $"集群 {affected} 个 → {groupName}");
        }
        return affected;
    }

    /// <summary>统计未分组集群的数量,供分组页提示与校验使用。</summary>
    public async Task<int> GetUngroupedClusterCountAsync()
    {
        var count = await _clusterRepo.CountUngroupedAsync();
        _logger.LogInformation("GetUngroupedClusterCount count={Count}", count);
        return count;
    }
}
