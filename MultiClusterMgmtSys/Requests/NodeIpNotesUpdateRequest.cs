namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 批量保存某集群某节点 IP 备注的入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterNodeService"/> 的备注更新方法(UpdateNodeIpNotesAsync)消费;
/// 服务端按提交行对齐已有记录,增量新增/修改/删除该节点的备注。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="NodeName">目标节点名称。</param>
/// <param name="Items">该节点各 IP 的备注行(未出现的地址即删除,备注为 null 表示清除)。</param>
public record NodeIpNotesUpdateRequest(int ClusterId, string NodeName, IReadOnlyList<NodeIpNoteEditItem> Items);

/// <summary>
/// 节点 IP 备注编辑行：Dialog 每行一个 IP 的备注。
/// </summary>
public class NodeIpNoteEditItem
{
    /// <summary>节点 IP 地址(与已有记录按地址对齐;空白行会被服务端忽略)。</summary>
    public string Address { get; set; } = "";

    /// <summary>该 IP 的备注文本(最长 64 字符;null = 清除备注)。</summary>
    public string? Note { get; set; }
}