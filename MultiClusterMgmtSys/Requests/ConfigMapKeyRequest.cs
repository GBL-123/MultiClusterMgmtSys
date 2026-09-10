namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// ConfigMap 资源键,定位单个 ConfigMap,
/// 由 <see cref="MultiClusterMgmtSys.Services.ConfigMapService"/> 的查看/删除方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">ConfigMap 名称(Kubernetes 对象名)。</param>
/// <param name="Namespace">ConfigMap 所在命名空间。</param>
public record ConfigMapKeyRequest(int ClusterId, string Name, string Namespace);