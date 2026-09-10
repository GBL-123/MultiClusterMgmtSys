using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 编辑集群入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterService"/> 的更新方法(UpdateClusterAsync)消费。
/// 凭据语义与新建一致:按接入方式二选一保存,非当前方式的凭据字段置空;
/// 接入配置有变化时服务会重新探测可达状态。集群不存在时抛业务异常。
/// </summary>
public class ClusterUpdateRequest
{
    /// <summary>要编辑的集群 Id(数据库主键)。</summary>
    public int Id { get; set; }

    /// <summary>集群显示名称(必填)。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属分组的 Id;null = 不分组(未分组集群)。</summary>
    public int? GroupId { get; set; }

    /// <summary>凭据接入方式(kubeconfig 文本 / Token 直连),决定保存哪种凭据字段。</summary>
    public ConnectionType ConnectionType { get; set; }

    /// <summary>API Server 地址(Token 直连方式配合 Token 使用;kubeconfig 方式可空)。</summary>
    public string? ApiServer { get; set; }

    /// <summary>kubeconfig 文本(YAML),仅接入方式为 KubeConfig 时保存,否则置空。</summary>
    public string? KubeConfig { get; set; }

    /// <summary>直连 Bearer Token 文本,仅接入方式为 Token 时保存,否则置空。</summary>
    public string? Token { get; set; }

    /// <summary>是否跳过 TLS 证书校验(默认 true,兼容自签证书环境)。</summary>
    public bool SkipTlsVerify { get; set; } = true;
}