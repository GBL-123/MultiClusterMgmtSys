namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 节点排空(预检与执行)的定位入参,由 <see cref="MultiClusterMgmtSys.Application.Services.ClusterNodeService"/> 的排空方法消费。
/// </summary>
/// <param name="ClusterId">集群 Id(数据库主键)。</param>
/// <param name="NodeName">节点名称(Kubernetes Node 名称)。</param>
public record NodeDrainRequest(int ClusterId, string NodeName);
