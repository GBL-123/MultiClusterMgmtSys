namespace MultiClusterMgmtSys.Components.Common;

/// <summary>
/// 跨页面记住当前选中集群的会话级状态(scoped,Blazor Server 电路内共享)。
/// </summary>
public class ClusterSelectionState
{
    /// <summary>当前选中集群 Id(null = 未选中)。</summary>
    public int? SelectedClusterId { get; private set; }

    /// <summary>记录当前选中的集群 Id。</summary>
    public void Set(int clusterId)
    {
        SelectedClusterId = clusterId;
    }

    /// <summary>清除选中状态(切到无集群上下文的页面)。</summary>
    public void Clear()
    {
        SelectedClusterId = null;
    }
}