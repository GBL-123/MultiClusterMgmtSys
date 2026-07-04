using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.ViewModels;

public class ClusterViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ClusterStatus Status { get; set; }
    public string StatusText { get; set; } = "";
    public string? Version { get; set; }
    public int NodeCount { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string? ApiServer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public ConnectionType? ConnectionType { get; set; }
}
