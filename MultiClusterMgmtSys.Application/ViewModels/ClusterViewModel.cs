using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 集群列表页展示数据。
/// </summary>
public class ClusterViewModel
{
    /// <summary>集群主键。</summary>
    public int Id { get; set; }

    /// <summary>集群名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>集群状态枚举(<see cref="ClusterStatus"/>),徽标配色依据。</summary>
    public ClusterStatus Status { get; set; }

    /// <summary>状态中文文本(在线/离线/未知)。</summary>
    public string StatusText { get; set; } = "";

    /// <summary>Kubernetes 版本;探测失败或未同步时为 null。</summary>
    public string? Version { get; set; }

    /// <summary>节点总数。</summary>
    public int NodeCount { get; set; }

    /// <summary>节点数展示文本(带「台」单位)。</summary>
    public string NodeCountText => K8sDisplayText.NodeCountText(NodeCount);

    /// <summary>所属分组主键;未分组为 null。</summary>
    public int? GroupId { get; set; }

    /// <summary>所属分组名称;未分组为 null。</summary>
    public string? GroupName { get; set; }

    /// <summary>API Server 地址;未登记时为 null。</summary>
    public string? ApiServer { get; set; }

    /// <summary>集群接入时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最近一次连通性探测时间;从未探测为 null。</summary>
    public DateTime? LastCheckedAt { get; set; }

    /// <summary>连接方式(kubeconfig 或 Token);null 表示未记录。</summary>
    public ConnectionType? ConnectionType { get; set; }

    /// <summary>连接方式中文展示名(配置文件/访问令牌);未记录时为空字符串。</summary>
    public string ConnectionTypeText => ConnectionType is { } type ? K8sDisplayText.ConnectionTypeText(type) : "";

    /// <summary>连接方式英文原文(Kubeconfig/Token);未记录时为空字符串。</summary>
    public string ConnectionTypeRawText => ConnectionType is { } type ? K8sDisplayText.ConnectionTypeRaw(type) : "";
}
