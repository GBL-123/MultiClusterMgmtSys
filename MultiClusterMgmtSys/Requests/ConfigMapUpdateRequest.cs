namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 更新 ConfigMap 的入参,由 <see cref="MultiClusterMgmtSys.Services.ConfigMapService"/> 的更新方法(UpdateConfigMapFromYamlAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">ConfigMap 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">ConfigMap 所在命名空间。</param>
/// <param name="Yaml">修改后的资源清单 YAML 文本。</param>
public record ConfigMapUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);