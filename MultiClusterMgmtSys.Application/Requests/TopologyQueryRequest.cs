namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 拓扑查询入参:以单个 K8s 资源为中心聚合一跳关系,由 <see cref="MultiClusterMgmtSys.Application.Services.TopologyService"/> 消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">中心资源所在命名空间(集群级资源传空)。</param>
/// <param name="Kind">中心资源类型(原始 Kind,如 Pod / Service)。</param>
/// <param name="Name">中心资源名称。</param>
public record TopologyQueryRequest(int ClusterId, string Namespace, string Kind, string Name);
