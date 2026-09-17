namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 列举事件列表的入参,由 <see cref="MultiClusterMgmtSys.Application.Services.EventService"/> 的列举方法(ListEventsAsync)消费。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
public record EventQueryRequest(int ClusterId);
