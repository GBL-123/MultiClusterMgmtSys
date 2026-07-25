namespace MultiClusterMgmtSys.Features.Clusters.ViewModels;

public class ClusterNodeViewModel
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Roles { get; set; } = "";
    public string KubeletVersion { get; set; } = "";
    public string OsImage { get; set; } = "";
    public string InternalIP { get; set; } = "";
}
