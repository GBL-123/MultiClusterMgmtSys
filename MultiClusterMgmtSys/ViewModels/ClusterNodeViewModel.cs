using MultiClusterMgmtSys.ViewModels.Mappings;

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

    /// <summary>节点状态中文展示名(就绪/未就绪/未知)。</summary>
    public string StatusText => K8sDisplayText.NodeStatusText(Status);

    /// <summary>节点状态对应的状态徽章 CSS 类(online/offline/unknown)。</summary>
    public string StatusCssClass => K8sDisplayText.NodeStatusCssClass(Status);

    /// <summary>角色标签串,如 control-plane、worker。</summary>
    public string Roles { get; set; } = "";

    /// <summary>节点角色中文展示名(顿号连接;未登记角色回退原文)。</summary>
    public string RolesText => K8sDisplayText.NodeRoleText(Roles);


    /// <summary>kubelet 版本。</summary>
    public string KubeletVersion { get; set; } = "";

    /// <summary>操作系统镜像描述。</summary>
    public string OsImage { get; set; } = "";

    /// <summary>是否已被标记不可调度(cordon)。</summary>
    public bool Unschedulable { get; set; }

    /// <summary>节点 IP 列表(含管理员备注)。</summary>
    public List<NodeIpViewModel> IpAddresses { get; set; } = new();
}
