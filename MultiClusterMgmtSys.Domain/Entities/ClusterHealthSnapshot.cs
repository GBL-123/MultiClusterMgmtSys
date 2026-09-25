namespace MultiClusterMgmtSys.Domain.Entities;

/// <summary>
/// 集群节点健康快照:后台同步每次成功探测一个集群时追加一条,记录当轮采集到的节点就绪统计。
/// 记录只追加不覆盖,同一集群的多条记录按采集时间排列即构成节点健康的时间序列;
/// 探测失败与停机取消不写记录,因此「最新一条」始终代表该集群最近一次成功探测的结果。
/// 随所属集群级联删除。
/// </summary>
public class ClusterHealthSnapshot
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>所属集群 Id;集群删除时本记录随其级联删除。</summary>
    public int ClusterId { get; set; }

    /// <summary>所属集群导航属性。</summary>
    public ClusterInfo? Cluster { get; set; }

    /// <summary>采集时间(UTC),即该轮探测成功并写入本记录的时刻。</summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>当轮采集到的节点总数。</summary>
    public int TotalNodes { get; set; }

    /// <summary>当轮采集到的就绪节点数(Ready 条件为 True 的节点)。</summary>
    public int ReadyNodes { get; set; }

    /// <summary>当轮采集到的未就绪节点数(Ready 条件为 False 或 Unknown,以及缺失 Ready 条件的节点);与就绪数之和恒等于节点总数。</summary>
    public int NotReadyNodes { get; set; }
}
