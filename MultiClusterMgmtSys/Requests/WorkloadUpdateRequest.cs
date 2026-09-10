namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 更新工作负载(Deployment/StatefulSet/DaemonSet/ReplicaSet)的入参,
/// 由 <see cref="MultiClusterMgmtSys.Services.WorkloadService"/> 的 Update*FromYamlAsync 系列方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">资源名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">资源所在命名空间。</param>
/// <param name="Yaml">修改后的资源清单 YAML 文本。</param>
public record WorkloadUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);
