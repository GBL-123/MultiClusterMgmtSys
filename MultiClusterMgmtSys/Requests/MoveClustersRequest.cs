namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 批量移动集群到目标分组的入参,由 <see cref="MultiClusterMgmtSys.Services.GroupService"/> 的移动方法(MoveClustersToGroupAsync)消费。
/// </summary>
/// <param name="ClusterIds">要移动的集群 Id 列表。</param>
/// <param name="TargetGroupId">目标分组 Id;null = 移出分组(变为未分组);0 为无效哨兵,服务层会拒绝。</param>
public record MoveClustersRequest(IReadOnlyList<int> ClusterIds, int? TargetGroupId);