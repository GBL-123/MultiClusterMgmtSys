using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// 拓扑布局纯函数:为节点计算层号与层内序号(契约见 resource-topology)。
/// 中心节点层号 0、序号 0;直接邻居层号 1;邻居行内先上游组(Service/工作负载/Node)后下游组(ConfigMap/Secret/PVC/Ingress/PV/ReplicaSet),
/// 组内按名称字典序;同输入必得同结果(确定性)。
/// </summary>
public static class TopologyLayout
{
    /// <summary>上游组 Kind 集合(邻居行靠前:选择/拥有/调度等"供给中心"的关系对象)。</summary>
    private static readonly HashSet<string> UpstreamKinds = new(StringComparer.Ordinal)
    {
        "Service", "Deployment", "StatefulSet", "DaemonSet", "Job", "Node",
    };

    /// <summary>下游组 Kind 集合(邻居行靠后:被中心消费或旁路的对象)。</summary>
    private static readonly HashSet<string> DownstreamKinds = new(StringComparer.Ordinal)
    {
        "ConfigMap", "Secret", "PersistentVolumeClaim", "Ingress", "PersistentVolume", "ReplicaSet",
    };

    /// <summary>
    /// 计算布局(就地写回每个节点的 Layer 与 Order)。
    /// </summary>
    /// <param name="nodes">拓扑节点集合(含恰好一个中心节点)。</param>
    public static void Apply(IReadOnlyList<TopologyNodeViewModel> nodes)
    {
        var index = 0;
        var ordered = nodes
            .Where(n => !n.IsCenter)
            .OrderBy(n => GroupOf(n.Kind))
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .ThenBy(n => n.Kind, StringComparer.Ordinal)
            .ThenBy(n => n.Namespace, StringComparer.Ordinal);
        foreach (var node in ordered)
        {
            node.Layer = 1;
            node.Order = index;
            index++;
        }

        var center = nodes.FirstOrDefault(n => n.IsCenter);
        if (center is not null)
        {
            center.Layer = 0;
            center.Order = 0;
        }
    }

    /// <summary>Kind 所属分组:上游 0、下游 1、未知 2(未知类型稳定排最后)。</summary>
    private static int GroupOf(string kind) => kind switch
    {
        var k when UpstreamKinds.Contains(k) => 0,
        var k when DownstreamKinds.Contains(k) => 1,
        _ => 2,
    };
}
