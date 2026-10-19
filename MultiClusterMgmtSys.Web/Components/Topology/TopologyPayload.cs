using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Web.Components.Topology;

/// <summary>
/// 拓扑 JS 载荷构建(详情页卡片与放大查看对话框共用,保证两处契约一致):
/// 把拓扑 ViewModel 投影为 topology-graph.js 的载荷对象(字段名与 JS 侧卡片渲染口径一致)。
/// </summary>
internal static class TopologyPayload
{
    /// <summary>构建载荷:nodes(含渲染语义字段)+ edges + dotnetRef(节点点击回调目标)。</summary>
    /// <param name="topo">已加载的拓扑视图模型。</param>
    /// <param name="dotnetRef">节点点击回调的 .NET 引用。</param>
    /// <returns>匿名载荷对象,直接传给 JS 侧 mount/update。</returns>
    internal static object Build(TopologyViewModel topo, object dotnetRef) => new
    {
        nodes = topo.Nodes.Select(n => new
        {
            id = n.Id,
            label = n.Name,
            kind = n.Kind,
            kindText = n.KindText,
            @namespace = n.Namespace,
            statusHint = n.StatusHint,
            statusHintText = n.StatusHintText,
            isCenter = n.IsCenter,
            isMissing = n.IsMissing,
            isAbove = n.IsAbove,
            group = n.Group,
            order = n.Order,
            relation = n.Relation,
        }),
        edges = topo.Edges.Select(e => new
        {
            from = e.From,
            to = e.To,
            relation = e.Relation,
        }),
        dotnetRef = dotnetRef,
    };
}
