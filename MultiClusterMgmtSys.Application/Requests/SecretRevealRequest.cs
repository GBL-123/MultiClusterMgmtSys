namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 查看 Secret 指定键的明文,由 <see cref="MultiClusterMgmtSys.Application.Services.SecretService"/> 的揭示方法(RevealSecretKeyAsync)使用。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">Secret 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">Secret 所属命名空间。</param>
/// <param name="Key">要揭示明文的键名(审计只记录键名,不记录值)。</param>
public record SecretRevealRequest(int ClusterId, string Name, string Namespace, string Key);
