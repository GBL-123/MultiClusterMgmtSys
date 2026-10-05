using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 跨集群一键克隆的入参,由 <see cref="MultiClusterMgmtSys.Application.Services.ClusterCompareService"/> 的克隆方法(CloneAsync)消费。
/// </summary>
/// <param name="SourceClusterId">源集群 Id(数据库主键)。</param>
/// <param name="TargetClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Kind">资源族(五选一)。</param>
/// <param name="Namespace">同名对照的命名空间(同 K8s 对象名,克隆保持不变)。</param>
/// <param name="Name">资源名称(Kubernetes 对象名,克隆保持不变)。</param>
public record CompareCloneRequest(
    int SourceClusterId,
    int TargetClusterId,
    CompareKind Kind,
    string Namespace,
    string Name);
