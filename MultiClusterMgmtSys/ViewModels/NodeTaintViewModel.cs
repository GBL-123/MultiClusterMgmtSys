namespace MultiClusterMgmtSys.ViewModels;

public class NodeTaintViewModel
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public string Effect { get; set; } = "";
}
