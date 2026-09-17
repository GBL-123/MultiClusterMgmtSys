namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 命名空间资源键,定位单个 Namespace,
/// 由 <see cref="MultiClusterMgmtSys.Application.Services.NamespaceService"/> 的查看/删除方法消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Name">命名空间名称(Kubernetes 对象名)。</param>
public record NamespaceKeyRequest(int ClusterId, string Name);
