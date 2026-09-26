namespace MultiClusterMgmtSys.Domain.Entities;

/// <summary>
/// Helm release 归属记录:安装成功时写入一条,用于「Admin 可操作任意 release、成员仅可操作自己安装的」权限判定。
/// Helm 本身不记录安装者身份,归属由本表维护;同一集群同一命名空间同一 release 名称至多一条记录,随所属集群级联删除。
/// 系统内卸载成功后删除记录;当前 revision 小于安装时 revision 视为已被系统外重装,降级为无主。
/// </summary>
public class HelmReleaseOwnership
{
    /// <summary>自增主键。</summary>
    public int Id { get; set; }

    /// <summary>所属集群 Id;集群删除时本记录随其级联删除。</summary>
    public int ClusterId { get; set; }

    /// <summary>所属集群导航属性。</summary>
    public ClusterInfo? Cluster { get; set; }

    /// <summary>release 所在命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>release 名称。</summary>
    public string ReleaseName { get; set; } = "";

    /// <summary>安装者账号 Id(Identity 主键);账号删除后保持原值,不再匹配任何登录用户。</summary>
    public int OwnerUserId { get; set; }

    /// <summary>安装者用户名快照,用于日志与追溯(界面不展示安装者列)。</summary>
    public string OwnerUserName { get; set; } = "";

    /// <summary>安装成功时间(UTC)。</summary>
    public DateTime InstalledAt { get; set; }

    /// <summary>安装成功时观察到的 revision;当前 revision 小于该值时视为系统外重装。</summary>
    public int InstalledRevision { get; set; }
}
