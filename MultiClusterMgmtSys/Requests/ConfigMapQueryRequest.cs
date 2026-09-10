namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 列举 ConfigMap 的入参,由 <see cref="MultiClusterMgmtSys.Services.ConfigMapService"/> 的列举方法(ListConfigMapsAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">命名空间过滤;null = 列举全部命名空间。</param>
public record ConfigMapQueryRequest(int ClusterId, string? Namespace);