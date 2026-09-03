using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Components.Clusters.ViewModels;

public class ClusterNodeViewModel
{
    public string Name { get; set; } = "";

    public string Status { get; set; } = "";

    public string Roles { get; set; } = "";

    public string KubeletVersion { get; set; } = "";

    public string OsImage { get; set; } = "";

    public bool Unschedulable { get; set; }

    public List<NodeIpViewModel> IpAddresses { get; set; } = new();
}
