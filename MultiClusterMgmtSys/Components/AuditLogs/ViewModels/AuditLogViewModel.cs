namespace MultiClusterMgmtSys.Components.AuditLogs.ViewModels;

public class AuditLogViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string ActionName { get; set; } = "";
    public string Target { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
