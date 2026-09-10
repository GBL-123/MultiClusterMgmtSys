namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 以 YAML 新建 Service 的入参,由 <see cref="MultiClusterMgmtSys.Services.SvcService"/> 的新建方法(CreateServiceFromYamlAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Yaml">资源清单 YAML 文本。</param>
public record SvcCreateRequest(int ClusterId, string Yaml);
