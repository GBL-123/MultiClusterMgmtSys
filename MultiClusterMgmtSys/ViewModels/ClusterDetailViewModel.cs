using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Components.Clusters.ViewModels;

public class ClusterDetailViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public ClusterStatus Status { get; set; }

    public string StatusText { get; set; } = "";

    public string? Version { get; set; }

    public int NodeCount { get; set; }

    public int? GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? ApiServer { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public ConnectionType? ConnectionType { get; set; }

    public List<ClusterNodeViewModel> Nodes { get; set; } = new();

    public bool IsReachable { get; set; }

    public List<ClusterEndpointViewModel> Endpoints { get; set; } = new();
}
