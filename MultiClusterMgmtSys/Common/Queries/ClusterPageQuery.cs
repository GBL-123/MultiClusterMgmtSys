using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Common.Queries;

public record ClusterPageQuery(
    int? GroupId,
    string? NameContains,
    ClusterStatus? Status,
    string? Version,
    DateTime? CreatedAfter,
    DateTime? CreatedBefore,
    ClusterSortField SortBy = ClusterSortField.CreatedAt,
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20);

public static class VersionFilterSentinel
{
    public const string All = "";
    public const string OnlyNull = "__null__";
}