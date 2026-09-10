namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 工作负载资源键,定位单个 Deployment/StatefulSet/DaemonSet/ReplicaSet,
/// 由 <see cref="MultiClusterMgmtSys.Services.WorkloadService"/> 的 Get*/Delete*/Restart* 系列方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">资源名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">资源所在命名空间。</param>
public record WorkloadKeyRequest(int ClusterId, string Name, string Namespace);
