using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Components.AuditLogs.Requests;

public class AuditLogQueryRequest
{
    public string? SearchName { get; set; }

    public AuditCategory? Category { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
