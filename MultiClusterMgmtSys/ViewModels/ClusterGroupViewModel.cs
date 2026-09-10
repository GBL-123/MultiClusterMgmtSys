namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 集群分组展示数据,用于分组列表与下拉选择。
/// </summary>
public class ClusterGroupViewModel
{
    /// <summary>分组主键。</summary>
    public int Id { get; set; }

    /// <summary>分组名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>组内集群数量。</summary>
    public int ClusterCount { get; set; }

    /// <summary>分组创建时间。</summary>
    public DateTime CreatedAt { get; set; }
}
