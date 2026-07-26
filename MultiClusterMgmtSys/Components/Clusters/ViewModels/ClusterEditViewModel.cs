using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Components.Clusters.ViewModels;

public class ClusterEditViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? GroupId { get; set; }
    public string? ApiServer { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public bool SkipTlsVerify { get; set; }
    public string? KubeConfig { get; set; }
    public string? Token { get; set; }
}
