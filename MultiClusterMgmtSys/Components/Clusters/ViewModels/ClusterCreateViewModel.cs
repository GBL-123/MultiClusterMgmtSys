using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Features.Clusters.ViewModels;

public class ClusterCreateViewModel
{
    public string Name { get; set; } = "";
    public int? GroupId { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public string? ApiServer { get; set; }
    public string? KubeConfig { get; set; }
    public string? Token { get; set; }
    public bool SkipTlsVerify { get; set; } = true;
}
