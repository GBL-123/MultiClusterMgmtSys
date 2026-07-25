using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Features.Clusters.ViewModels;
using MultiClusterMgmtSys.Features.Clusters.ViewModels.Mappings;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Features.Clusters.Services;

public class GroupService(GroupRepository repo, ILogger<GroupService> logger)
{
    private readonly GroupRepository repo = repo;
    private readonly ILogger<GroupService> logger = logger;

    public async Task<List<ClusterGroupViewModel>> GetGroupsAsync()
    {
        var groups = await repo.GetAllAsync();
        return groups.Select(g => g.ToViewModel()).ToList();
    }

    public async Task<ClusterGroupViewModel> AddGroupAsync(GroupCreateViewModel vm)
    {
        var entity = new ClusterGroup
        {
            Name = vm.Name,
            Description = vm.Description,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        return entity.ToViewModel();
    }

    public async Task DeleteGroupAsync(int id)
    {
        await repo.DeleteAsync(id);
    }
}
