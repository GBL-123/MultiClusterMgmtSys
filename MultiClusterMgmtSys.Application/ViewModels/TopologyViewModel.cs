using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 拓扑图节点:以资源为顶点的展示单元;渲染侧、分组与顺序由 <see cref="TopologyLayout"/> 计算。
/// </summary>
public class TopologyNodeViewModel
{
    /// <summary>去重标识,格式 Kind/name。</summary>
    public string Id { get; set; } = "";

    /// <summary>原始 Kind(供排序/过滤与前端映射)。</summary>
    public string Kind { get; set; } = "";

    /// <summary>中文类型名(契约见 display-conventions,未登记 Kind 回退原文)。</summary>
    public string KindText => K8sDisplayText.ResourceKindText(Kind);

    /// <summary>资源名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>命名空间(集群级资源为空)。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>是否为中心节点。</summary>
    public bool IsCenter { get; set; }

    /// <summary>是否为缺失节点(被引用但集群中已不存在,前端以虚线渲染)。</summary>
    public bool IsMissing { get; set; }

    /// <summary>状态提示(原始英文,前端经显示映射转中文,未登记回退原文)。</summary>
    public string? StatusHint { get; set; }

    /// <summary>状态提示的中文展示文本(未登记值回退原文,无状态提示时为 null)。</summary>
    public string? StatusHintText => K8sDisplayText.ResourceStatusText(Kind, StatusHint);

    /// <summary>渲染侧:true = 渲染于中心上方(属主链/入口/供给等「指向中心」的对象),false = 渲染于中心下方。</summary>
    public bool IsAbove { get; set; }

    /// <summary>下行侧分组键(同 Kind 邻居 ≥2 个时按 Kind 聚合成组卡片);null = 直接挂在中心下。</summary>
    public string? Group { get; set; }

    /// <summary>确定性渲染顺序:中心为 0,邻居按(渲染侧、分组、Kind、名称、命名空间、Id)排序后从 1 连续递增(前端按此序喂给布局引擎,同输入必得同布局)。</summary>
    public int Order { get; set; }

    /// <summary>与中心的关系名;多关系按(调度/拥有/选择/路由/挂载/引用/供给)优先级以 " / " 连接;中心节点为空串。</summary>
    public string Relation { get; set; } = "";
}

/// <summary>
/// 拓扑图边:有向关系(From → To),关系名固定为中文。
/// </summary>
public class TopologyEdgeViewModel
{
    /// <summary>起点节点 Id(Kind/name)。</summary>
    public string From { get; set; } = "";

    /// <summary>终点节点 Id(Kind/name)。</summary>
    public string To { get; set; } = "";

    /// <summary>中文关系名(调度/拥有/选择/挂载/引用/路由/供给)。</summary>
    public string Relation { get; set; } = "";
}

/// <summary>
/// 拓扑查询结果:中心节点 + 邻居节点(去重)与关系边集合。
/// </summary>
public class TopologyViewModel
{
    /// <summary>节点列表(中心 + 邻居,按 Id 去重)。</summary>
    public List<TopologyNodeViewModel> Nodes { get; set; } = [];

    /// <summary>边列表(有向,From → To)。</summary>
    public List<TopologyEdgeViewModel> Edges { get; set; } = [];
}
