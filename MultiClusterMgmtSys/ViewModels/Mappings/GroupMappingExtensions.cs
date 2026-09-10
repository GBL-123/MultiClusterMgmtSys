using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.ViewModels.Mappings;

/// <summary>
/// 集群分组实体 → 分组展示模型的映射。
/// </summary>
public static class GroupMappingExtensions
{
    /// <summary>集群分组实体 → 分组展示 ViewModel 映射。</summary>
    public static ClusterGroupViewModel ToViewModel(this ClusterGroup g)
    {
        return new ClusterGroupViewModel
        {
            Id = g.Id,
            Name = g.Name,
            ClusterCount = g.Clusters?.Count ?? 0,
            CreatedAt = g.CreatedAt
        };
    }
}
