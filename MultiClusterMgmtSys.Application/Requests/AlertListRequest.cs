namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// 告警列表查询入参,由 <see cref="MultiClusterMgmtSys.Application.Services.AlertService"/> 的查询方法(GetAlertsAsync)消费;
/// 前端把表格状态与状态过滤收拢后传入。
/// </summary>
public class AlertListRequest
{
    /// <summary>
    /// 状态过滤:<c>null</c> = 全部;
    /// <c>false</c> = 仅 open(默认,告警中);
    /// <c>true</c> = 仅 resolved(已解析)。
    /// </summary>
    public bool? Resolved { get; set; } = false;

    /// <summary>页码,从 1 起(小于 1 被服务层归一为 1)。</summary>
    public int Page { get; set; } = 1;

    /// <summary>每页条数,默认 20(小于 1 被服务层归一为 1)。</summary>
    public int PageSize { get; set; } = 20;
}
