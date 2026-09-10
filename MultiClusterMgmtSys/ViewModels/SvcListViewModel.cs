namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 服务列表页展示数据。
/// </summary>
public class SvcListViewModel
{
    /// <summary>服务名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>服务类型,如 ClusterIP/NodePort/LoadBalancer/ExternalName。</summary>
    public string Type { get; set; } = "";

    /// <summary>集群内虚拟 IP;Headless 服务为 None。</summary>
    public string ClusterIP { get; set; } = "";

    /// <summary>是否为 Headless 服务(ClusterIP 为 None)。</summary>
    public bool Headless { get; set; }

    /// <summary>端口列表。</summary>
    public List<SvcPortViewModel> Ports { get; set; } = new();

    /// <summary>外部入口摘要文本(按类型取 LoadBalancer 入口/节点端口/ExternalName);无外部入口时为"—"。</summary>
    public string ExternalEntry { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
