using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.Requests;

public record ClusterEndpointsUpdateRequest(int ClusterId, IReadOnlyList<ClusterEndpointEditItem> Items);

/// <summary>
/// 集群端点编辑行：编辑器（ClusterEndpointEditor）每行一条，作为提交输入传给服务。
/// Id == 0 表示新增行；持久化时服务端采用全量替换，Id 仅为编辑器内跟踪用。
/// IsDeleted 为编辑器软删除标记，提交时不传（删掉的行根本不出现在提交列表里）。
/// </summary>
public class ClusterEndpointEditItem
{
    public int Id { get; set; }

    public ClusterEndpointKind Kind { get; set; }

    public string Value { get; set; } = "";

    public string? Note { get; set; }

    public int SortOrder { get; set; }

    public bool IsDeleted { get; set; }
}