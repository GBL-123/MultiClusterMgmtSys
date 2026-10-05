using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 跨集群 YAML 对照的入参,由 <see cref="MultiClusterMgmtSys.Application.Services.ClusterCompareService"/> 的对照方法(GetPairAsync)消费。
/// </summary>
/// <param name="SourceClusterId">源集群 Id(数据库主键)。</param>
/// <param name="TargetClusterId">对照集群 Id(数据库主键)。</param>
/// <param name="Kind">资源族(五选一)。</param>
/// <param name="Namespace">同名对照的命名空间(同 K8s 对象名)。</param>
/// <param name="Name">资源名称(Kubernetes 对象名)。</param>
public record ComparePairQueryRequest(
    int SourceClusterId,
    int TargetClusterId,
    CompareKind Kind,
    string Namespace,
    string Name);
