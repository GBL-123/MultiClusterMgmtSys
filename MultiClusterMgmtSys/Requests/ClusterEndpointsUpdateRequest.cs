using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

/// <summary>
/// 批量更新集群端点的入参,由 <see cref="MultiClusterMgmtSys.Services.ClusterService"/> 的更新端点方法(UpdateClusterEndpointsAsync)消费;
/// 服务端按提交列表全量替换该集群的端点,集群不存在时抛业务异常。
/// </summary>
/// <param name="ClusterId">目标集群 Id(数据库主键)。</param>
/// <param name="Items">端点编辑行全量列表(软删除的行不包含在内),行结构见 <see cref="ClusterEndpointEditItem"/>。</param>
public record ClusterEndpointsUpdateRequest(int ClusterId, IReadOnlyList<ClusterEndpointEditItem> Items);

/// <summary>
/// 集群端点编辑行：编辑器（ClusterEndpointEditor）每行一条，作为提交输入传给服务。
/// Id == 0 表示新增行；持久化时服务端采用全量替换，Id 仅为编辑器内跟踪用。
/// IsDeleted 为编辑器软删除标记，提交时不传（删掉的行根本不出现在提交列表里）。
/// </summary>
public class ClusterEndpointEditItem
{
    /// <summary>编辑器内行 Id:0 = 新增行,大于 0 = 已有端点;服务端全量替换,仅编辑器内跟踪用。</summary>
    public int Id { get; set; }

    /// <summary>端点类别(VIP / 域名),见 <see cref="ClusterEndpointKind"/>。</summary>
    public ClusterEndpointKind Kind { get; set; }

    /// <summary>端点地址文本(VIP 地址或域名)。</summary>
    public string Value { get; set; } = "";

    /// <summary>备注说明;null 或空 = 无备注。</summary>
    public string? Note { get; set; }

    /// <summary>显示顺序(数字越小越靠前)。</summary>
    public int SortOrder { get; set; }

    /// <summary>编辑器软删除标记,仅在编辑器内部使用;提交列表中已不包含被删行。</summary>
    public bool IsDeleted { get; set; }
}