namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 查询单个节点详情的入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterNodeService"/> 的节点详情方法(GetNodeDetailAsync)消费。
/// </summary>
/// <param name="ClusterId">集群 Id(数据库主键)。</param>
/// <param name="NodeName">节点名称(Kubernetes Node 名称)。</param>
public record NodeDetailQueryRequest(int ClusterId, string NodeName);