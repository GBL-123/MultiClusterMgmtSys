using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.ViewModels;

public class ClusterUpdateViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? GroupId { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public string? ApiServer { get; set; }
    public string? KubeConfig { get; set; }
    public string? Token { get; set; }
    public bool SkipTlsVerify { get; set; } = true;
}
