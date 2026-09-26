namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm release 资源键:定位单个 release,
/// 由 Helm 服务与页面消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">release 名称。</param>
/// <param name="Namespace">release 所在命名空间。</param>
public record HelmReleaseKeyRequest(int ClusterId, string Name, string Namespace);
