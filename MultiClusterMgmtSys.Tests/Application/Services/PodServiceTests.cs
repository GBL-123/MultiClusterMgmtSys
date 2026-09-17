using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

public class PodServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly PodService service;

    public PodServiceTests()
    {
        service = new PodService(harness.ClusterRepo, NullLogger<PodService>.Instance, K8sMocks.Cache(k8s));
    }

    public void Dispose() => harness.Dispose();

    private async Task<int> SeedAsync(string name = "pod-cluster", ClusterStatus status = ClusterStatus.Online)
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster(name, status: status))).Id;

    [Fact]
    public async Task ListPodsAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.ListPodsAsync(new PodQueryRequest(999, null)));

        k8s.Verify(x => x.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListPodsAsync_all_namespaces_projects_slim_view_and_writes_no_audit()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListPods(
            K8sMocks.NewPod("web-1", "app", nodeName: "node-a", podIp: "10.1.1.1"),
            K8sMocks.NewPod("db-0", "data", phase: "Pending", podIp: ""));

        var items = await service.ListPodsAsync(new PodQueryRequest(clusterId, null));

        Assert.Equal(2, items.Count);
        var web = items.Single(p => p.Name == "web-1");
        Assert.Equal("app", web.Namespace);
        Assert.Equal("node-a", web.NodeName);
        Assert.Equal("10.1.1.1", web.PodIp);
        Assert.Equal("Running", web.StatusRaw);
        Assert.Equal("online", web.StatusCssClass);
        Assert.Empty(harness.Db.AuditLogs);
    }

    [Fact]
    public async Task ListPodsAsync_namespaced_call_uses_field_selector_namespace()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespacedPods("data", K8sMocks.NewPod("db-0", "data"));

        var items = await service.ListPodsAsync(new PodQueryRequest(clusterId, "data"));

        Assert.Single(items, p => p.Name == "db-0");
        k8s.Verify(x => x.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListPodsAsync_k8s_permission_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListPodsThrows(K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => service.ListPodsAsync(new PodQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task ListPodsAsync_timeout_translated_to_unreachable()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListPodsThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => service.ListPodsAsync(new PodQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task ListPodsAsync_label_selector_filters_server_side()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespacedPodsBySelector("app", "app=web", K8sMocks.NewPod("web-1", "app"));

        var items = await service.ListPodsAsync(new PodQueryRequest(clusterId, "app", "app=web"));

        Assert.Single(items, p => p.Name == "web-1");
        k8s.Verify(x => x.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
            It.Is<string>(n => n == "app"),
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.Is<string?>(s => s == "app=web"), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        k8s.Verify(x => x.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPodAsync_returns_detail_with_conditions_and_containers()
    {
        var clusterId = await SeedAsync();
        var pod = K8sMocks.NewPod("web-1", "app", customize: p =>
        {
            p.Status.Conditions =
            [
                new V1PodCondition { Type = "Ready", Status = "True", LastTransitionTime = DateTime.UtcNow }
            ];
            p.Status.ContainerStatuses =
            [
                new V1ContainerStatus
                {
                    Name = "app",
                    Ready = true,
                    RestartCount = 2,
                    Image = "nginx:1.25",
                    State = K8sMocks.PodStateRunning(),
                    LastState = new V1ContainerState { Terminated = new V1ContainerStateTerminated { Reason = "OOMKilled", ExitCode = 137, FinishedAt = DateTime.UtcNow.AddHours(-1) } }
                }
            ];
        });
        k8s.SetupReadPod("web-1", "app", pod);

        var vm = await service.GetPodAsync(new PodKeyRequest(clusterId, "app", "web-1"));

        Assert.NotNull(vm);
        Assert.Equal("Running", vm.Phase);
        Assert.Equal("运行中", vm.Containers.Single().StateText);
        Assert.Equal(2, vm.Containers.Single().RestartCount);
        Assert.Equal("内存不足被杀", vm.Containers.Single().LastTerminatedReasonText);
        Assert.Equal("成立", vm.Conditions.Single(c => c.Type == "Ready").StatusText);
        Assert.Empty(harness.Db.AuditLogs);
    }

    [Fact]
    public async Task GetPodAsync_not_found_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodThrows("ghost", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetPodAsync(new PodKeyRequest(clusterId, "app", "ghost")));
    }

    [Fact]
    public async Task GetPodAsync_missing_cluster_returns_null()
    {
        var vm = await service.GetPodAsync(new PodKeyRequest(999, "app", "web-1"));

        Assert.Null(vm);
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
    public async Task GetPodLogAsync_default_read_returns_content_and_line_count()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodLog("web-1", "app", "line-1\nline-2\n");

        var log = await service.GetPodLogAsync(new PodLogRequest(clusterId, "app", "web-1", null, 500, false));

        Assert.Equal("line-1\nline-2\n", log.Content);
        Assert.Equal(2, log.LineCount);
        Assert.Empty(harness.Db.AuditLogs);
    }

    [Fact]
    public async Task GetPodLogAsync_passes_container_tail_lines_and_previous()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodLogExact("web-1", "app", "main", 1000, true, "previous log");

        var log = await service.GetPodLogAsync(new PodLogRequest(clusterId, "app", "web-1", "main", 1000, true));

        Assert.Equal("previous log", log.Content);
        k8s.Verify(x => x.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(
            It.Is<string>(n => n == "web-1"),
            It.Is<string>(n => n == "app"),
            It.Is<string?>(c => c == "main"),
            It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>(),
            It.IsAny<bool?>(),
            It.Is<bool?>(p => p == true),
            It.IsAny<int?>(), It.IsAny<string?>(),
            It.Is<int?>(t => t == 1000),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPodLogAsync_empty_log_has_zero_lines()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodLog("web-1", "app", "");

        var log = await service.GetPodLogAsync(new PodLogRequest(clusterId, "app", "web-1", null, 500, false));

        Assert.Equal("", log.Content);
        Assert.Equal(0, log.LineCount);
    }

    [Fact]
    public async Task GetPodLogAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetPodLogAsync(new PodLogRequest(999, "app", "web-1", null, 500, false)));

        k8s.Verify(x => x.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPodLogAsync_not_found_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodLogThrows("ghost", "app", K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetPodLogAsync(new PodLogRequest(clusterId, "app", "ghost", null, 500, false)));
    }

    [Fact]
    public async Task GetPodLogAsync_container_waiting_400_translated_with_api_message()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadPodLogThrows("web-1", "app",
            K8sMocks.K8sError(400, "container \"app\" in pod \"web-1\" is waiting to start: ContainerCreating"));

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.GetPodLogAsync(new PodLogRequest(clusterId, "app", "web-1", "app", 500, false)));

        Assert.Contains("waiting to start", ex.UserMessage);
    }
}
