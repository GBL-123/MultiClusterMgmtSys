namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 以 YAML 更新既有 Secret(值可含 <c>&lt;REDACTED:key&gt;</c> 占位符),由 <see cref="MultiClusterMgmtSys.Application.Services.SecretService"/> 的编辑方法(UpdateSecretFromYamlAsync)使用。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">Secret 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">Secret 所属命名空间。</param>
/// <param name="Yaml">用户提交的 Secret YAML 原文(回显占位符保留现值)。</param>
public record SecretUpdateRequest(int ClusterId, string Name, string Namespace, string Yaml);
