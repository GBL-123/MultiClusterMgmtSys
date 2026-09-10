namespace MultiClusterMgmtSys.ViewModels;

public class SvcPortViewModel
{
    public string? Name { get; set; } = null;

    public int Port { get; set; }

    public string? TargetPort { get; set; } = null;

    public string Protocol { get; set; } = "TCP";

    public int? NodePort { get; set; } = null;
}
