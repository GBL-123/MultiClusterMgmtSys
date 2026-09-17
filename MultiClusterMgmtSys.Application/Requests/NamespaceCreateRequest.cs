namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 以 YAML 新建命名空间的入参,
/// 由 <see cref="MultiClusterMgmtSys.Application.Services.NamespaceService"/> 的新建方法(CreateNamespaceFromYamlAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Yaml">命名空间清单 YAML 文本。</param>
public record NamespaceCreateRequest(int ClusterId, string Yaml);
