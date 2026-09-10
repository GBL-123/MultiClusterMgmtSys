namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// Service 资源键,定位单个 Service,
/// 由 <see cref="MultiClusterMgmtSys.Services.SvcService"/> 的查看/端点列举/删除方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">Service 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">Service 所在命名空间。</param>
public record SvcKeyRequest(int ClusterId, string Name, string Namespace);
