namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 服务详情页展示数据,含端口、选择器与后端端点。
/// </summary>
public class SvcDetailViewModel
{
    /// <summary>服务名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>API 对象唯一标识(Uid)。</summary>
    public string Uid { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>服务类型,如 ClusterIP/NodePort/LoadBalancer/ExternalName。</summary>
    public string Type { get; set; } = "";

    /// <summary>集群内虚拟 IP;Headless 服务为 None。</summary>
    public string ClusterIP { get; set; } = "";

    /// <summary>是否为 Headless 服务(ClusterIP 为 None)。</summary>
    public bool Headless { get; set; }

    /// <summary>外部入口摘要文本(按类型取 LoadBalancer 入口/节点端口/ExternalName);无外部入口时为"—"。</summary>
    public string ExternalEntry { get; set; } = "";

    /// <summary>ExternalName 类型指向的外部域名;其他类型为 null。</summary>
    public string? ExternalName { get; set; } = null;

    /// <summary>标签选择器键值对;无选择器时为空字典。</summary>
    public Dictionary<string, string> Selector { get; set; } = new();

    /// <summary>端口列表。</summary>
    public List<SvcPortViewModel> Ports { get; set; } = new();

    /// <summary>后端端点列表;加载失败时为空列表。</summary>
    public List<SvcEndpointViewModel> Endpoints { get; set; } = new();

    /// <summary>端点列表是否加载失败(为 true 时页面显示加载失败而非空态)。</summary>
    public bool EndpointsLoadFailed { get; set; }

    /// <summary>服务原始 YAML 文本,用于详情页 YAML 视图。</summary>
    public string Yaml { get; set; } = "";
}
