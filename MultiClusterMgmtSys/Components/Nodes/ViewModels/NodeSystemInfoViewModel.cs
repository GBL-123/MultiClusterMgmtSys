namespace MultiClusterMgmtSys.Components.Nodes.ViewModels;

public class NodeSystemInfoViewModel
{
    public string Architecture { get; set; } = "";
    public string BootID { get; set; } = "";
    public string ContainerRuntimeVersion { get; set; } = "";
    public string KernelVersion { get; set; } = "";
    public string KubeProxyVersion { get; set; } = "";
    public string KubeletVersion { get; set; } = "";
    public string MachineID { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string OsImage { get; set; } = "";
    public string SystemUUID { get; set; } = "";
}
