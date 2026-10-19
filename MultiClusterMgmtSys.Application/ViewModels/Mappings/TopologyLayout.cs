using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// 拓扑布局纯函数:为中心拓扑计算渲染侧(上行/下行)、下行分组与确定性渲染顺序(契约见 resource-topology)。
/// 渲染侧取边关系:拥有/路由/供给边的起点渲染于中心上方(属主链、Ingress、PV 等「指向中心」的对象),
/// 其余邻居渲染于中心下方;下行侧同 Kind 邻居 ≥2 个时按 Kind 聚合成组卡片;同输入必得同结果(确定性)。
/// </summary>
public static class TopologyLayout
{
    /// <summary>渲染于中心上方的边关系集合(这些关系的起点是「指向中心」的对象)。</summary>
    private static readonly HashSet<string> AboveRelations = new(StringComparer.Ordinal)
    {
        "拥有", "路由", "供给",
    };

    /// <summary>关系名合并优先级(节点携带多个关系时按此排序后以 " / " 连接)。</summary>
    private static readonly string[] RelationPriority =
    [
        "调度", "拥有", "选择", "路由", "挂载", "引用", "供给",
    ];

    /// <summary>
    /// 计算布局语义(就地写回每个节点的 IsAbove/Group/Order/Relation)。
    /// </summary>
    /// <param name="nodes">拓扑节点集合(含恰好一个中心节点)。</param>
    /// <param name="edges">拓扑边集合(用于判定渲染侧与逐节点关系名)。</param>
    public static void Apply(IReadOnlyList<TopologyNodeViewModel> nodes, IReadOnlyList<TopologyEdgeViewModel> edges)
    {
        var center = nodes.FirstOrDefault(n => n.IsCenter);
        if (center is not null)
        {
            center.IsAbove = false;
            center.Group = null;
            center.Order = 0;
            center.Relation = "";
        }

        var aboveIds = edges
            .Where(e => AboveRelations.Contains(e.Relation))
            .Select(e => e.From)
            .ToHashSet(StringComparer.Ordinal);

        var neighbors = nodes.Where(n => !n.IsCenter).ToList();
        foreach (var node in neighbors)
        {
            node.IsAbove = aboveIds.Contains(node.Id);
            node.Relation = JoinRelations(edges, node.Id);
        }

        var belowKindCounts = neighbors
            .Where(n => !n.IsAbove)
            .GroupBy(n => n.Kind, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        foreach (var node in neighbors)
        {
            node.Group = !node.IsAbove && belowKindCounts.GetValueOrDefault(node.Kind) >= 2
                ? node.Kind
                : null;
        }

        var ordered = neighbors
            .OrderBy(n => n.IsAbove ? 0 : 1)
            .ThenBy(n => n.Group ?? "", StringComparer.Ordinal)
            .ThenBy(n => n.Kind, StringComparer.Ordinal)
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .ThenBy(n => n.Namespace, StringComparer.Ordinal)
            .ThenBy(n => n.Id, StringComparer.Ordinal);
        var index = 1;
        foreach (var node in ordered)
        {
            node.Order = index;
            index++;
        }
    }

    /// <summary>合并节点与中心之间去重后的关系名(按固定优先级排序,以 " / " 连接)。</summary>
    private static string JoinRelations(IReadOnlyList<TopologyEdgeViewModel> edges, string nodeId)
    {
        var relations = edges
            .Where(e => e.From == nodeId || e.To == nodeId)
            .Select(e => e.Relation)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(RelationPriorityOf)
            .ThenBy(r => r, StringComparer.Ordinal);
        return string.Join(" / ", relations);
    }

    /// <summary>关系名在合并优先级序列中的位置;未登记关系排最后(字典序兜底)。</summary>
    private static int RelationPriorityOf(string relation)
    {
        var index = Array.IndexOf(RelationPriority, relation);
        return index < 0 ? int.MaxValue : index;
    }
}
