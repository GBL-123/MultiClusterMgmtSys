namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 调整工作负载副本数的入参,由 <see cref="MultiClusterMgmtSys.Services.WorkloadService"/> 的 Scale*Async 系列方法消费(Deployment/StatefulSet/ReplicaSet)。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">资源名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">资源所在命名空间。</param>
/// <param name="Replicas">目标副本数。</param>
public record WorkloadScaleRequest(int ClusterId, string Name, string Namespace, int Replicas);
