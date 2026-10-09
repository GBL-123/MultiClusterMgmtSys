using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 拓扑图节点:以资源为顶点的展示单元;布局层号与层内序号由 <see cref="TopologyLayout"/> 计算。
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

    /// <summary>布局层号:中心 0、直接邻居 1(v1 仅一跳)。</summary>
    public int Layer { get; set; }

    /// <summary>层内序号(前端乘以像素间距得 x 坐标)。</summary>
    public int Order { get; set; }
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
