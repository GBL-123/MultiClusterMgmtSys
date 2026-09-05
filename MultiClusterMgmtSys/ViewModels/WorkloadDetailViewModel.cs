using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

public class WorkloadConditionViewModel
{
    public string Type { get; set; } = "";

    public string Status { get; set; } = "";

    public string Reason { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime? LastTransitionAt { get; set; } = null;
}

public class WorkloadDetailViewModel
{
    public string Name { get; set; } = "";

    public string Namespace { get; set; } = "";

    public string Uid { get; set; } = "";

    public WorkloadKind Kind { get; set; }

    public WorkloadRolloutState RolloutState { get; set; } = WorkloadRolloutState.NotReady;

    public int DesiredCount { get; set; }

    public int ReadyCount { get; set; }

    public int UpdatedCount { get; set; }

    public string ReadyText => $"{ReadyCount}/{DesiredCount}";

    public string Selector { get; set; } = "";

    public List<WorkloadConditionViewModel> Conditions { get; set; } = new();

    public DateTime? CreatedAt { get; set; } = null;

    public string Yaml { get; set; } = "";
}
