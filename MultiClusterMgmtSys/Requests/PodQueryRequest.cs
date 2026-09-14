namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 列举 Pod 的入参,由 <see cref="MultiClusterMgmtSys.Services.PodService"/> 的列举方法(ListPodsAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">命名空间过滤;null = 列举全部命名空间。</param>
/// <param name="LabelSelector">K8s label selector 过滤(工作负载直达用);null = 不过滤。提供时必须同时给定命名空间。</param>
public record PodQueryRequest(int ClusterId, string? Namespace, string? LabelSelector = null);
