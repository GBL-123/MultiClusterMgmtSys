using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Features.Clusters.ViewModels;

public class ClusterQuery
{
    public string? Name { get; set; }
    public int? GroupId { get; set; }
    public ClusterStatus? Status { get; set; }
    public string? Version { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public ClusterSortField SortBy { get; set; } = ClusterSortField.CreatedAt;
    public bool SortDescending { get; set; } = true;
}