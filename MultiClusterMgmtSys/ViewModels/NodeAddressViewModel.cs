namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 节点地址条目展示数据(含管理员备注)。
/// </summary>
public class NodeAddressViewModel
{
    /// <summary>地址类型,如 InternalIP/ExternalIP/Hostname。</summary>
    public string Type { get; set; } = "";

    /// <summary>地址值。</summary>
    public string Address { get; set; } = "";

    /// <summary>管理员维护的备注;仅 InternalIP/ExternalIP 可维护,未设置时为 null。</summary>
    public string? Note { get; set; }
}
