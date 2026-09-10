namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 更新 Service 的入参,由 <see cref="MultiClusterMgmtSys.Services.SvcService"/> 的更新方法(UpdateServiceFromYamlAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">Service 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">Service 所在命名空间。</param>
/// <param name="Yaml">修改后的资源清单 YAML 文本。</param>
public record SvcUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);
