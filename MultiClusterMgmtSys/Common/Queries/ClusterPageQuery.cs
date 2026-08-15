using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Common.Queries;

/// <summary>
/// Pure, UI-agnostic paged-query specification consumed by <c>ClusterRepository</c>.
/// </summary>
public record ClusterPageQuery
{
    /// <summary>
    /// Cluster group filter. <c>null</c> = no filter (return all clusters);
    /// <c>0</c> = ungrouped sentinel (the repository translates this to <c>WHERE GroupId IS NULL</c>);
    /// any positive <c>int</c> = equality with that group id.
    /// </summary>
    public int? GroupId { get; init; }

    public string? NameContains { get; init; }

    public ClusterStatus? Status { get; init; }

    public string? Version { get; init; }

    public DateTime? CreatedAfter { get; init; }

    public DateTime? CreatedBefore { get; init; }

    public ClusterSortField SortBy { get; init; } = ClusterSortField.CreatedAt;

    public bool SortDescending { get; init; } = true;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public static class VersionFilterSentinel
{
    public const string All = "";
    public const string OnlyNull = "__null__";
}