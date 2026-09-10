namespace MultiClusterMgmtSys.Models;

/// <summary>
/// 节点列表的前端过滤条件(纯前端状态,数据已在内存中,不进仓库层)。
/// </summary>
public class NodeListFilter
{
    /// <summary>节点名称模糊匹配(默认空 = 不过滤)。</summary>
    public string Name { get; set; } = "";

    /// <summary>节点角色过滤(control-plane/worker;null = 不过滤)。</summary>
    public string? Role { get; set; }

    /// <summary>节点状态过滤(Ready/NotReady 等;null = 不过滤)。</summary>
    public string? Status { get; set; }

    /// <summary>是否可调度过滤(null = 不过滤)。</summary>
    public bool? Schedulable { get; set; }

    /// <summary>是否有任一过滤条件生效(决定 UI 是否显示"重置")。</summary>
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrEmpty(Role)
        || !string.IsNullOrEmpty(Status)
        || Schedulable.HasValue;

    /// <summary>清空全部过滤条件,恢复默认状态。</summary>
    public void Reset()
    {
        Name = "";
        Role = null;
        Status = null;
        Schedulable = null;
    }
}