namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 集群节点列表展示数据(也用于集群详情页的节点表)。
/// </summary>
public class ClusterNodeViewModel
{
    /// <summary>节点名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>节点状态文本(Ready 为在线,其余为离线/未知)。</summary>
    public string Status { get; set; } = "";

    /// <summary>角色标签串,如 control-plane、worker。</summary>
    public string Roles { get; set; } = "";

    /// <summary>kubelet 版本。</summary>
    public string KubeletVersion { get; set; } = "";

    /// <summary>操作系统镜像描述。</summary>
    public string OsImage { get; set; } = "";

    /// <summary>是否已被标记不可调度(cordon)。</summary>
    public bool Unschedulable { get; set; }

    /// <summary>节点 IP 列表(含管理员备注)。</summary>
    public List<NodeIpViewModel> IpAddresses { get; set; } = new();
}
