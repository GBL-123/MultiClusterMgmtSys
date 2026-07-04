using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

public static class GroupMappingExtensions
{
    public static ClusterGroupViewModel ToViewModel(this ClusterGroup g)
    {
        return new ClusterGroupViewModel
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            ClusterCount = g.Clusters?.Count ?? 0,
            CreatedAt = g.CreatedAt
        };
    }
}
