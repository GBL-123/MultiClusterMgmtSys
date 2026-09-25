using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 集群节点健康快照的持久化端口:只负责追加与查询,不访问 Kubernetes API。
/// 快照只追加不覆盖,读取以「每集群最新一条」为主,该条即代表集群最近一次成功探测的结果。
/// </summary>
public interface IClusterHealthRepository
{
    /// <summary>追加一条节点健康快照并立即保存;由上层服务在探测成功后调用。</summary>
    /// <param name="snapshot">待追加的快照记录(含集群 Id、采集时间与就绪统计)。</param>
    Task AddAsync(ClusterHealthSnapshot snapshot);

    /// <summary>查询每个集群最近一条快照(同一集群内按采集时间倒序、再按 Id 倒序取首条);无任何快照的集群不出现在结果中,无副作用。</summary>
    /// <returns>集群 Id 到该集群最新一条快照的映射。</returns>
    Task<Dictionary<int, ClusterHealthSnapshot>> GetLatestPerClusterAsync();
}
