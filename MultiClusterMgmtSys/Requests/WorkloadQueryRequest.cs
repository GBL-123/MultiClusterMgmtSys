namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 列举工作负载(Deployment/StatefulSet/DaemonSet/ReplicaSet)的入参,
/// 由 <see cref="MultiClusterMgmtSys.Services.WorkloadService"/> 的 List*Async 系列方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">命名空间过滤;null = 列举全部命名空间。</param>
public record WorkloadQueryRequest(int ClusterId, string? Namespace);
