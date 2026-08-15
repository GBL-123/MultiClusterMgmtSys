namespace MultiClusterMgmtSys.Components.Nodes.Requests;

/// <summary>
/// 节点 IP 备注编辑行：Dialog 每行一个 IP 的备注。
/// </summary>
public class NodeIpNoteEditItem
{
    public string Address { get; set; } = "";
    public string? Note { get; set; }
}
