namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 定位单个 Pod 的入参,由 <see cref="MultiClusterMgmtSys.Services.PodService"/> 的详情方法(GetPodAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Namespace">Pod 所在命名空间。</param>
/// <param name="Name">Pod 名称。</param>
public record PodKeyRequest(int ClusterId, string Namespace, string Name);
