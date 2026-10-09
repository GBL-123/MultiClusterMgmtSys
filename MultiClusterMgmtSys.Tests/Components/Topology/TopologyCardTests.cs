using System.Text.Json;
using Bunit;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Topology;

public class TopologyCardTests
{
    private const string ModulePath = "/js/topology-graph.js";

    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static V1Node ReadyNode(string name) => new()
    {
        Metadata = new V1ObjectMeta { Name = name },
        Status = new V1NodeStatus
        {
            Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
        }
    };

    private static void StubPodCenter(Mock<IKubernetes> k8s, string podName, V1Pod pod)
    {
        k8s.SetupReadPod(podName, "app", pod);
        k8s.SetupListNamespacedServices("app");
        k8s.SetupListNamespacedConfigMaps("app");
        k8s.SetupListNamespacedSecrets("app");
        k8s.SetupListNamespacedClaims("app");
    }

    private static IReadOnlyList<JSRuntimeInvocation> InvocationsOf(BunitJSModuleInterop module, string identifier)
    {
        try
        {
            return module.Invocations[identifier] ?? [];
        }
        catch (KeyNotFoundException)
        {
            return [];
        }
    }

    private static JsonElement AsJson(object? argument) => argument switch
    {
        null => throw new InvalidOperationException("缺少 JS 调用参数"),
        JsonElement element => element,
        _ => JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(argument))
    };

    [Fact]
    public async Task Card_shows_placeholder_and_skips_module_before_activation()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-card"));
        StubPodCenter(k8s, "web-1", K8sMocks.NewPod("web-1", "app"));
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Kind, "Pod")
            .Add(p => p.Name, "web-1"));

        Assert.Contains("尚未加载", cut.Markup);
        Assert.Empty(InvocationsOf(module, "mount"));
    }

    [Fact]
    public async Task Card_mounts_payload_with_center_neighbor_and_edge()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-card"));
        StubPodCenter(k8s, "web-1", K8sMocks.NewPod("web-1", "app"));
        k8s.SetupReadNode("node-1", ReadyNode("node-1"));
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Kind, "Pod")
            .Add(p => p.Name, "web-1"));
        await cut.InvokeAsync(() => cut.Instance.LoadAsync());

        var mounts = InvocationsOf(module, "mount");
        Assert.Single(mounts);
        var payload = AsJson(mounts[^1].Arguments![^1]);
        var nodes = payload.GetProperty("nodes");
        Assert.Equal(2, nodes.GetArrayLength());
        var center = nodes[0];
        Assert.Equal("Pod/web-1", center.GetProperty("id").GetString());
        Assert.True(center.GetProperty("isCenter").GetBoolean());
        Assert.False(center.GetProperty("isMissing").GetBoolean());
        Assert.Equal("Running", center.GetProperty("statusHint").GetString());
        Assert.Equal(0, center.GetProperty("layer").GetInt32());
        var node = nodes[1];
        Assert.Equal("Node/node-1", node.GetProperty("id").GetString());
        Assert.False(node.GetProperty("isCenter").GetBoolean());
        Assert.Equal("Ready", node.GetProperty("statusHint").GetString());
        Assert.Equal(1, node.GetProperty("layer").GetInt32());
        Assert.Equal(0, node.GetProperty("order").GetInt32());
        var edges = payload.GetProperty("edges");
        Assert.Equal(1, edges.GetArrayLength());
        Assert.Equal("Pod/web-1", edges[0].GetProperty("from").GetString());
        Assert.Equal("Node/node-1", edges[0].GetProperty("to").GetString());
        Assert.Equal("调度", edges[0].GetProperty("relation").GetString());
        Assert.Contains("资源拓扑", cut.Markup);
    }

    [Theory]
    [InlineData("Pod/web-1", "/pods/7/app/web-1")]
    [InlineData("Service/svc-a", "/services/7/app/svc-a")]
    [InlineData("ConfigMap/cm-1", "/configmaps/7/app/cm-1")]
    [InlineData("Secret/s-1", "/secrets/7/app/s-1")]
    [InlineData("PersistentVolumeClaim/pvc-1", "/storage/claims/7/app/pvc-1")]
    [InlineData("Node/node-1", "/nodes/7/node-1")]
    [InlineData("Deployment/web", "/workloads/deployments/7/app/web")]
    [InlineData("StatefulSet/web", "/workloads/statefulsets/7/app/web")]
    [InlineData("DaemonSet/agent", "/workloads/daemonsets/7/app/agent")]
    [InlineData("ReplicaSet/web-1", "/workloads/replicasets/7/app/web-1")]
    public async Task Card_click_callback_navigates_by_kind(string nodeId, string expectedTail)
    {
        await using var ctx = new BunitHost();
        ctx.AddTopologyStack();
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, 7)
            .Add(p => p.Namespace, "app"));

        await cut.InvokeAsync(() => cut.Instance.OnNodeClicked(nodeId));

        var uri = ctx.Services.GetRequiredService<NavigationManager>().Uri;
        Assert.EndsWith(expectedTail, uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Card_click_terminal_nodes_does_not_navigate()
    {
        await using var ctx = new BunitHost();
        ctx.AddTopologyStack();
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, 7)
            .Add(p => p.Namespace, "app"));
        var uri0 = ctx.Services.GetRequiredService<NavigationManager>().Uri;

        await cut.InvokeAsync(() => cut.Instance.OnNodeClicked("Ingress/web"));
        await cut.InvokeAsync(() => cut.Instance.OnNodeClicked("PersistentVolume/pv-1"));

        Assert.Equal(uri0, ctx.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Card_shows_error_state_when_service_fails()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-card"));
        k8s.SetupReadPodThrows("web-1", "app", K8sMocks.K8sError(500, "boom"));
        StubPodCenterLists(k8s);
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Kind, "Pod")
            .Add(p => p.Name, "web-1"));
        await cut.InvokeAsync(() => cut.Instance.LoadAsync());

        Assert.Contains("加载拓扑失败,请重试", cut.Markup);
        Assert.Empty(InvocationsOf(module, "mount"));
    }

    private static void StubPodCenterLists(Mock<IKubernetes> k8s)
    {
        k8s.SetupListNamespacedServices("app");
        k8s.SetupListNamespacedConfigMaps("app");
        k8s.SetupListNamespacedSecrets("app");
        k8s.SetupListNamespacedClaims("app");
    }

    [Fact]
    public async Task Card_retry_after_failure_loads_and_mounts()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-card"));
        StubPodCenterLists(k8s);
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Kind, "Pod")
            .Add(p => p.Name, "web-1"));

        k8s.SetupReadPodThrows("web-1", "app", K8sMocks.K8sError(500, "boom"));
        await cut.InvokeAsync(() => cut.Instance.LoadAsync());
        Assert.Contains("加载拓扑失败,请重试", cut.Markup);

        k8s.SetupReadPod("web-1", "app", K8sMocks.NewPod("web-1", "app"));
        k8s.SetupReadNode("node-1", ReadyNode("node-1"));
        await cut.InvokeAsync(() => cut.Instance.LoadAsync());
        cut.WaitForState(() => InvocationsOf(module, "mount").Count == 1);

        Assert.Single(InvocationsOf(module, "mount"));
        Assert.DoesNotContain("加载拓扑失败", cut.Markup);
    }

    [Fact]
    public async Task Card_disposes_module_on_component_dispose()
    {
        BunitJSModuleInterop module;
        {
            await using var ctx = new BunitHost();
            AuthorizeAdmin(ctx);
            var (harness, k8s) = ctx.AddTopologyStack();
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-card"));
            StubPodCenter(k8s, "web-1", K8sMocks.NewPod("web-1", "app"));
            k8s.SetupReadNode("node-1", ReadyNode("node-1"));
            module = ctx.JSInterop.SetupModule(ModulePath);

            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Topology.TopologyGraphCard>(parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Kind, "Pod")
                .Add(p => p.Name, "web-1"));
            await cut.InvokeAsync(() => cut.Instance.LoadAsync());
            cut.WaitForState(() => InvocationsOf(module, "mount").Count == 1);
        }

        Assert.Single(InvocationsOf(module, "dispose"));
    }

    [Fact]
    public async Task PodDetail_loads_topology_only_when_tab_activated()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-page"));
        StubPodCenter(k8s, "web-1", K8sMocks.NewPod("web-1", "app"));
        k8s.SetupReadNode("node-1", ReadyNode("node-1"));
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Pods.Pages.PodDetail>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Name, "web-1"));
        cut.WaitForState(() => cut.Markup.Contains("概览"));
        await Task.Delay(50);

        Assert.Empty(InvocationsOf(module, "mount"));

        await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[3].Click());
        cut.WaitForState(() => InvocationsOf(module, "mount").Count == 1);

        await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[3].Click());
        await Task.Delay(50);
        Assert.Single(InvocationsOf(module, "mount"));
        Assert.Empty(InvocationsOf(module, "update"));
        Assert.Contains("资源拓扑", cut.Markup);
    }

    [Fact]
    public async Task SvcDetail_loads_topology_only_when_tab_activated()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddTopologyStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("topo-svc"));
        k8s.SetupReadService("svc-a", "app", new V1Service
        {
            Metadata = new V1ObjectMeta { Name = "svc-a", NamespaceProperty = "app" },
            Spec = new V1ServiceSpec { ClusterIPs = ["10.0.0.1"] }
        });
        k8s.SetupListNamespacedPods("app");
        k8s.SetupListNamespacedIngresses("app");
        var module = ctx.JSInterop.SetupModule(ModulePath);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Svcs.Pages.SvcDetail>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Namespace, "app")
            .Add(p => p.Name, "svc-a"));
        cut.WaitForState(() => cut.Markup.Contains("YAML"));
        await Task.Delay(50);

        Assert.Empty(InvocationsOf(module, "mount"));

        await cut.InvokeAsync(() => cut.FindAll(".mud-tab")[3].Click());
        cut.WaitForState(() => InvocationsOf(module, "mount").Count == 1);
        Assert.Contains("资源拓扑", cut.Markup);
    }
}
