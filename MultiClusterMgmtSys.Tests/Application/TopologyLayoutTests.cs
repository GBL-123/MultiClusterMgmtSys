using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Application;

/// <summary>TopologyLayout 布局纯函数测试:渲染侧(上行/下行)、下行分组、关系合并与确定性(契约:确定性布局)。</summary>
public class TopologyLayoutTests
{
    private static TopologyNodeViewModel Node(string kind, string name, bool isCenter = false) => new()
    {
        Id = $"{kind}/{name}",
        Kind = kind,
        Name = name,
        IsCenter = isCenter
    };

    private static TopologyEdgeViewModel Edge(string from, string to, string relation) =>
        new() { From = from, To = to, Relation = relation };

    private static TopologyNodeViewModel PodCenter() => Node("Pod", "web-0", isCenter: true);

    [Fact(DisplayName = "中心节点:Order=0、不上行、无分组、关系名为空")]
    public void Apply_CenterSemantics()
    {
        var center = PodCenter();
        var nodes = new List<TopologyNodeViewModel> { center, Node("ConfigMap", "app-config") };
        var edges = new List<TopologyEdgeViewModel> { Edge("Pod/web-0", "ConfigMap/app-config", "挂载") };

        TopologyLayout.Apply(nodes, edges);

        Assert.False(center.IsAbove);
        Assert.Null(center.Group);
        Assert.Equal(0, center.Order);
        Assert.Equal("", center.Relation);
    }

