using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

/// <summary>TopologyService 拓扑聚合测试:覆盖 Pod/Service/配置对象/PVC/工作负载五类中心的边生成、缺失节点与降级口径。</summary>
public class TopologyServiceTests : IDisposable
{
    private readonly ServiceHarness _harness = new();
    private readonly Mock<IKubernetes> _k8s;
    private readonly TopologyService _service;
    private readonly string _ns = "app";

    public TopologyServiceTests()
    {
        _k8s = K8sMocks.Create();
        _service = new TopologyService(
            _harness.ClusterRepo,
            NullLogger<TopologyService>.Instance,
            K8sMocks.Cache(_k8s));
    }

    public void Dispose() => _harness.Dispose();

    private async Task<int> SeedClusterAsync()
        => (await _harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-cluster"))).Id;

    private static TopologyNodeViewModel? Node(TopologyViewModel view, string id)
        => view.Nodes.FirstOrDefault(n => n.Id == id);

    private static bool HasEdge(TopologyViewModel view, string from, string to, string relation)
        => view.Edges.Any(e => e.From == from && e.To == to && e.Relation == relation);

    private void SetupEmptyLists(string ns)
    {
        _k8s.SetupListNamespacedServices(ns);
        _k8s.SetupListNamespacedConfigMaps(ns);
        _k8s.SetupListNamespacedSecrets(ns);
        _k8s.SetupListNamespacedClaims(ns);
        _k8s.SetupListNamespacedPods(ns);
        _k8s.SetupListNamespacedIngresses(ns);
    }

    private static V1Service Service(string name, string ns, Dictionary<string, string>? selector = null)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1ServiceSpec { Selector = selector }
        };

