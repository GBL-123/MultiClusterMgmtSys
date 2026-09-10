using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using k8s;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class SvcServiceTests : IDisposable
{
    private readonly ServiceHarness harness = new("admin", "Admin");
    private readonly Mock<IKubernetes> k8s = K8sMocks.Create();
    private readonly SvcService service;

    public SvcServiceTests()
    {
        service = new SvcService(
            harness.ClusterRepo, harness.Audit, NullLogger<SvcService>.Instance, K8sMocks.Factory(k8s));
    }

    public void Dispose() => harness.Dispose();

    private async Task<int> SeedAsync()
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster("svc-cluster"))).Id;

    private static V1Service NewService(string name, string ns, string type = "ClusterIP", string? clusterIp = "10.96.0.10")
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec
            {
                Type = type,
                ClusterIP = clusterIp,
                Ports = [new V1ServicePort { Port = 80, Protocol = "TCP" }]
            }
        };

    private const string ServiceYaml = """
        apiVersion: v1
        kind: Service
        metadata:
          name: web-svc
          namespace: app
        spec:
          type: ClusterIP
          ports:
            - port: 80
              targetPort: 8080
        """;

    [Fact]
    public async Task GetNamespacesAsync_missing_cluster_throws_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetNamespacesAsync(999));
    }

    [Fact]
    public async Task ListServicesAsync_all_namespaces()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListServices(new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "svc-a", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { Type = "ClusterIP", ClusterIP = "10.96.0.10" }
        });

        var items = await service.ListServicesAsync(new SvcQueryRequest(clusterId, null));

        var vm = Assert.Single(items);
        Assert.Equal("svc-a", vm.Name);
        Assert.Equal("10.96.0.10", vm.ClusterIP);
    }

    [Fact]
    public async Task ListServicesAsync_namespaced_and_headless_flag()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListNamespacedServices("app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "headless-svc", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { Type = "ClusterIP", ClusterIP = "None" }
        });

        var items = await service.ListServicesAsync(new SvcQueryRequest(clusterId, "app"));

        var vm = Assert.Single(items);
        Assert.Equal("headless-svc", vm.Name);
        Assert.True(vm.Headless);
    }

    [Fact]
    public async Task ListServicesAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListServicesThrows(new TaskCanceledException("timeout"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => service.ListServicesAsync(new SvcQueryRequest(clusterId, null)));
    }

    [Fact]
    public async Task GetServiceAsync_missing_cluster_returns_null()
    {
        Assert.Null(await service.GetServiceAsync(new SvcKeyRequest(999, "svc", "app")));
    }

    [Fact]
    public async Task GetServiceAsync_maps_detail_with_ports()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadService("web", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", Uid = "u-1" },
            Spec = new V1ServiceSpec
            {
                Type = "NodePort",
                ClusterIP = "10.96.0.20",
                Ports = [new V1ServicePort { Port = 80, TargetPort = 8080, NodePort = 30080 }]
            }
        });

        var detail = await service.GetServiceAsync(new SvcKeyRequest(clusterId, "web", "app"));

        Assert.NotNull(detail);
        Assert.Equal("NodePort", detail!.Type);
        Assert.Equal(30080, detail.Ports.Single().NodePort);
        Assert.Contains("web", detail.Yaml);
    }

    [Fact]
    public async Task GetServiceEndpointsAsync_slices_mapped_with_ready()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListEndpointSlices("app", SvcMappingExtensions.EndpointSliceServiceLabel + "=web", new V1EndpointSlice
        {
            Endpoints =
            [
                new V1Endpoint { Addresses = ["10.1.1.1"], Conditions = new V1EndpointConditions { Ready = true } },
                new V1Endpoint { Addresses = ["10.1.1.2"], Conditions = new V1EndpointConditions { Ready = false } }
            ],
            Ports = [new Discoveryv1EndpointPort { Port = 80 }]
        });

        var endpoints = await service.GetServiceEndpointsAsync(new SvcKeyRequest(clusterId, "web", "app"));

        Assert.Equal(2, endpoints.Count);
        Assert.Equal("10.1.1.1:80", endpoints[0].Address);
        Assert.True(endpoints[0].Ready);
        Assert.False(endpoints[1].Ready);
    }

    [Fact]
    public async Task GetServiceEndpointsAsync_falls_back_to_legacy_endpoints()
    {
        var clusterId = await SeedAsync();
        k8s.SetupListEndpointSlicesThrows("app", K8sMocks.K8sError(404));
        k8s.SetupListEndpoints("app", new V1Endpoints
        {
            Metadata = new V1ObjectMeta { Name = "web" },
            Subsets =
            [
                new V1EndpointSubset
                {
                    Addresses = [new V1EndpointAddress { Ip = "10.0.0.5" }],
                    NotReadyAddresses = [new V1EndpointAddress { Ip = "10.0.0.6" }]
                }
            ]
        });

        var endpoints = await service.GetServiceEndpointsAsync(new SvcKeyRequest(clusterId, "web", "app"));

        Assert.Equal(2, endpoints.Count);
        Assert.True(endpoints[0].Ready);
        Assert.False(endpoints[1].Ready);
    }

    [Fact]
    public async Task DeleteServiceAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteService("svc-a", "app");

        await service.DeleteServiceAsync(new SvcKeyRequest(clusterId, "svc-a", "app"));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditCategory.Service, audit.Category);
        Assert.Equal(AuditAction.Delete, audit.Action);
    }

    [Fact]
    public async Task DeleteServiceAsync_k8s_error_translated()
    {
        var clusterId = await SeedAsync();
        k8s.SetupDeleteServiceThrows("svc-a", "app", K8sMocks.K8sError(409));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.DeleteServiceAsync(new SvcKeyRequest(clusterId, "svc-a", "app")));
    }

    [Fact]
    public async Task CreateServiceFromYamlAsync_success_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupCreateService("app");

        await service.CreateServiceFromYamlAsync(new SvcCreateRequest(clusterId, ServiceYaml));

        var audit = harness.Db.AuditLogs.Single();
        Assert.Equal(AuditAction.Create, audit.Action);
        Assert.Contains("web-svc", audit.Target);
    }

    [Fact]
    public async Task CreateServiceFromYamlAsync_missing_namespace_throws_validation()
    {
        var clusterId = await SeedAsync();
        var yaml = ServiceYaml.Replace("  namespace: app\n", "");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateServiceFromYamlAsync(new SvcCreateRequest(clusterId, yaml)));

        Assert.Contains("metadata.namespace", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateServiceFromYamlAsync_changed_cluster_ip_throws_validation()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadService("web", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { ClusterIP = "10.96.0.10" }
        });

        var yaml = """
            apiVersion: v1
            kind: Service
            metadata:
              name: web
              namespace: app
            spec:
              clusterIP: 10.96.0.99
            """;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateServiceFromYamlAsync(new SvcUpdateRequest(clusterId, "web", "app", yaml)));

        Assert.Contains("clusterIP", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateServiceFromYamlAsync_replaces_and_audits()
    {
        var clusterId = await SeedAsync();
        k8s.SetupReadService("web", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", ResourceVersion = "42", Uid = "uid-1" },
            Spec = new V1ServiceSpec { ClusterIP = "10.96.0.10" }
        });
        k8s.SetupReplaceService("web", "app");

        var yaml = """
            apiVersion: v1
            kind: Service
            metadata:
              name: web
              namespace: app
            spec:
              selector:
                app: web
            """;
        await service.UpdateServiceFromYamlAsync(new SvcUpdateRequest(clusterId, "web", "app", yaml));

        k8s.Verify(x => x.CoreV1.ReplaceNamespacedServiceWithHttpMessagesAsync(
            It.Is<V1Service>(s => s.Spec!.ClusterIP == "10.96.0.10" && s.Metadata!.ResourceVersion == "42"),
            "web", "app",
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(AuditAction.Update, harness.Db.AuditLogs.Single().Action);
    }
}



