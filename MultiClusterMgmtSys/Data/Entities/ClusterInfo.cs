using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

public class ClusterInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? ApiServer { get; set; }
    public string? KubeConfig { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public string? Token { get; set; }
    public bool SkipTlsVerify { get; set; } = true;
    public ClusterStatus Status { get; set; }
    public string? Version { get; set; }
    public int NodeCount { get; set; }
    public int? GroupId { get; set; }
    public ClusterGroup? Group { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCheckedAt { get; set; }
}