    private static V1Ingress IngressRouting(string name, string ns, string serviceName)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1IngressSpec
            {
                Rules =
                [
                    new V1IngressRule
                    {
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths =
                            [
                                new V1HTTPIngressPath
                                {
                                    Backend = new V1IngressBackend
                                    {
                                        Service = new V1IngressServiceBackend { Name = serviceName }
                                    }
                                }
                            ]
                        }
                    }
                ]
            }
        };

    private static Dictionary<string, string> Labels(string app) => new() { ["app"] = app };

    private static V1Pod Pod(string name, string ns, string? phase = "Running", string? nodeName = null, Action<V1Pod>? customize = null)
        => K8sMocks.NewPod(name, ns, phase: phase, nodeName: nodeName, customize: customize);

    private void StubPodCenterLists(string name, V1Pod pod)
    {
        SetupEmptyLists(_ns);
        _k8s.SetupReadPod(name, _ns, pod);
    }

    // ---------- Pod 中心 ----------

    [Fact]
    public async Task GetTopologyAsync_bare_pod_center_only()
    {
        var clusterId = await SeedClusterAsync();
        StubPodCenterLists("web-1", Pod("web-1", _ns, nodeName: null));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        var node = Assert.Single(view.Nodes);
        Assert.True(node.IsCenter);
        Assert.Equal("Pod/web-1", node.Id);
        Assert.False(node.IsMissing);
        Assert.Equal(0, node.Layer);
        Assert.Empty(view.Edges);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_scheduling_edge_with_ready_hint()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: "node-1");
        StubPodCenterLists("web-1", pod);
        _k8s.SetupReadNode("node-1", new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "node-1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.True(HasEdge(view, "Pod/web-1", "Node/node-1", "调度"));
        var node = Node(view, "Node/node-1");
        Assert.NotNull(node);
        Assert.False(node!.IsMissing);
        Assert.Equal("Ready", node.StatusHint);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_owner_chain_rs_to_deployment()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
            p.Metadata.OwnerReferences =
            [
                new V1OwnerReference { ApiVersion = "apps/v1", Kind = "ReplicaSet", Name = "web-rs", Uid = "u-rs" }
            ]);
        StubPodCenterLists("web-1", pod);
        _k8s.SetupReadReplicaSetBody("web-rs", _ns, new V1ReplicaSet
        {
            Metadata = new V1ObjectMeta
            {
                Name = "web-rs",
                NamespaceProperty = _ns,
                OwnerReferences =
                [
                    new V1OwnerReference { ApiVersion = "apps/v1", Kind = "Deployment", Name = "web-dep", Uid = "u-dep" }
                ]
            }
        });
        _k8s.SetupReadDeployment("web-dep", _ns, new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web-dep", NamespaceProperty = _ns }
        });

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.True(HasEdge(view, "ReplicaSet/web-rs", "Pod/web-1", "拥有"));
        Assert.True(HasEdge(view, "Deployment/web-dep", "ReplicaSet/web-rs", "拥有"));
        Assert.NotNull(Node(view, "Deployment/web-dep"));
        Assert.Equal(2, view.Edges.Count);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_owner_statefulset_one_hop()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("db-0", _ns, nodeName: null, customize: p =>
            p.Metadata.OwnerReferences =
            [
                new V1OwnerReference { ApiVersion = "apps/v1", Kind = "StatefulSet", Name = "db-sts", Uid = "u-sts" }
            ]);
        SetupEmptyLists(_ns);
        _k8s.SetupReadPod("db-0", _ns, pod);
        _k8s.SetupReadStatefulSet("db-sts", _ns, new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Name = "db-sts", NamespaceProperty = _ns }
        });

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "db-0"));

        Assert.True(HasEdge(view, "StatefulSet/db-sts", "Pod/db-0", "拥有"));
        Assert.Equal(1, view.Edges.Count);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_owner_non_workload_kind_stops()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("sp-1", _ns, nodeName: null, customize: p =>
            p.Metadata.OwnerReferences =
            [
                new V1OwnerReference { ApiVersion = "apps/v1", Kind = "UnknownController", Name = "sp-ctrl", Uid = "u-x" }
            ]);
        StubPodCenterLists("sp-1", pod);

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "sp-1"));

        Assert.True(HasEdge(view, "UnknownController/sp-ctrl", "Pod/sp-1", "拥有"));
        Assert.Equal(1, view.Edges.Count);
        Assert.DoesNotContain(view.Nodes, n => n.Kind is "Deployment" or "ReplicaSet");
        Assert.DoesNotContain(_k8s.Invocations, i =>
            i.Method.Name.Contains("Deployment") || i.Method.Name.Contains("ReplicaSet"));
    }

    [Fact]
    public async Task GetTopologyAsync_pod_service_selector_matches()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p => p.Metadata.Labels = Labels("web"));
        StubPodCenterLists("web-1", pod);
        _k8s.SetupListNamespacedServices(_ns, Service("app-svc", _ns, Labels("web")), Service("bare-svc", _ns));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.True(HasEdge(view, "Service/app-svc", "Pod/web-1", "选择"));
        Assert.Equal(1, view.Edges.Count(e => e.Relation == "选择"));
        Assert.DoesNotContain(view.Nodes, n => n.Id == "Service/bare-svc");
    }

    [Fact]
    public async Task GetTopologyAsync_pod_service_selector_miss_and_empty()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p => p.Metadata.Labels = Labels("other"));
        StubPodCenterLists("web-1", pod);
        _k8s.SetupListNamespacedServices(_ns, Service("app-svc", _ns, Labels("web")), Service("bare-svc", _ns));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.Empty(view.Edges.Where(e => e.Relation == "选择"));
    }

    [Fact]
    public async Task GetTopologyAsync_pod_mounts_and_refs_with_dedup()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
        {
            p.Spec.Volumes =
            [
                new V1Volume { Name = "cfg", ConfigMap = new V1ConfigMapVolumeSource { Name = "app-config" } },
                new V1Volume { Name = "sec", Secret = new V1SecretVolumeSource { SecretName = "app-secret" } },
                new V1Volume { Name = "vol", PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = "data-vol" } }
            ];
            p.Spec.Containers =
            [
                new V1Container
                {
                    Env =
                    [
                        new V1EnvVar
                        {
                            Name = "CFG",
                            ValueFrom = new V1EnvVarSource
                            {
                                ConfigMapKeyRef = new V1ConfigMapKeySelector { Name = "app-config", Key = "k" }
                            }
                        },
                        new V1EnvVar
                        {
                            Name = "SEC",
                            ValueFrom = new V1EnvVarSource
                            {
                                SecretKeyRef = new V1SecretKeySelector { Name = "app-secret", Key = "k" }
                            }
                        }
                    ],
                    EnvFrom =
                    [
                        new V1EnvFromSource { ConfigMapRef = new V1ConfigMapEnvSource { Name = "env-from-cm" } },
                        new V1EnvFromSource { SecretRef = new V1SecretEnvSource { Name = "env-from-sec" } }
                    ]
                }
            ];
        });
        StubPodCenterLists("web-1", pod);
        _k8s.SetupListNamespacedConfigMaps(_ns, Cm("app-config", _ns), Cm("env-from-cm", _ns));
        _k8s.SetupListNamespacedSecrets(_ns, Sec("app-secret", _ns), Sec("env-from-sec", _ns));
        _k8s.SetupListNamespacedClaims(_ns, Claim("data-vol", _ns, "Bound"));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.True(HasEdge(view, "Pod/web-1", "ConfigMap/app-config", "挂载"));
        Assert.True(HasEdge(view, "Pod/web-1", "ConfigMap/app-config", "引用"));
        Assert.True(HasEdge(view, "Pod/web-1", "Secret/app-secret", "挂载"));
        Assert.True(HasEdge(view, "Pod/web-1", "Secret/app-secret", "引用"));
        Assert.True(HasEdge(view, "Pod/web-1", "PersistentVolumeClaim/data-vol", "挂载"));
        Assert.True(HasEdge(view, "Pod/web-1", "ConfigMap/env-from-cm", "引用"));
        Assert.True(HasEdge(view, "Pod/web-1", "Secret/env-from-sec", "引用"));
        Assert.Single(view.Nodes, n => n.Id == "ConfigMap/app-config");
        Assert.Equal("Bound", Node(view, "PersistentVolumeClaim/data-vol")!.StatusHint);
        Assert.Equal(7, view.Edges.Count);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_missing_refs_marked_and_edge_kept()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
        {
            p.Spec.Volumes =
            [
                new V1Volume { Name = "cfg", ConfigMap = new V1ConfigMapVolumeSource { Name = "ghost-cm" } },
                new V1Volume { Name = "vol", PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = "ghost-vol" } }
            ];
        });
        StubPodCenterLists("web-1", pod);
        _k8s.SetupListNamespacedConfigMaps(_ns);
        _k8s.SetupListNamespacedClaims(_ns);

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        var cm = Node(view, "ConfigMap/ghost-cm");
        Assert.NotNull(cm);
        Assert.True(cm!.IsMissing);
        Assert.True(HasEdge(view, "Pod/web-1", "ConfigMap/ghost-cm", "挂载"));
        var vol = Node(view, "PersistentVolumeClaim/ghost-vol");
        Assert.NotNull(vol);
        Assert.True(vol!.IsMissing);
        Assert.True(HasEdge(view, "Pod/web-1", "PersistentVolumeClaim/ghost-vol", "挂载"));
    }

    [Fact]
    public async Task GetTopologyAsync_pod_owner_rs_404_shows_missing()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
            p.Metadata.OwnerReferences =
            [
                new V1OwnerReference { ApiVersion = "apps/v1", Kind = "ReplicaSet", Name = "ghost-rs", Uid = "u-g" }
            ]);
        StubPodCenterLists("web-1", pod);
        _k8s.SetupReadReplicaSetThrows("ghost-rs", _ns, K8sMocks.K8sError(404));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        var rs = Node(view, "ReplicaSet/ghost-rs");
        Assert.NotNull(rs);
        Assert.True(rs!.IsMissing);
        Assert.True(HasEdge(view, "ReplicaSet/ghost-rs", "Pod/web-1", "拥有"));
    }

    [Fact]
    public async Task GetTopologyAsync_pod_owner_read_failure_drops_edge()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
            p.Metadata.OwnerReferences =
            [
                new V1OwnerReference { ApiVersion = "apps/v1", Kind = "Deployment", Name = "ghost-dep", Uid = "u-g" }
            ]);
        StubPodCenterLists("web-1", pod);
        _k8s.SetupReadDeploymentThrows("ghost-dep", _ns, K8sMocks.K8sError(503));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.DoesNotContain(view.Nodes, n => n.Id == "Deployment/ghost-dep");
        Assert.Empty(view.Edges);
    }

    [Fact]
    public async Task GetTopologyAsync_pod_service_list_failure_degrades()
    {
        var clusterId = await SeedClusterAsync();
        var pod = Pod("web-1", _ns, nodeName: null, customize: p =>
        {
            p.Metadata.Labels = Labels("web");
            p.Spec.Volumes = [new V1Volume { Name = "cfg", ConfigMap = new V1ConfigMapVolumeSource { Name = "app-config" } }];
        });
        StubPodCenterLists("web-1", pod);
        _k8s.SetupListServicesThrows(K8sMocks.K8sError(500));
        _k8s.SetupListNamespacedConfigMaps(_ns, Cm("app-config", _ns));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1"));

        Assert.DoesNotContain(view.Edges, e => e.Relation == "选择");
        Assert.True(HasEdge(view, "Pod/web-1", "ConfigMap/app-config", "挂载"));
    }

    // ---------- Service 中心 ----------

    [Fact]
    public async Task GetTopologyAsync_service_selects_pods_and_routes()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadService("app-svc", _ns, Service("app-svc", _ns, Labels("web")));
        _k8s.SetupListNamespacedPods(_ns,
            Pod("web-1", _ns, customize: p => p.Metadata.Labels = Labels("web")),
            Pod("other-1", _ns, customize: p => p.Metadata.Labels = Labels("db")));
        _k8s.SetupListNamespacedIngresses(_ns, IngressRouting("web-ing", _ns, "app-svc"));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Service", "app-svc"));

        Assert.True(HasEdge(view, "Service/app-svc", "Pod/web-1", "选择"));
        Assert.DoesNotContain(view.Edges, e => e.To == "Pod/other-1");
        Assert.True(HasEdge(view, "Ingress/web-ing", "Service/app-svc", "路由"));
        var podNode = Node(view, "Pod/web-1");
        Assert.Equal("Running", podNode!.StatusHint);
    }

    [Fact]
    public async Task GetTopologyAsync_service_null_selector_no_pods()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadService("app-svc", _ns, Service("app-svc", _ns));
        _k8s.SetupListNamespacedPods(_ns, Pod("web-1", _ns));
        _k8s.SetupListNamespacedIngresses(_ns);

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Service", "app-svc"));

        Assert.Empty(view.Edges);
    }

    [Fact]
    public async Task GetTopologyAsync_service_ingress_failure_degrades()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadService("app-svc", _ns, Service("app-svc", _ns, Labels("web")));
        _k8s.SetupListNamespacedPods(_ns, Pod("web-1", _ns, customize: p => p.Metadata.Labels = Labels("web")));
        _k8s.SetupListNamespacedIngressesThrows(_ns, K8sMocks.K8sError(500));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Service", "app-svc"));

        Assert.True(HasEdge(view, "Service/app-svc", "Pod/web-1", "选择"));
        Assert.DoesNotContain(view.Edges, e => e.Relation == "路由");
    }

    // ---------- ConfigMap / Secret 中心 ----------

    [Fact]
    public async Task GetTopologyAsync_configmap_reverse_lookup()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMap("app-config", _ns, Cm("app-config", _ns));
        _k8s.SetupListNamespacedPods(_ns,
            Pod("vol-pod", _ns, customize: p =>
                p.Spec.Volumes = [new V1Volume { Name = "cfg", ConfigMap = new V1ConfigMapVolumeSource { Name = "app-config" } }]),
            Pod("env-pod", _ns, customize: p =>
                p.Spec.Containers =
                [
                    new V1Container
                    {
                        Env =
                        [
                            new V1EnvVar
                            {
                                Name = "K",
                                ValueFrom = new V1EnvVarSource
                                {
                                    ConfigMapKeyRef = new V1ConfigMapKeySelector { Name = "app-config", Key = "k" }
                                }
                            }
                        ]
                    }
                ]),
            Pod("unrelated", _ns));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "ConfigMap", "app-config"));

        Assert.True(HasEdge(view, "Pod/vol-pod", "ConfigMap/app-config", "挂载"));
        Assert.True(HasEdge(view, "Pod/env-pod", "ConfigMap/app-config", "引用"));
        Assert.Equal(2, view.Edges.Count);
    }

    [Fact]
    public async Task GetTopologyAsync_configmap_no_references_center_only()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadConfigMap("app-config", _ns, Cm("app-config", _ns));
        _k8s.SetupListNamespacedPods(_ns, Pod("unrelated", _ns));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "ConfigMap", "app-config"));

        var node = Assert.Single(view.Nodes);
        Assert.True(node.IsCenter);
        Assert.Equal("ConfigMap/app-config", node.Id);
        Assert.Empty(view.Edges);
    }

    [Fact]
    public async Task GetTopologyAsync_secret_reverse_lookup_carries_no_values()
    {
        var clusterId = await SeedClusterAsync();
        var secret = new V1Secret
        {
            Metadata = new V1ObjectMeta { Name = "app-secret", NamespaceProperty = _ns },
            Data = new Dictionary<string, byte[]> { ["password"] = "super-secret"u8.ToArray() }
        };
        _k8s.SetupReadSecret("app-secret", _ns, secret);
        _k8s.SetupListNamespacedPods(_ns,
            Pod("vol-pod", _ns, customize: p =>
                p.Spec.Volumes = [new V1Volume { Name = "sec", Secret = new V1SecretVolumeSource { SecretName = "app-secret" } }]),
            Pod("env-pod", _ns, customize: p =>
                p.Spec.Containers =
                [
                    new V1Container
                    {
                        EnvFrom = [new V1EnvFromSource { SecretRef = new V1SecretEnvSource { Name = "app-secret" } }]
                    }
                ]));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Secret", "app-secret"));

        Assert.True(HasEdge(view, "Pod/vol-pod", "Secret/app-secret", "挂载"));
        Assert.True(HasEdge(view, "Pod/env-pod", "Secret/app-secret", "引用"));
        var json = JsonSerializer.Serialize(view);
        Assert.DoesNotContain("password", json);
        Assert.DoesNotContain("super-secret", json);
        Assert.DoesNotContain("Data", json);
        Assert.DoesNotContain("data", json);
        Assert.DoesNotContain(_k8s.Invocations, i =>
        {
            var name = i.Method.Name;
            return name.Contains("Create") || name.Contains("Delete") || name.Contains("Replace") || name.Contains("Patch");
        });
    }

    // ---------- PVC 中心 ----------

    [Fact]
    public async Task GetTopologyAsync_claim_mounts_and_supply_edge()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("data-vol", _ns, Claim("data-vol", _ns, "Bound", volumeName: "pv-data"));
        _k8s.SetupListNamespacedPods(_ns,
            Pod("vol-pod", _ns, customize: p =>
                p.Spec.Volumes = [new V1Volume { Name = "vol", PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = "data-vol" } }]));
        _k8s.SetupReadVolume("pv-data", new V1PersistentVolume
        {
            Metadata = new V1ObjectMeta { Name = "pv-data" }
        });

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "PersistentVolumeClaim", "data-vol"));

        Assert.True(HasEdge(view, "Pod/vol-pod", "PersistentVolumeClaim/data-vol", "挂载"));
        Assert.True(HasEdge(view, "PersistentVolume/pv-data", "PersistentVolumeClaim/data-vol", "供给"));
        Assert.Equal("Bound", Node(view, "PersistentVolumeClaim/data-vol")!.StatusHint);
    }

    [Fact]
    public async Task GetTopologyAsync_claim_unbound_no_supply()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("data-vol", _ns, Claim("data-vol", _ns, "Pending", volumeName: null));
        _k8s.SetupListNamespacedPods(_ns);

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "PersistentVolumeClaim", "data-vol"));

        Assert.DoesNotContain(view.Nodes, n => n.Kind == "PersistentVolume");
        Assert.Empty(view.Edges);
    }

    [Fact]
    public async Task GetTopologyAsync_claim_pv_read_failure_drops_supply()
    {
        var clusterId = await SeedClusterAsync();
        _k8s.SetupReadClaim("data-vol", _ns, Claim("data-vol", _ns, "Bound", volumeName: "pv-gone"));
        _k8s.SetupListNamespacedPods(_ns);
        _k8s.SetupReadVolumeThrows("pv-gone", K8sMocks.K8sError(500));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "PersistentVolumeClaim", "data-vol"));

        Assert.DoesNotContain(view.Nodes, n => n.Kind == "PersistentVolume");
        Assert.DoesNotContain(view.Edges, e => e.Relation == "供给");
    }

    // ---------- 工作负载中心 ----------

    [Theory]
    [InlineData("Deployment")]
    [InlineData("StatefulSet")]
    [InlineData("DaemonSet")]
    [InlineData("Job")]
    public async Task GetTopologyAsync_workload_owns_matching_pods(string kind)
    {
        var clusterId = await SeedClusterAsync();
        SetupEmptyLists(_ns);
        switch (kind)
        {
            case "Deployment":
                _k8s.SetupReadDeployment("web", _ns, new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = _ns },
                    Spec = new V1DeploymentSpec { Selector = new V1LabelSelector { MatchLabels = Labels("web") } }
                });
                break;
            case "StatefulSet":
                _k8s.SetupReadStatefulSet("web", _ns, new V1StatefulSet
                {
                    Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = _ns },
                    Spec = new V1StatefulSetSpec { Selector = new V1LabelSelector { MatchLabels = Labels("web") } }
                });
                break;
            case "DaemonSet":
                _k8s.SetupReadDaemonSetBody("web", _ns, new V1DaemonSet
                {
                    Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = _ns },
                    Spec = new V1DaemonSetSpec { Selector = new V1LabelSelector { MatchLabels = Labels("web") } }
                });
                break;
            default:
                _k8s.SetupReadJob("web", _ns, new V1Job
                {
                    Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = _ns },
                    Spec = new V1JobSpec { Selector = new V1LabelSelector { MatchLabels = Labels("web") } }
                });
                break;
        }

        _k8s.SetupListNamespacedPods(_ns,
            Pod("web-1", _ns, customize: p => p.Metadata.Labels = Labels("web")),
            Pod("other-1", _ns, customize: p => p.Metadata.Labels = Labels("db")));

        var view = await _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, kind, "web"));

        Assert.True(HasEdge(view, $"{kind}/web", "Pod/web-1", "拥有"));
        Assert.Equal(1, view.Edges.Count);
    }

    // ---------- 整体失败路径 ----------

    [Fact]
    public async Task GetTopologyAsync_center_pod_404_translated_to_not_found()
    {
        var clusterId = await SeedClusterAsync();
        SetupEmptyLists(_ns);
        _k8s.SetupReadPodThrows("ghost", _ns, K8sMocks.K8sError(404));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "ghost")));
    }

    [Fact]
    public async Task GetTopologyAsync_cluster_missing_throws_without_k8s_call()
    {
        await SeedClusterAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetTopologyAsync(new TopologyQueryRequest(999, _ns, "Pod", "web-1")));

        Assert.Empty(_k8s.Invocations);
    }

    [Fact]
    public async Task GetTopologyAsync_unsupported_kind_throws_validation()
    {
        var clusterId = await SeedClusterAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Ingress", "web-ing")));

        Assert.Empty(_k8s.Invocations);
    }

    [Fact]
    public async Task GetTopologyAsync_center_unreachable_translated()
    {
        var clusterId = await SeedClusterAsync();
        SetupEmptyLists(_ns);
        _k8s.SetupReadPodThrows("web-1", _ns, new HttpRequestException("boom"));

        await Assert.ThrowsAsync<ClusterUnreachableException>(
            () => _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1")));
    }

    [Fact]
    public async Task GetTopologyAsync_center_unsupported_status_translated_to_validation()
    {
        var clusterId = await SeedClusterAsync();
        SetupEmptyLists(_ns);
        _k8s.SetupReadPodThrows("web-1", _ns, K8sMocks.K8sError(403));

        await Assert.ThrowsAsync<PermissionException>(
            () => _service.GetTopologyAsync(new TopologyQueryRequest(clusterId, _ns, "Pod", "web-1")));
    }

    private static V1ConfigMap Cm(string name, string ns)
        => new() { Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns } };

    private static V1Secret Sec(string name, string ns)
        => new() { Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns } };

    private static V1PersistentVolumeClaim Claim(string name, string ns, string phase, string? volumeName = null)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Spec = new V1PersistentVolumeClaimSpec { VolumeName = volumeName },
            Status = new V1PersistentVolumeClaimStatus { Phase = phase }
        };
}
