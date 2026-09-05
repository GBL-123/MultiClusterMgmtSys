using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

public class ClusterCreateRequest
{
    public string Name { get; set; } = "";

    public int? GroupId { get; set; }

    public ConnectionType ConnectionType { get; set; }

    public string? ApiServer { get; set; }

    public string? KubeConfig { get; set; }

    public string? Token { get; set; }

    public bool SkipTlsVerify { get; set; } = true;

    public List<ClusterEndpointEditItem> Endpoints { get; set; } = new();
}