using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

public class WorkloadListViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public WorkloadKind Kind { get; set; }

    public int ReadyCount { get; set; }

    public int DesiredCount { get; set; }

    public string ReadyText => $"{ReadyCount}/{DesiredCount}";

    public WorkloadRolloutState RolloutState { get; set; } = WorkloadRolloutState.NotReady;

    public DateTime? CreatedAt { get; set; } = null;
}
