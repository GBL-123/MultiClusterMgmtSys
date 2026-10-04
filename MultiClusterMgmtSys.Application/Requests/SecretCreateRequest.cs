namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Secret 创建请求,携带 YAML 原文,
/// 由 <see cref="MultiClusterMgmtSys.Application.Services.SecretService"/> 的创建流程(CreateSecretFromYamlAsync)使用。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Yaml">用户提交的 Secret YAML 原文。</param>
public record SecretCreateRequest(int ClusterId, string Yaml);
