namespace MultiClusterMgmtSys.Components.Common;

public class ClusterSelectionState
{
    public int? SelectedClusterId { get; private set; }

    public void Set(int clusterId)
    {
        SelectedClusterId = clusterId;
    }

    public void Clear()
    {
        SelectedClusterId = null;
    }
}