namespace MultiClusterMgmtSys.Application.Models;

/// <summary>
/// 告警记录分页查询对象:仓库执行的纯数据查询契约,
/// 由 <see cref="MultiClusterMgmtSys.Application.Services.AlertService"/> 从 <see cref="MultiClusterMgmtSys.Application.Requests.AlertListRequest"/> 翻译而来。
/// </summary>
public class AlertPageQuery
{
    /// <summary>
    /// 状态过滤:<c>null</c> = 不过滤(全部);
    /// <c>false</c> = 仅 open(未解析,ResolvedAt 为空);
    /// <c>true</c> = 仅 resolved(已解析,ResolvedAt 非空)。
    /// </summary>
    public bool? Resolved { get; set; }

    /// <summary>页码,从 1 起(小于 1 按为 1 处理)。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数(小于 1 按 1 处理)。</summary>
    public int PageSize { get; set; } = 20;
}
