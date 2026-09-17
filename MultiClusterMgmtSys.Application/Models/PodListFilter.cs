using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Application.Models;

/// <summary>状态分类计数选项:分类名(中文)与该分类在当前过滤集合下的 Pod 数量。</summary>
/// <param name="Status">状态分类名(异常/运行中/等待中/已完成/失败/未知)。</param>
/// <param name="Count">该分类的 Pod 数量。</param>
public record PodStatusOption(string Status, int Count);

/// <summary>
/// Pod 列表的前端内存过滤器(纯前端筛选,Pod 数据已全量在内存,不经过服务层)。
/// </summary>
public static class PodListFilter
{
    /// <summary>按已应用的过滤条件筛选 Pod 列表。</summary>
    /// <param name="source">全量 Pod 列表。</param>
    /// <param name="appliedNamespace">已应用的命名空间过滤(精确匹配,忽略大小写)。</param>
    /// <param name="appliedStatusGroup">已应用的状态分类过滤(按状态分类精确匹配;null/空 = 不过滤)。</param>
    /// <param name="keyword">已应用的关键词过滤(包含匹配 Pod 名称、所在节点与 Pod IP,忽略大小写)。</param>
    public static IEnumerable<PodListViewModel> Apply(
        this IEnumerable<PodListViewModel> source,
        string? appliedNamespace,
        string? appliedStatusGroup,
        string? keyword)
    {
        var result = source;
        if (!string.IsNullOrWhiteSpace(appliedNamespace))
        {
            var ns = appliedNamespace;
            result = result.Where(p => string.Equals(p.Namespace, ns, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(appliedStatusGroup))
        {
            var group = appliedStatusGroup;
            result = result.Where(p => p.StatusGroup == group);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var text = keyword.Trim();
            result = result.Where(p =>
                p.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                p.NodeName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                p.PodIp.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        return result;
    }

    /// <summary>计算状态分类选项:基于命名空间/关键词筛选后的集合按状态分类分组计数(全部分类都返回,便于展示零计数)。</summary>
    /// <param name="source">全量 Pod 列表。</param>
    /// <param name="appliedNamespace">已应用的命名空间过滤。</param>
    /// <param name="keyword">已应用的关键词过滤。</param>
    /// <returns>六个固定分类的计数,按约定顺序:异常、运行中、等待中、已完成、失败、未知。</returns>
    public static List<PodStatusOption> StatusOptions(
        this IEnumerable<PodListViewModel> source,
        string? appliedNamespace,
        string? keyword)
    {
        var filtered = source.Apply(appliedNamespace, null, keyword).ToList();
        string[] groups = ["异常", "运行中", "等待中", "已完成", "失败", "未知"];
        return groups
            .Select(g => new PodStatusOption(g, filtered.Count(p => p.StatusGroup == g)))
            .ToList();
    }
}
