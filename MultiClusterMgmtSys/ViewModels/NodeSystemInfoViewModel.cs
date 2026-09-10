namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点系统信息(Node SystemInfo)展示数据。
/// </summary>
public class NodeSystemInfoViewModel
{
    /// <summary>CPU 架构,如 amd64/arm64。</summary>
    public string Architecture { get; set; } = "";

    /// <summary>本次启动的唯一标识。</summary>
    public string BootID { get; set; } = "";

    /// <summary>容器运行时及版本,如 containerd://1.7.x。</summary>
    public string ContainerRuntimeVersion { get; set; } = "";

    /// <summary>内核版本。</summary>
    public string KernelVersion { get; set; } = "";

    /// <summary>kube-proxy 版本。</summary>
    public string KubeProxyVersion { get; set; } = "";

    /// <summary>kubelet 版本。</summary>
    public string KubeletVersion { get; set; } = "";

    /// <summary>机器唯一标识。</summary>
    public string MachineID { get; set; } = "";

    /// <summary>操作系统类型,如 linux/windows。</summary>
    public string OperatingSystem { get; set; } = "";

    /// <summary>操作系统镜像描述。</summary>
    public string OsImage { get; set; } = "";

    /// <summary>系统 UUID。</summary>
    public string SystemUUID { get; set; } = "";
}
