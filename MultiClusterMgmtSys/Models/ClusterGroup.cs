namespace MultiClusterMgmtSys.Models;

public class ClusterGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClusterInfo> Clusters { get; set; } = new();
}
