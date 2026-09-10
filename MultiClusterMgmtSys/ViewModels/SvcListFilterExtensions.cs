namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 服务列表的内存过滤扩展(服务数据已全量在内存,按已应用的过滤条件在客户端筛选)。
/// </summary>
public static class SvcListFilterExtensions
{
    /// <summary>按已应用的过滤条件筛选服务列表。</summary>
    /// <param name="source">全量服务列表。</param>
    /// <param name="appliedName">已应用的名称过滤(包含,忽略大小写)。</param>
    /// <param name="appliedPort">已应用的端口过滤(匹配端口/NodePort/目标端口)。</param>
    /// <param name="appliedType">已应用的类型过滤(ClusterIP/NodePort/LoadBalancer 等)。</param>
    public static IEnumerable<SvcListViewModel> Apply(
        this IEnumerable<SvcListViewModel> source,
        string? appliedName,
        string? appliedPort,
        string? appliedType)
    {
        var result = source;
        if (!string.IsNullOrWhiteSpace(appliedName))
        {
            var name = appliedName;
            result = result.Where(s => s.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(appliedPort))
        {
            var port = appliedPort;
            result = result.Where(s => s.Ports.Any(p =>
                p.Port.ToString().Contains(port, StringComparison.OrdinalIgnoreCase) ||
                (p.NodePort?.ToString().Contains(port, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.TargetPort?.Contains(port, StringComparison.OrdinalIgnoreCase) ?? false)));
        }
        if (!string.IsNullOrWhiteSpace(appliedType))
        {
            var type = appliedType;
            result = result.Where(s => s.Type == type);
        }
        return result;
    }
}
