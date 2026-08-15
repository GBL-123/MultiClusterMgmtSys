namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 节点 IP 备注：管理员手动登记的节点 IP 功能说明（如管理口 / 数据口）。
/// 键为 (ClusterId, NodeName, Address)，与 k8s 节点实时地址对应，级联删除。
/// </summary>
public class NodeIpRemark
{
    public int Id { get; set; }

    public int ClusterId { get; set; }

    public string NodeName { get; set; } = "";

    public string Address { get; set; } = "";

    public string? Note { get; set; }

    public ClusterInfo? Cluster { get; set; }
}
