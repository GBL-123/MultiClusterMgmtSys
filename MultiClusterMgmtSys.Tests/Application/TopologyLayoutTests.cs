using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Application;

/// <summary>TopologyLayout 布局纯函数测试:确定性、层内组顺序、中心与邻居分层(契约:确定性布局)。</summary>
public class TopologyLayoutTests
{
    private static TopologyNodeViewModel Node(string kind, string name, bool isCenter = false) => new()
    {
        Id = $"{kind}/{name}",
        Kind = kind,
        Name = name,
        IsCenter = isCenter
    };

    private static TopologyNodeViewModel PodCenter() => Node("Pod", "web-0", isCenter: true);

    [Fact(DisplayName = "中心节点 Layer=0 Order=0,邻居节点 Layer=1 且 Order 从 0 连续递增")]
    public void Apply_CenterAndNeighborLayers()
    {
        var center = PodCenter();
        var nodes = new List<TopologyNodeViewModel>
        {
            Node("ConfigMap", "app-config"),
            center,
            Node("Service", "web-svc")
        };

        TopologyLayout.Apply(nodes);

        Assert.Equal(0, center.Layer);
        Assert.Equal(0, center.Order);
        var neighbors = nodes.Where(n => !n.IsCenter).ToList();
        foreach (var neighbor in neighbors)
        {
            Assert.Equal(1, neighbor.Layer);
        }
        Assert.Equal([0, 1], neighbors.OrderBy(n => n.Order).Select(n => n.Order));
    }

    [Fact(DisplayName = "同一输入两次 Apply 结果完全一致(确定性)")]
    public void Apply_IsDeterministic()
    {
        var first = new List<TopologyNodeViewModel>
        {
            Node("PVC", "data-vol"),
            PodCenter(),
            Node("Node", "node-a"),
            Node("ConfigMap", "zz-config"),
            Node("Service", "web-svc")
        };
        var second = new List<TopologyNodeViewModel>
        {
            Node("PVC", "data-vol"),
            PodCenter(),
            Node("Node", "node-a"),
            Node("ConfigMap", "zz-config"),
            Node("Service", "web-svc")
        };

        TopologyLayout.Apply(first);
        TopologyLayout.Apply(second);

        Assert.Equal(
            first.Select(n => (n.Id, n.Layer, n.Order)),
            second.Select(n => (n.Id, n.Layer, n.Order)));
    }

    [Fact(DisplayName = "层内顺序:上游组(Service/Node/工作负载)在前,下游组(ConfigMap/Secret/PVC/PV/Ingress/ReplicaSet)在后,组内按名称排序")]
    public void Apply_UpstreamBeforeDownstream()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            Node("ConfigMap", "app-config"),
            Node("Node", "node-b"),
            PodCenter(),
            Node("PersistentVolumeClaim", "aaa-data"),
            Node("Service", "web-svc"),
            Node("Node", "node-a"),
            Node("Ingress", "web-ing"),
            Node("ReplicaSet", "web-rs"),
            Node("Deployment", "web-dep")
        };

        TopologyLayout.Apply(nodes);

        var order = nodes.Where(n => !n.IsCenter).OrderBy(n => n.Order).Select(n => n.Id).ToList();
        // 上游组在前、下游组在后,组内按名称字典序
        Assert.Equal(
        [
            "Node/node-a",
            "Node/node-b",
            "Deployment/web-dep",
            "Service/web-svc",
            "PersistentVolumeClaim/aaa-data",
            "ConfigMap/app-config",
            "Ingress/web-ing",
            "ReplicaSet/web-rs"
        ], order);
    }

    [Fact(DisplayName = "空邻居列表与仅中心节点均不抛异常")]
    public void Apply_EmptyAndCenterOnly()
    {
        var centerOnly = new List<TopologyNodeViewModel> { PodCenter() };
        TopologyLayout.Apply(centerOnly);
        Assert.Equal(0, centerOnly[0].Layer);

        var empty = new List<TopologyNodeViewModel>();
        TopologyLayout.Apply(empty);
        Assert.Empty(empty);
    }
}
