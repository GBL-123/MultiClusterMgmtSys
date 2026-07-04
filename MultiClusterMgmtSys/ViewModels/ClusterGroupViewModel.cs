namespace MultiClusterMgmtSys.ViewModels;

public class ClusterGroupViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ClusterCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
