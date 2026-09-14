using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class EventServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly EventService service;

    public EventServiceTests()
    {
        service = new EventService(harness.ClusterRepo, NullLogger<EventService>.Instance, K8sMocks.Cache(k8s));
    }

    public void Dispose() => harness.Dispose();

    private async Task<int> SeedAsync()
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-cluster"))).Id;

    private static Corev1Event NewEvent(
        string name = "api-1",
        string ns = "app",
        string type = "Normal",
        string reason = "Scheduled",
        int? count = null,
        DateTime? lastTimestamp = null,
        DateTime? eventTime = null)
        => new()
        {
            Type = type,
            Reason = reason,
            Message = "Successfully assigned",
            Count = count,
            LastTimestamp = lastTimestamp,
            EventTime = eventTime,
            Metadata = new V1ObjectMeta { NamespaceProperty = ns },
            InvolvedObject = new V1ObjectReference { Kind = "Pod", Name = name, NamespaceProperty = ns }
        };

    [Fact]
    public async Task ListEventsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListEventsAsync(new EventQueryRequest(999)));

        k8s.Verify(x => x.CoreV1.ListEventForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListEventsAsync_all_namespaces_maps_aggregation_and_writes_no_audit()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListEvents(
            NewEvent("api-1", "app", count: 12),
            NewEvent("cache-0", "data", type: "Warning", reason: "BackOff"));

        var items = await service.ListEventsAsync(new EventQueryRequest(clusterId));

        Assert.Equal(2, items.Count);
        var first = items.Single(e => e.Namespace == "app");
        Assert.Equal("×12", first.CountText);
        Assert.Equal("正常", first.TypeText);
        var second = items.Single(e => e.Namespace == "data");
        Assert.Equal("警告", second.TypeText);
        Assert.Empty(harness.Db.AuditLogs);
    }

    [Fact]
    public async Task ListEventsAsync_resolves_time_when_last_timestamp_missing()
    {
        var clusterId = await SeedAsync();
        var eventTime = new DateTime(2026, 9, 14, 10, 31, 2, DateTimeKind.Utc);
        k8s.SetupListEvents(NewEvent(lastTimestamp: null, eventTime: eventTime));

        var items = await service.ListEventsAsync(new EventQueryRequest(clusterId));

        Assert.Equal(eventTime, Assert.Single(items).OccurredAt);
    }

    [Fact]
    public async Task ListEventsAsync_k8s_permission_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListEventsThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => service.ListEventsAsync(new EventQueryRequest(clusterId)));
    }

    [Fact]
    public async Task ListEventsAsync_timeout_translated_to_unreachable()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListEventsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => service.ListEventsAsync(new EventQueryRequest(clusterId)));
    }

    [Fact]
    public async Task GetNamespacesAsync_returns_sorted_namespaces()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespaces("zeta", "alpha");

        var namespaces = await service.GetNamespacesAsync(clusterId);

        Assert.Equal(new[] { "alpha", "zeta" }, namespaces);
    }

    [Fact]
    public async Task GetNamespacesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetNamespacesAsync(999));
    }

    [Fact]
    public async Task GetNamespacesAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespacesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(() => service.GetNamespacesAsync(clusterId));
    }
}
