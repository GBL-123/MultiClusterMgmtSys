using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.Models;

/// <summary>
/// 事件列表的前端内存过滤器(纯前端筛选,事件数据已全量在内存,不经过服务层)。
/// </summary>
public static class EventListFilter
{
    /// <summary>按已应用的过滤条件筛选事件列表。</summary>
    /// <param name="source">全量事件列表。</param>
    /// <param name="appliedNamespace">已应用的命名空间过滤(按事件记录所在命名空间精确匹配,忽略大小写)。</param>
    /// <param name="appliedType">已应用的级别过滤(保持 K8s 原始值 Normal/Warning,精确匹配)。</param>
    /// <param name="appliedKind">已应用的对象类型过滤(按关联对象 kind 精确匹配;null/空 = 不过滤)。</param>
    /// <param name="keyword">已应用的关键词过滤(包含匹配关联对象名称、原因与消息,忽略大小写)。</param>
    public static IEnumerable<EventListViewModel> Apply(
        this IEnumerable<EventListViewModel> source,
        string? appliedNamespace,
        string? appliedType,
        string? appliedKind,
        string? keyword)
    {
        var result = source;
        if (!string.IsNullOrWhiteSpace(appliedNamespace))
        {
            var ns = appliedNamespace;
            result = result.Where(e => string.Equals(e.Namespace, ns, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(appliedType))
        {
            var type = appliedType;
            result = result.Where(e => e.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(appliedKind))
        {
            var kind = appliedKind;
            result = result.Where(e => e.InvolvedKind == kind);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var text = keyword.Trim();
            result = result.Where(e =>
                e.InvolvedName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                e.Reason.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return result;
    }

    /// <summary>计算对象类型分类选项:基于命名空间/级别/关键词筛选后的集合按关联对象 kind 分组计数。</summary>
    /// <param name="source">全量事件列表。</param>
    /// <param name="appliedNamespace">已应用的命名空间过滤。</param>
    /// <param name="appliedType">已应用的级别过滤。</param>
    /// <param name="keyword">已应用的关键词过滤。</param>
    /// <returns>仅含计数大于 0 的分类,按计数降序、同数按 kind 名升序(Ordinal);空 kind 不参与分类。</returns>
    public static List<EventKindOption> KindOptions(
        this IEnumerable<EventListViewModel> source,
        string? appliedNamespace,
        string? appliedType,
        string? keyword)
        => source.Apply(appliedNamespace, appliedType, null, keyword)
            .Where(e => !string.IsNullOrEmpty(e.InvolvedKind))
            .GroupBy(e => e.InvolvedKind)
            .Select(g => new EventKindOption(g.Key, g.Count()))
            .OrderByDescending(o => o.Count)
            .ThenBy(o => o.Kind, StringComparer.Ordinal)
            .ToList();
}
