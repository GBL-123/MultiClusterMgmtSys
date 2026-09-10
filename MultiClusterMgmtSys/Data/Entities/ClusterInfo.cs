using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 集群主档:保存名称、API 地址、访问凭据及由后台同步维护的可达状态。
/// 凭据按 <see cref="ConnectionType"/> 二选一使用(KubeConfig 文本或 Token 文本);
/// 所属分组删除时 GroupId 置空(SetNull),端点与节点 IP 备注随本实体级联删除。
/// </summary>
public class ClusterInfo
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>集群名称,必填,页面以此为主要标识。</summary>
    public string Name { get; set; } = "";

    /// <summary>API Server 地址(Token 直连方式使用),可空。</summary>
    public string? ApiServer { get; set; }

    /// <summary>kubeconfig 文本(YAML,TEXT 列),KubeConfig 接入方式使用,可空。</summary>
    public string? KubeConfig { get; set; }

    /// <summary>凭据接入方式:决定构建 K8s 客户端时使用 KubeConfig 还是 Token。</summary>
    public ConnectionType ConnectionType { get; set; }

    /// <summary>Bearer Token 文本(TEXT 列),Token 直连方式使用,可空。</summary>
    public string? Token { get; set; }

    /// <summary>是否跳过 TLS 证书校验,默认 true。</summary>
    public bool SkipTlsVerify { get; set; } = true;

    /// <summary>可达状态,由后台同步探测得出,见 <see cref="ClusterStatus"/>。</summary>
    public ClusterStatus Status { get; set; }

    /// <summary>探测得到的 Kubernetes 版本;未探测成功时为空,参与版本筛选。</summary>
    public string? Version { get; set; }

    /// <summary>最近一次同步统计到的节点数量。</summary>
    public int NodeCount { get; set; }

    /// <summary>所属分组 Id,可空(null = 未分组);删除分组时由数据库置空。</summary>
    public int? GroupId { get; set; }

    /// <summary>所属分组导航属性。</summary>
    public ClusterGroup? Group { get; set; }

    /// <summary>登记的端点集合(管理员维护的 VIP/域名),随集群级联删除。</summary>
    public ICollection<ClusterEndpoint> Endpoints { get; set; } = [];

    /// <summary>节点 IP 备注集合,随集群级联删除。</summary>
    public ICollection<NodeIpRemark> NodeIpRemarks { get; set; } = [];

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>最近一次可达性探测时间,可空(从未探测过则为空)。</summary>
    public DateTime? LastCheckedAt { get; set; }
}
