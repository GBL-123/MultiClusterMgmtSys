namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 节点 IP 备注：管理员手动登记的节点 IP 功能说明（如管理口 / 数据口）。
/// 键为 (ClusterId, NodeName, Address)，与 k8s 节点实时地址对应，级联删除。
/// </summary>
public class NodeIpRemark
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>所属集群 Id;随集群级联删除,且参与唯一索引。</summary>
    public int ClusterId { get; set; }

    /// <summary>Kubernetes 节点名称,备注归属键之一,参与唯一索引。</summary>
    public string NodeName { get; set; } = "";

    /// <summary>节点 IP 地址,备注归属键之一,参与唯一索引;仅节点 InternalIP/ExternalIP 类型的地址会与本表合并。</summary>
    public string Address { get; set; } = "";

    /// <summary>管理员对该 IP 的功能备注(如「管理口」「数据口」),可空,最长 64。</summary>
    public string? Note { get; set; }

    /// <summary>所属集群导航属性。</summary>
    public ClusterInfo? Cluster { get; set; }
}
