namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 新建 ConfigMap 的入参,由 <see cref="MultiClusterMgmtSys.Services.ConfigMapService"/> 的新建方法(CreateConfigMapFromYamlAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Yaml">资源清单 YAML 文本。</param>
public record ConfigMapCreateRequest(int ClusterId, string Yaml);