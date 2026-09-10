namespace MultiClusterMgmtSys.Data.Entities;

/// <summary>
/// 集群分组:对集群的纯管理性归类(不参与 K8s 逻辑),名称必填。
/// 删除分组不会删除集群——其下集群的 GroupId 按 SetNull 置空,退回未分组。
/// </summary>
public class ClusterGroup
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>分组名称,必填且在界面上唯一展示。</summary>
    public string Name { get; set; } = "";

    /// <summary>创建时间。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>组内集群集合,用于侧栏分组计数与批量移动;不代表级联删除。</summary>
    public List<ClusterInfo> Clusters { get; set; } = [];
}
