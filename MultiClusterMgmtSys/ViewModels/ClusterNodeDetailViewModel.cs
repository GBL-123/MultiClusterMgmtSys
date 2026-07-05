namespace MultiClusterMgmtSys.ViewModels;

public class ClusterNodeDetailViewModel
{
    // 概要
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string Roles { get; set; } = "";
    public string KubeletVersion { get; set; } = "";
    public string OsImage { get; set; } = "";
    public string InternalIP { get; set; } = "";

    // 元数据
    public string Uid { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public bool Unschedulable { get; set; }
    public string PodCIDR { get; set; } = "";
    public string Phase { get; set; } = "";

    // 列表
    public List<NodeAddressViewModel> Addresses { get; set; } = new();
    public List<NodeConditionViewModel> Conditions { get; set; } = new();
    public List<NodeTaintViewModel> Taints { get; set; } = new();

    // 字典
    public Dictionary<string, string> Capacity { get; set; } = new();
    public Dictionary<string, string> Allocatable { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, string> Annotations { get; set; } = new();

    // 系统信息
    public NodeSystemInfoViewModel SystemInfo { get; set; } = new();

    // 上下文
    public int ClusterId { get; set; }
    public string ClusterName { get; set; } = "";
    public bool IsReachable { get; set; }
}
