using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 集群端点：管理员手动登记的 VIP / 域名等可达性元数据。
/// 属于某个集群（<see cref="ClusterInfo"/>），级联删除。
/// 与集群连通性无关——离线集群的端点照常可读。
/// </summary>
public class ClusterEndpoint
{
    public int Id { get; set; }

    public int ClusterId { get; set; }

    public ClusterEndpointKind Kind { get; set; }

    public string Value { get; set; } = "";

    public string? Note { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    public ClusterInfo? Cluster { get; set; }
}
