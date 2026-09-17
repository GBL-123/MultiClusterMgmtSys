using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Application.ViewModels;

namespace MultiClusterMgmtSys.Tests.Application.Models;

public class PodListFilterTests
{
    private static PodListViewModel Pod(string name, string ns, string phase = "Running", string? reason = null, string node = "node-1", string ip = "10.0.0.1")
        => new()
        {
            Name = name,
            Namespace = ns,
            Phase = phase,
            ContainerReason = reason,
            NodeName = node,
            PodIp = ip
        };

    private static readonly List<PodListViewModel> Pods =
    [
        Pod("web-1", "app", node: "node-a", ip: "10.1.1.1"),
        Pod("web-2", "app", phase: "Pending", node: "node-b"),
        Pod("db-0", "data", reason: "CrashLoopBackOff", node: "node-c"),
        Pod("job-x", "batch", phase: "Succeeded"),
        Pod("bad-1", "batch", phase: "Failed")
    ];

    [Fact]
    public void Apply_namespace_filter_is_case_insensitive_and_exact()
    {
        var result = PodListFilter.Apply(Pods, "APP", null, null).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("app", p.Namespace));
    }

    [Fact]
    public void Apply_status_group_filter_selects_container_signal_first()
    {
        var result = PodListFilter.Apply(Pods, null, "异常", null).ToList();

        Assert.Single(result, p => p.Name == "db-0");
    }

    [Fact]
    public void Apply_keyword_matches_name_node_and_ip()
    {
        var byName = PodListFilter.Apply(Pods, null, null, "web-").ToList();
        Assert.Equal(2, byName.Count);

        var byNode = PodListFilter.Apply(Pods, null, null, "node-c").ToList();
        Assert.Single(byNode, p => p.Name == "db-0");

        var byIp = PodListFilter.Apply(Pods, null, null, "10.1.1.1").ToList();
        Assert.Single(byIp, p => p.Name == "web-1");
    }

    [Fact]
    public void Apply_combined_filters_narrow_results()
    {
        var result = PodListFilter.Apply(Pods, "batch", "失败", null).ToList();

        Assert.Single(result, p => p.Name == "bad-1");
    }

    [Fact]
    public void StatusOptions_count_all_six_groups_after_scope_filters()
    {
        var options = PodListFilter.StatusOptions(Pods, null, null);

        Assert.Equal(new[] { "异常", "运行中", "等待中", "已完成", "失败", "未知" }, options.Select(o => o.Status));
        Assert.Equal(1, options.Single(o => o.Status == "运行中").Count);
        Assert.Equal(1, options.Single(o => o.Status == "异常").Count);
        Assert.Equal(0, options.Single(o => o.Status == "未知").Count);
    }

    [Fact]
    public void StatusOptions_respect_namespace_and_keyword()
    {
        var options = PodListFilter.StatusOptions(Pods, "app", null);

        Assert.Equal(1, options.Single(o => o.Status == "运行中").Count);
        Assert.Equal(0, options.Single(o => o.Status == "异常").Count);
    }
}
