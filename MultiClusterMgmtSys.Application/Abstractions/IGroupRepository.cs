using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 集群分组的持久化端口:只做实体持久化与查询,不访问 Kubernetes API;
/// 业务规则(重名校验、移动分组时的审计等)由上层服务负责。
/// </summary>
public interface IGroupRepository
{
    /// <summary>查询全部分组,按 Id 升序,附带组内集群集合用于计算侧栏计数;无副作用。</summary>
    /// <returns>未跟踪的分组列表(含集群集合)。</returns>
    Task<List<ClusterGroup>> GetAllAsync();

    /// <summary>按 Id 查询单个分组,附带组内集群集合;不存在时返回 null。</summary>
    /// <param name="id">分组 Id。</param>
    /// <returns>分组实体;不存在为 null。</returns>
    Task<ClusterGroup?> GetByIdAsync(int id);

    /// <summary>新增分组并保存;返回带自增 Id 的实体,审计由上层服务写入。</summary>
    /// <param name="entity">待新增的分组。</param>
    /// <returns>保存后的分组实体(含生成的 Id)。</returns>
    Task<ClusterGroup> AddAsync(ClusterGroup entity);

    /// <summary>删除指定分组并保存;分组不存在时静默跳过。组内集群不删除,GroupId 由数据库 SetNull 置空。</summary>
    /// <param name="id">分组 Id。</param>
    Task DeleteAsync(int id);

    /// <summary>重命名指定分组并保存;分组不存在时静默跳过,重名校验由上层服务负责。</summary>
    /// <param name="id">分组 Id。</param>
    /// <param name="newName">新名称。</param>
    Task RenameAsync(int id, string newName);
}
