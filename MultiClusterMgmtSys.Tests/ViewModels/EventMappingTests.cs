using k8s.Models;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.ViewModels;

public class EventMappingTests
{
    private static Corev1Event NewEvent(
        string type = "Normal",
        string reason = "Scheduled",
        string message = "Successfully assigned default/api-1 to node-1",
        string kind = "Pod",
        string name = "api-1",
        string ns = "default",
        string eventNs = "default",
        int? count = null,
        DateTime? firstTimestamp = null,
        DateTime? lastTimestamp = null,
        Corev1EventSeries? series = null,
        DateTime? eventTime = null,
        DateTime? creationTimestamp = null,
        V1EventSource? source = null)
        => new()
        {
            Type = type,
            Reason = reason,
            Message = message,
            Count = count,
            FirstTimestamp = firstTimestamp,
            LastTimestamp = lastTimestamp,
            EventTime = eventTime,
            Metadata = new V1ObjectMeta { NamespaceProperty = eventNs, CreationTimestamp = creationTimestamp },
            InvolvedObject = new V1ObjectReference { Kind = kind, Name = name, NamespaceProperty = ns },
            Series = series,
            Source = source
        };

    [Fact]
    public void Warning_event_maps_bilingual_type_and_badge_class()
    {
        var vm = NewEvent(type: "Warning").ToEventListViewModel(3);

        Assert.Equal("Warning", vm.Type);
        Assert.Equal("警告", vm.TypeText);
        Assert.Equal("warning", vm.TypeCssClass);
    }

    [Fact]
    public void Unknown_type_falls_back_to_raw_text_and_unknown_badge()
    {
        var vm = NewEvent(type: "Custom").ToEventListViewModel(3);

        Assert.Equal("Custom", vm.TypeText);
        Assert.Equal("unknown", vm.TypeCssClass);
    }

    [Fact]
    public void Normal_event_uses_normal_badge_class()
    {
        var vm = NewEvent(type: "Normal").ToEventListViewModel(3);

        Assert.Equal("正常", vm.TypeText);
        Assert.Equal("normal", vm.TypeCssClass);
    }

    [Fact]
    public void Occurred_at_falls_back_to_series_last_observed_when_last_timestamp_missing()
    {
        var seriesTime = new DateTime(2026, 9, 14, 10, 31, 2, DateTimeKind.Utc);
        var vm = NewEvent(series: new Corev1EventSeries { Count = 12, LastObservedTime = seriesTime }).ToEventListViewModel(3);

        Assert.Equal(seriesTime, vm.OccurredAt);
    }

    [Fact]
    public void Occurred_at_falls_back_to_event_time()
    {
        var eventTime = new DateTime(2026, 9, 14, 10, 31, 2, DateTimeKind.Utc);
        var vm = NewEvent(eventTime: eventTime).ToEventListViewModel(3);

        Assert.Equal(eventTime, vm.OccurredAt);
    }

    [Fact]
    public void Occurred_at_falls_back_to_creation_timestamp()
    {
        var created = new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc);
        var vm = NewEvent(creationTimestamp: created).ToEventListViewModel(3);

        Assert.Equal(created, vm.OccurredAt);
    }

    [Fact]
    public void Occurred_at_prefers_last_timestamp_over_other_fields()
    {
        var last = new DateTime(2026, 9, 14, 11, 0, 0, DateTimeKind.Utc);
        var vm = NewEvent(
            lastTimestamp: last,
            eventTime: new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc),
            creationTimestamp: new DateTime(2026, 9, 14, 9, 0, 0, DateTimeKind.Utc)).ToEventListViewModel(3);

        Assert.Equal(last, vm.OccurredAt);
    }

    [Fact]
    public void Occurred_at_all_null_shows_placeholder()
    {
        var vm = NewEvent().ToEventListViewModel(3);

        Assert.Null(vm.OccurredAt);
        Assert.Equal("—", vm.OccurredAtText);
        Assert.Equal("—", vm.OccurredAtAbsoluteText);
        Assert.Equal("—", vm.FirstOccurredAtText);
    }

    [Fact]
    public void Occurred_at_absolute_text_uses_second_precision()
    {
        var vm = NewEvent(lastTimestamp: new DateTime(2026, 9, 14, 14, 31, 2)).ToEventListViewModel(3);

        Assert.Equal("2026-09-14 14:31:02", vm.OccurredAtAbsoluteText);
    }

    [Fact]
    public void Count_missing_defaults_to_single()
    {
        var vm = NewEvent().ToEventListViewModel(3);

        Assert.Equal(1, vm.Count);
        Assert.Equal("1", vm.CountText);
    }

    [Fact]
    public void Count_over_one_shows_multiplication()
    {
        var vm = NewEvent(count: 12).ToEventListViewModel(3);

        Assert.Equal("×12", vm.CountText);
    }

    [Fact]
    public void Deployment_event_maps_detail_route_with_namespace()
    {
        var vm = NewEvent(kind: "Deployment", name: "web", ns: "app").ToEventListViewModel(7);

        Assert.Equal("/workloads/deployments/7/app/web", vm.DetailRoute);
    }

    [Fact]
    public void Node_event_route_has_no_namespace()
    {
        var vm = NewEvent(kind: "Node", name: "node-1", ns: "", eventNs: "default").ToEventListViewModel(7);

        Assert.Equal("/nodes/7/node-1", vm.DetailRoute);
        Assert.Equal("default", vm.Namespace);
    }

    [Theory]
    [InlineData("Pod")]
    [InlineData("Job")]
    [InlineData("Ingress")]
    public void Kind_without_detail_page_has_no_route(string kind)
    {
        var vm = NewEvent(kind: kind).ToEventListViewModel(7);

        Assert.Null(vm.DetailRoute);
    }

    [Fact]
    public void Source_and_field_path_are_mapped()
    {
        var ev = NewEvent(source: new V1EventSource { Component = "kubelet", Host = "node-1" });
        ev.InvolvedObject.FieldPath = "spec.containers{app}";

        var vm = ev.ToEventListViewModel(3);

        Assert.Equal("kubelet", vm.SourceComponent);
        Assert.Equal("node-1", vm.SourceHost);
        Assert.Equal("spec.containers{app}", vm.InvolvedFieldPath);
    }
}
