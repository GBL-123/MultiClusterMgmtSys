using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

public class AuditLogQueryRequest
{
    public string? SearchName { get; set; }

    public AuditCategory? Category { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; } = "CreatedAt";

    public bool SortDescending { get; set; } = true;
}
