using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 集群端点：管理员手动登记的 VIP / 域名等可达性元数据。
/// 属于某个集群（<see cref="ClusterInfo"/>），级联删除。
/// 与集群连通性无关——离线集群的端点照常可读。
/// </summary>
public class ClusterEndpoint
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>所属集群 Id;外键级联自 <see cref="ClusterInfo"/>,删除集群时一并删除。</summary>
    public int ClusterId { get; set; }

    /// <summary>端点类别(VIP 或域名),见 <see cref="ClusterEndpointKind"/>。</summary>
    public ClusterEndpointKind Kind { get; set; }

    /// <summary>端点值(VIP 地址或域名),必填,最长 256。</summary>
    public string Value { get; set; } = "";

    /// <summary>管理员备注(如「主入口」),可空,最长 64。</summary>
    public string? Note { get; set; }

    /// <summary>展示排序号,决定端点在列表中的先后顺序。</summary>
    public int SortOrder { get; set; }

    /// <summary>所属集群导航属性。</summary>
    public ClusterInfo? Cluster { get; set; }
}