    [Fact(DisplayName = "属主链上行:拥有边起点(Deployment/ReplicaSet)渲染于中心上方,调度/挂载目标在下")]
    public void Apply_OwnerChainAbove()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            Node("Deployment", "web-dep"),
            PodCenter(),
            Node("ReplicaSet", "web-rs"),
            Node("Node", "node-1"),
            Node("ConfigMap", "cm-1"),
        };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("Deployment/web-dep", "ReplicaSet/web-rs", "拥有"),
            Edge("ReplicaSet/web-rs", "Pod/web-0", "拥有"),
            Edge("Pod/web-0", "Node/node-1", "调度"),
            Edge("Pod/web-0", "ConfigMap/cm-1", "挂载"),
        };

        TopologyLayout.Apply(nodes, edges);

        Assert.True(nodes.Single(n => n.Id == "Deployment/web-dep").IsAbove);
        Assert.True(nodes.Single(n => n.Id == "ReplicaSet/web-rs").IsAbove);
        Assert.False(nodes.Single(n => n.Id == "Node/node-1").IsAbove);
        Assert.False(nodes.Single(n => n.Id == "ConfigMap/cm-1").IsAbove);
    }

    [Fact(DisplayName = "Service 中心:路由边起点 Ingress 上行,选择边目标 Pod 下行")]
    public void Apply_ServiceCenterRoutesAboveAndSelectedPodsBelow()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            Node("Service", "svc-a", isCenter: true),
            Node("Ingress", "web-ing"),
            Node("Pod", "web-1"),
        };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("Ingress/web-ing", "Service/svc-a", "路由"),
            Edge("Service/svc-a", "Pod/web-1", "选择"),
        };

        TopologyLayout.Apply(nodes, edges);

        Assert.True(nodes.Single(n => n.Id == "Ingress/web-ing").IsAbove);
        Assert.False(nodes.Single(n => n.Id == "Pod/web-1").IsAbove);
    }

    [Fact(DisplayName = "PVC 中心:供给边起点 PV 上行,挂载来源 Pod 下行")]
    public void Apply_ClaimCenterSupplyAbove()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            Node("PersistentVolumeClaim", "data", isCenter: true),
            Node("PersistentVolume", "pv-1"),
            Node("Pod", "web-1"),
        };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("PersistentVolume/pv-1", "PersistentVolumeClaim/data", "供给"),
            Edge("Pod/web-1", "PersistentVolumeClaim/data", "挂载"),
        };

        TopologyLayout.Apply(nodes, edges);

        Assert.True(nodes.Single(n => n.Id == "PersistentVolume/pv-1").IsAbove);
        Assert.False(nodes.Single(n => n.Id == "Pod/web-1").IsAbove);
    }

    [Fact(DisplayName = "下行同 Kind ≥2 成组(组键 = Kind),单个邻居与上行邻居不成组")]
    public void Apply_DownstreamGroupingByKind()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            PodCenter(),
            Node("ConfigMap", "cm-a"),
            Node("ConfigMap", "cm-b"),
            Node("Secret", "sec-a"),
            Node("Service", "svc-a"),
            Node("Deployment", "web-dep"),
        };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("Pod/web-0", "ConfigMap/cm-a", "挂载"),
            Edge("Pod/web-0", "ConfigMap/cm-b", "引用"),
            Edge("Pod/web-0", "Secret/sec-a", "挂载"),
            Edge("Service/svc-a", "Pod/web-0", "选择"),
            Edge("Deployment/web-dep", "Pod/web-0", "拥有"),
        };

        TopologyLayout.Apply(nodes, edges);

        Assert.Equal("ConfigMap", nodes.Single(n => n.Id == "ConfigMap/cm-a").Group);
        Assert.Equal("ConfigMap", nodes.Single(n => n.Id == "ConfigMap/cm-b").Group);
        Assert.Null(nodes.Single(n => n.Id == "Secret/sec-a").Group);
        Assert.Null(nodes.Single(n => n.Id == "Service/svc-a").Group);
        Assert.Null(nodes.Single(n => n.Id == "Deployment/web-dep").Group);
    }

    [Fact(DisplayName = "关系名合并:同一节点多关系按固定优先级以 \" / \" 连接")]
    public void Apply_MergedRelationsFollowPriority()
    {
        var nodes = new List<TopologyNodeViewModel> { PodCenter(), Node("ConfigMap", "cm-a"), Node("Service", "svc-a") };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("Pod/web-0", "ConfigMap/cm-a", "引用"),
            Edge("Pod/web-0", "ConfigMap/cm-a", "挂载"),
            Edge("Service/svc-a", "Pod/web-0", "选择"),
        };

        TopologyLayout.Apply(nodes, edges);

        Assert.Equal("挂载 / 引用", nodes.Single(n => n.Id == "ConfigMap/cm-a").Relation);
        Assert.Equal("选择", nodes.Single(n => n.Id == "Service/svc-a").Relation);
    }

    [Fact(DisplayName = "同输入两次 Apply 结果完全一致(确定性)")]
    public void Apply_IsDeterministic()
    {
        List<TopologyNodeViewModel> Build() =>
        [
            Node("PersistentVolumeClaim", "data-vol"),
            PodCenter(),
            Node("Node", "node-a"),
            Node("ConfigMap", "zz-config"),
            Node("ConfigMap", "aa-config"),
            Node("Service", "web-svc"),
        ];
        List<TopologyEdgeViewModel> BuildEdges() =>
        [
            Edge("Service/web-svc", "Pod/web-0", "选择"),
            Edge("Pod/web-0", "Node/node-a", "调度"),
            Edge("Pod/web-0", "ConfigMap/zz-config", "挂载"),
            Edge("Pod/web-0", "ConfigMap/aa-config", "引用"),
            Edge("Pod/web-0", "PersistentVolumeClaim/data-vol", "挂载"),
        ];

        var first = Build();
        var second = Build();
        TopologyLayout.Apply(first, BuildEdges());
        TopologyLayout.Apply(second, BuildEdges());

        Assert.Equal(
            first.Select(n => (n.Id, n.IsAbove, n.Group, n.Order, n.Relation)),
            second.Select(n => (n.Id, n.IsAbove, n.Group, n.Order, n.Relation)));
    }

    [Fact(DisplayName = "邻居顺序:先上行后下行,下行内单个邻居先于分组,各自按 Kind/名称字典序")]
    public void Apply_DeterministicOrder()
    {
        var nodes = new List<TopologyNodeViewModel>
        {
            PodCenter(),
            Node("ConfigMap", "cm-b"),
            Node("ConfigMap", "cm-a"),
            Node("Service", "web-svc"),
            Node("Deployment", "web-dep"),
            Node("ReplicaSet", "web-rs"),
        };
        var edges = new List<TopologyEdgeViewModel>
        {
            Edge("Deployment/web-dep", "ReplicaSet/web-rs", "拥有"),
            Edge("ReplicaSet/web-rs", "Pod/web-0", "拥有"),
            Edge("Pod/web-0", "ConfigMap/cm-a", "挂载"),
            Edge("Pod/web-0", "ConfigMap/cm-b", "挂载"),
            Edge("Service/web-svc", "Pod/web-0", "选择"),
        };

        TopologyLayout.Apply(nodes, edges);

        var order = nodes.Where(n => !n.IsCenter).OrderBy(n => n.Order).Select(n => n.Id).ToList();
        Assert.Equal(
        [
            "Deployment/web-dep",
            "ReplicaSet/web-rs",
            "Service/web-svc",
            "ConfigMap/cm-a",
            "ConfigMap/cm-b"
        ], order);
        Assert.Equal([1, 2, 3, 4, 5], nodes.Where(n => !n.IsCenter).OrderBy(n => n.Order).Select(n => n.Order));
    }

    [Fact(DisplayName = "空邻居列表与仅中心节点均不抛异常")]
    public void Apply_EmptyAndCenterOnly()
    {
        var centerOnly = new List<TopologyNodeViewModel> { PodCenter() };
        TopologyLayout.Apply(centerOnly, []);
        Assert.Equal(0, centerOnly[0].Order);
        Assert.Equal("", centerOnly[0].Relation);

        var empty = new List<TopologyNodeViewModel>();
        TopologyLayout.Apply(empty, []);
        Assert.Empty(empty);
    }
}
