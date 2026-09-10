using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 新建集群入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterService"/> 的新建方法(AddClusterAsync)消费。
/// 凭据按接入方式二选一保存:KubeConfig 方式取 <see cref="KubeConfig"/>,Token 方式取 <see cref="ApiServer"/> + <see cref="Token"/>,
/// 非当前接入方式的凭据字段保存时会被置空。
/// </summary>
public class ClusterCreateRequest
{
    /// <summary>集群显示名称(必填,用于列表展示与模糊搜索)。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属分组的 Id;null = 不分组(未分组集群)。</summary>
    public int? GroupId { get; set; }

    /// <summary>凭据接入方式(kubeconfig 文本 / Token 直连),决定保存哪种凭据字段。</summary>
    public ConnectionType ConnectionType { get; set; }

    /// <summary>API Server 地址(Token 直连方式配合 <see cref="Token"/> 使用;kubeconfig 方式可空)。</summary>
    public string? ApiServer { get; set; }

    /// <summary>kubeconfig 文本(YAML),仅接入方式为 KubeConfig 时保存,否则置空。</summary>
    public string? KubeConfig { get; set; }

    /// <summary>直连 Bearer Token 文本,仅接入方式为 Token 时保存,否则置空。</summary>
    public string? Token { get; set; }

    /// <summary>是否跳过 TLS 证书校验(默认 true,兼容自签证书环境)。</summary>
    public bool SkipTlsVerify { get; set; } = true;

    /// <summary>管理员登记的集群端点列表(见 <see cref="ClusterEndpointEditItem"/>),随集群一并写入。</summary>
    public List<ClusterEndpointEditItem> Endpoints { get; set; } = new();
}