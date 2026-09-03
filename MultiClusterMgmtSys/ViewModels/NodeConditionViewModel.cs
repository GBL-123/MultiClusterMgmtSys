namespace MultiClusterMgmtSys.ViewModels;

public class NodeConditionViewModel
{
    public string Type { get; set; } = "";

    public string Status { get; set; } = "";

    public string? Reason { get; set; }

    public string? Message { get; set; }

    public DateTime? LastHeartbeatTime { get; set; }

    public DateTime? LastTransitionTime { get; set; }
}
