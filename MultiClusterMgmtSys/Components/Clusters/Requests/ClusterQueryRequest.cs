using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Queries;

namespace MultiClusterMgmtSys.Components.Clusters.Requests;

public class ClusterQueryRequest
{
    public string? Name { get; set; }

    public int? GroupId { get; set; }

    public ClusterStatus? Status { get; set; }

    public string VersionSelection { get; set; } = VersionFilterSentinel.All;

    public DateRange? DateRange { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public ClusterSortField SortBy { get; set; } = ClusterSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}

    
