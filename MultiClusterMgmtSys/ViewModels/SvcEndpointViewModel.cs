namespace MultiClusterMgmtSys.ViewModels;

public class SvcEndpointViewModel
{
    public string Address { get; set; } = "";

    public string? Port { get; set; } = null;

    public bool Ready { get; set; }
}
