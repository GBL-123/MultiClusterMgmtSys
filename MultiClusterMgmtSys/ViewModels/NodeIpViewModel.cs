namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点 IP 条目展示数据(含管理员备注)。
/// </summary>
public class NodeIpViewModel
{
    /// <summary>IP 地址。</summary>
    public string Address { get; set; } = "";

    /// <summary>管理员维护的备注;未设置时为 null。</summary>
    public string? Note { get; set; }
}
