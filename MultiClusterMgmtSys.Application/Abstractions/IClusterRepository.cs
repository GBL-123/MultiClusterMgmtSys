using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 集群持久化端口:只负责持久化与查询翻译,不访问 Kubernetes API;
/// 过滤/排序/分页语义由实现翻译为 SQL(契约见 cluster-query-layering spec)。
/// </summary>
public interface IClusterRepository
{
    /// <summary>按 Id 加载集群,附带分组、端点与节点 IP 备注集合(跟踪查询,可修改后保存);不存在时返回 null。</summary>
    /// <param name="id">集群 Id。</param>
    /// <returns>集群实体;不存在为 null。</returns>
    Task<ClusterInfo?> GetByIdAsync(int id);

    /// <summary>新增集群并保存;返回带自增 Id 的实体,审计由上层服务写入。</summary>
    /// <param name="entity">待新增的集群。</param>
    /// <returns>保存后的集群实体(含生成的 Id)。</returns>
    Task<ClusterInfo> AddAsync(ClusterInfo entity);

    /// <summary>将集群实体标记为已修改并保存(全字段更新),供编辑、状态同步等场景使用。</summary>
    /// <param name="entity">待更新的集群实体。</param>
    Task UpdateAsync(ClusterInfo entity);

    /// <summary>删除指定集群并保存;不存在时静默跳过,其端点与节点 IP 备注级联删除。</summary>
    /// <param name="id">集群 Id。</param>
    Task DeleteAsync(int id);

    /// <summary>
    /// 按查询条件分页筛选集群:GroupId null=不过滤、0=未分组哨兵、正数=精确匹配;
    /// 排序后以 Id 倒序追加为稳定次序键,页码/页大小小于 1 时按 1 处理。
    /// </summary>
    /// <param name="q">过滤、排序与分页条件。</param>
    /// <returns>当页集群列表,以及过滤后(分页前)的命中总数。</returns>
    Task<(List<ClusterInfo> Items, int Total)> GetPagedAsync(ClusterPageQuery q);

    /// <summary>查询全部非空集群版本,去重后按版本号升序,供版本筛选下拉使用;无副作用。</summary>
    /// <returns>去重升序后的版本列表。</returns>
    Task<List<string>> GetDistinctVersionsAsync();

    /// <summary>以单条批量 UPDATE 修改一组集群的所属分组(targetGroupId 传 null 即移出分组);集群 Id 列表为空时返回 0。</summary>
    /// <param name="clusterIds">目标集群 Id 集合。</param>
    /// <param name="targetGroupId">目标分组 Id;null 表示脱离分组。</param>
    /// <returns>受影响的行数。</returns>
    Task<int> SetGroupIdForClustersAsync(IEnumerable<int> clusterIds, int? targetGroupId);

    /// <summary>统计未分组(GroupId 为空)的集群数量;无副作用。</summary>
    /// <returns>未分组集群数。</returns>
    Task<int> CountUngroupedAsync();

    /// <summary>查询全部集群 Id,用于全量同步等批量任务;无副作用。</summary>
    /// <returns>全部集群 Id 列表。</returns>
    Task<List<int>> GetAllIdsAsync();

    /// <summary>加载全部集群(跟踪查询,不加载导航集合),供全量状态刷新的探测与落库使用;无副作用。</summary>
    /// <returns>全部集群实体(处于跟踪状态,可修改后经 <see cref="UpdateAsync"/> 保存)。</returns>
    Task<List<ClusterInfo>> GetAllForSyncAsync();
}
