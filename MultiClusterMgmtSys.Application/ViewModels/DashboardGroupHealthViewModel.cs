namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 看板分组健康项:一个集群分组的成员规模与其中在线数;未分组集群作为独立一项展示。
/// </summary>
public class DashboardGroupHealthViewModel
{
    /// <summary>分组名称;未分组集群固定为「未分组」。</summary>
    public string GroupName { get; set; } = "";

    /// <summary>该分组下的集群总数。</summary>
    public int TotalClusters { get; set; }

    /// <summary>该分组下状态为在线的集群数。</summary>
    public int OnlineClusters { get; set; }
}
