namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 定位单个 Secret,由 <see cref="MultiClusterMgmtSys.Application.Services.SecretService"/> 的详情/删除方法使用。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">Secret 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">Secret 所属命名空间。</param>
public record SecretKeyRequest(int ClusterId, string Name, string Namespace);
