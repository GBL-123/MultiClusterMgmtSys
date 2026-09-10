namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 新建工作负载(Deployment/StatefulSet/DaemonSet/ReplicaSet)的入参,
/// 由 <see cref="MultiClusterMgmtSys.Services.WorkloadService"/> 的 Create*FromYamlAsync 系列方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Yaml">资源清单 YAML 文本。</param>
public record WorkloadCreateRequest(int ClusterId, string Yaml);
