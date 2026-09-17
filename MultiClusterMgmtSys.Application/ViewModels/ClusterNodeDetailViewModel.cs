using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 集群节点详情页展示数据,含概要、地址/状况/污点、资源容量与标签注解。
/// </summary>
public class ClusterNodeDetailViewModel
{
    // 概要
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

    // 元数据
    /// <summary>节点对象创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>是否已被标记不可调度(cordon)。</summary>
    public bool Unschedulable { get; set; }

    /// <summary>该节点的 Pod 网段(CIDR)。</summary>
    public string PodCIDR { get; set; } = "";

    /// <summary>节点生命周期阶段,通常为 Running/Pending/Terminated。</summary>
    public string Phase { get; set; } = "";

    /// <summary>节点阶段中文展示名(运行中/等待中/已终止)。</summary>
    public string PhaseText => K8sDisplayText.NodePhaseText(Phase);

    // 列表
    /// <summary>地址列表(含管理员备注)。</summary>
    public List<NodeAddressViewModel> Addresses { get; set; } = new();

    /// <summary>状况(Condition)列表。</summary>
    public List<NodeConditionViewModel> Conditions { get; set; } = new();

    /// <summary>污点(Taint)列表。</summary>
    public List<NodeTaintViewModel> Taints { get; set; } = new();

    // 资源
    /// <summary>资源容量与可分配行(含人类可读文本与占比),按固定资源序排列。</summary>
    public List<NodeResourceViewModel> Resources { get; set; } = new();

    /// <summary>节点标签键值对。</summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>节点注解键值对。</summary>
    public Dictionary<string, string> Annotations { get; set; } = new();

    // 系统信息
    /// <summary>节点系统信息(架构/内核/运行时等)。</summary>
    public NodeSystemInfoViewModel SystemInfo { get; set; } = new();

    // YAML
    /// <summary>节点原始 YAML 文本(KubernetesYaml 序列化),用于详情页 YAML 视图。</summary>
    public string Yaml { get; set; } = "";

    // 上下文
    /// <summary>所属集群主键(页面导航上下文)。</summary>
    public int ClusterId { get; set; }

    /// <summary>所属集群名称。</summary>
    public string ClusterName { get; set; } = "";

    /// <summary>集群当前是否可连通;false 时部分详情数据可能缺失,页面提示不可达。</summary>
    public bool IsReachable { get; set; }
}
