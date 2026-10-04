namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 查询 Secret 列表,由 <see cref="MultiClusterMgmtSys.Application.Services.SecretService"/> 的列表方法(ListSecretsAsync)使用。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">命名空间过滤;null = 查询全部命名空间。</param>
public record SecretQueryRequest(int ClusterId, string? Namespace);
