using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// 告警记录的持久化端口:负责告警的开立、解析与查询,不访问 Kubernetes API。
/// open 与 ResolvedAt 为空等价;同一集群同一规则至多一条 open 记录(库内部分唯一索引兜底)。
/// </summary>
public interface IAlertRepository
{
    /// <summary>查询当前全部 open 告警(ResolvedAt 为空),无跟踪查询;供评估器与现有状态比对做开立/解析 diff;无副作用。</summary>
    /// <returns>open 告警记录列表(按 Id 升序)。</returns>
    Task<List<AlertRecord>> GetOpenAsync();

    /// <summary>统计当前 open 告警总数(ResolvedAt 为空),供铃铛角标展示;无副作用。</summary>
    /// <returns>open 告警条数。</returns>
    Task<int> CountOpenAsync();

    /// <summary>按状态过滤分页查询告警记录:开立时间倒序(并列时 Id 倒序稳定次序),页码/页大小小于 1 时按 1 处理。</summary>
    /// <param name="query">状态过滤与分页条件(Resolved 为 null = 全部,false = 仅 open,true = 仅 resolved)。</param>
    /// <returns>当页告警记录列表,以及过滤后(分页前)的命中总数。</returns>
    Task<(List<AlertRecord> Items, int Total)> GetPagedAsync(AlertPageQuery query);

    /// <summary>开立一条告警并立即保存(ResolvedAt 保持为空);由评估器在规则命中且无同键 open 记录时调用。</summary>
    /// <param name="record">待开立的告警记录(含集群 Id、规则类别与开立时间)。</param>
    Task AddAsync(AlertRecord record);

    /// <summary>把指定告警解析为 resolved(回填解析时间并保存);记录不存在或已解析时静默跳过,不做重复解析。</summary>
    /// <param name="id">告警记录 Id。</param>
    /// <param name="resolvedAtUtc">解析时间(UTC)。</param>
    Task ResolveAsync(int id, DateTime resolvedAtUtc);
}
