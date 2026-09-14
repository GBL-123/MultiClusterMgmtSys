using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.ViewModels;

namespace MultiClusterMgmtSys.Tests.Models;

public class EventListFilterTests
{
    private static EventListViewModel Item(
        string name = "api-1",
        string ns = "app",
        string type = "Normal",
        string kind = "Pod",
        string reason = "Scheduled",
        string message = "Successfully assigned")
        => new()
        {
            Type = type,
            Reason = reason,
            Message = message,
            Namespace = ns,
            InvolvedKind = kind,
            InvolvedName = name
        };

    [Fact]
    public void Empty_conditions_return_all_items()
    {
        var items = new List<EventListViewModel> { Item(), Item(name: "api-2", ns: "data") };

        var result = items.Apply(null, null, null, null);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Namespace_filter_matches_event_record_namespace()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "api-1", ns: "app"),
            Item(name: "api-2", ns: "data"),
            Item(name: "node-1", ns: "default")
        };

        var result = items.Apply("DATA", null, null, null);

        Assert.Equal(["api-2"], result.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Type_filter_uses_raw_value_and_is_exact()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "a", type: "Warning"),
            Item(name: "b", type: "Normal"),
            Item(name: "c", type: "Custom")
        };

        var result = items.Apply(null, "Warning", null, null);

        Assert.Equal(["a"], result.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Kind_filter_matches_involved_kind_exactly()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "a", kind: "Pod"),
            Item(name: "b", kind: "Pod"),
            Item(name: "c", kind: "Node")
        };

        var result = items.Apply(null, null, "Pod", null);

        Assert.Equal(["a", "b"], result.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Keyword_matches_involved_name_reason_and_message_case_insensitively()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "web-1", reason: "Scheduled"),
            Item(name: "api-2", reason: "BackOff"),
            Item(name: "cache-3", message: "Liveness probe backoff detected")
        };

        var byName = items.Apply(null, null, null, "WEB");
        Assert.Equal(["web-1"], byName.Select(e => e.InvolvedName));

        var byReason = items.Apply(null, null, null, "backoff");
        Assert.Equal(["api-2", "cache-3"], byReason.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Conditions_are_combinable()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "web-1", ns: "app", type: "Warning", reason: "BackOff"),
            Item(name: "web-2", ns: "data", type: "Warning", reason: "BackOff"),
            Item(name: "web-3", ns: "app", type: "Normal", reason: "BackOff")
        };

        var result = items.Apply("app", "Warning", null, "web");

        Assert.Equal(["web-1"], result.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Kind_combines_with_other_conditions()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "pod-warn", kind: "Pod", type: "Warning"),
            Item(name: "pod-ok", kind: "Pod", type: "Normal"),
            Item(name: "node-warn", kind: "Node", type: "Warning")
        };

        var result = items.Apply(null, "Warning", "Pod", null);

        Assert.Equal(["pod-warn"], result.Select(e => e.InvolvedName));
    }

    [Fact]
    public void Whitespace_conditions_are_ignored()
    {
        var items = new List<EventListViewModel> { Item(), Item(name: "api-2") };

        var result = items.Apply("  ", "", "", "   ");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void Kind_options_exclude_empty_kind_and_zero_counts()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "a", kind: "Pod"),
            Item(name: "b", kind: "Pod"),
            Item(name: "c", kind: "Node"),
            Item(name: "mystery", kind: "")
        };

        var options = items.KindOptions(null, null, null);

        Assert.Equal(2, options.Count);
        Assert.Equal("Pod", options[0].Kind);
        Assert.Equal(2, options[0].Count);
        Assert.Equal("Node", options[1].Kind);
        Assert.Equal(1, options[1].Count);
    }

    [Fact]
    public void Kind_options_order_by_count_then_name()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "p1", kind: "Pod"),
            Item(name: "p2", kind: "Pod"),
            Item(name: "p3", kind: "Pod"),
            Item(name: "n1", kind: "Node"),
            Item(name: "n2", kind: "Node"),
            Item(name: "d1", kind: "Deployment")
        };

        var options = items.KindOptions(null, null, null);

        Assert.Equal(["Pod", "Node", "Deployment"], options.Select(o => o.Kind));
        Assert.Equal([3, 2, 1], options.Select(o => o.Count));
    }

    [Fact]
    public void Kind_options_reflect_other_filters_but_not_kind()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "pw", kind: "Pod", type: "Warning"),
            Item(name: "nw", kind: "Node", type: "Warning"),
            Item(name: "po", kind: "Pod", type: "Normal")
        };

        var options = items.KindOptions(null, "Warning", null);

        Assert.Equal(2, options.Count);
        Assert.Equal("Node", options[0].Kind);
        Assert.Equal(1, options[0].Count);
        Assert.Equal("Pod", options[1].Kind);
    }

    [Fact]
    public void Kind_options_respect_keyword_filter()
    {
        var items = new List<EventListViewModel>
        {
            Item(name: "api-1", kind: "Pod", reason: "BackOff"),
            Item(name: "api-2", kind: "Pod", reason: "Scheduled"),
            Item(name: "cache-0", kind: "Pod", reason: "Scheduled")
        };

        var options = items.KindOptions(null, null, "BackOff");

        var pod = Assert.Single(options);
        Assert.Equal("Pod", pod.Kind);
        Assert.Equal(1, pod.Count);
    }
}
