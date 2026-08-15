namespace MultiClusterMgmtSys.Components.Clusters.ViewModels;

public class ClusterGroupViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int ClusterCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
