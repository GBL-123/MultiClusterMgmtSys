using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public AuditCategory Category { get; set; }
    public AuditAction Action { get; set; }
    public string Target { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
