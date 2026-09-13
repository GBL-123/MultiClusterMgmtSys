using Bunit;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MudBlazor.Extensions;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Components.Layout;
using MultiClusterMgmtSys.Components.Namespaces.Pages;
using MultiClusterMgmtSys.Components.Namespaces.Shared;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class NamespacesPageTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static void SetupProbe(Mock<IKubernetes> k8s)
    {
        k8s.SetupListNodes(new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });
        k8s.SetupGetVersion("v1.30.2");
    }

    private static V1Namespace NewNamespace(string name, string phase = "Active")
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name },
            Status = new V1NamespaceStatus { Phase = phase }
        };

    [Fact]
    public async Task List_renders_status_badges_and_filters_names_on_query()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-list"));
        SetupProbe(k8s);
        k8s.SetupListNamespaceObjects(NewNamespace("kube-system"), NewNamespace("app-dev", "Terminating"));

        var cut = ctx.Render<Namespaces>(parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("kube-system"));

        Assert.Contains("status-badge online", cut.Markup);
        Assert.Contains("status-badge unknown", cut.Markup);

        var nameField = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "名称");
        await cut.InvokeAsync(async () => await nameField.Instance.ValueChanged!.InvokeAsync("app"));

        var query = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(async () => await query.Instance.OnClick.InvokeAsync());
        cut.WaitForState(() => !cut.Markup.Contains("kube-system"), TimeSpan.FromSeconds(5));

        Assert.Contains("app-dev", cut.Markup);
    }

    [Fact]
    public async Task List_disables_delete_for_protected_namespaces()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-protected"));
        SetupProbe(k8s);
        k8s.SetupListNamespaceObjects(NewNamespace("default"), NewNamespace("app-dev"));

        var cut = ctx.Render<Namespaces>(parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("app-dev"));

        var deleteButtons = cut.FindComponents<MudIconButton>()
            .Where(b => b.Instance.Icon == Icons.Material.Filled.DeleteOutline)
            .ToList();

        Assert.Collection(deleteButtons,
            first => Assert.True(first.Instance.Disabled),
            second => Assert.False(second.Instance.Disabled));
    }

    [Fact]
    public async Task Member_sees_no_create_or_delete_entries()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("member");
        var (harness, k8s) = ctx.AddNamespaceStack("member");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-member"));
        SetupProbe(k8s);
        k8s.SetupListNamespaceObjects(NewNamespace("app-dev"));

        var cut = ctx.Render<Namespaces>(parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("app-dev"));

        Assert.DoesNotContain("新建命名空间", cut.Markup);
        Assert.DoesNotContain(cut.FindComponents<MudIconButton>(),
            b => b.Instance.Icon == Icons.Material.Filled.DeleteOutline);
    }

    [Fact]
    public async Task Unreachable_cluster_shows_notice_and_disables_create()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-offline"));
        k8s.SetupListNodesThrows(K8sMocks.K8sError(500));

        var cut = ctx.Render<Namespaces>(parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("集群不可达，无法获取命名空间"));

        var createButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建命名空间"));
        Assert.True(createButton.Instance.Disabled);
    }

    [Fact]
    public async Task No_cluster_selected_shows_empty_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        ctx.AddNamespaceStack();

        var cut = ctx.Render<Namespaces>();

        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Remembers_cluster_within_session()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-memory"));
        SetupProbe(k8s);
        k8s.SetupListNamespaceObjects(NewNamespace("remembered"));

        ctx.Services.GetRequiredService<ClusterSelectionState>().Set(cluster.Id);

        var cut = ctx.Render<Namespaces>();
        cut.WaitForState(() => cut.Markup.Contains("remembered"));

        Assert.Contains("remembered", cut.Markup);
    }

    [Fact]
    public async Task Create_dialog_preloads_template_and_blocks_missing_name()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-create"));
        SetupProbe(k8s);
        k8s.SetupListNamespaceObjects(NewNamespace("app-dev"));

        var cut = ctx.Render<Namespaces>(parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(() => cut.Markup.Contains("app-dev"));

        var provider = ctx.Render<MudDialogProvider>();
        var createButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建命名空间"));
        createButton.Find("button").Click();
        provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));

        var yamlField = provider.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "YAML");
        Assert.Contains("kind: Namespace", yamlField.Instance.GetState(x => x.Value) ?? "");

        await provider.InvokeAsync(async () => await yamlField.Instance.ValueChanged!.InvokeAsync("""
            apiVersion: v1
            kind: Namespace
            metadata:
              labels:
                team: platform
            """));

        var submit = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("创建"));
        await provider.InvokeAsync(async () => await submit.Instance.OnClick.InvokeAsync());

        k8s.Verify(x => x.CoreV1.CreateNamespaceWithHttpMessagesAsync(
            It.IsAny<V1Namespace>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Detail_renders_toolbar_and_tabs()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-detail"));
        k8s.SetupReadNamespace("dev", new V1Namespace
        {
            Metadata = new V1ObjectMeta
            {
                Name = "dev",
                Labels = new Dictionary<string, string> { ["team"] = "platform" },
                Annotations = new Dictionary<string, string> { ["owner"] = "ops" }
            },
            Status = new V1NamespaceStatus { Phase = "Active" }
        });

        var cut = ctx.Render<NamespaceDetail>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Name, "dev"));
        cut.WaitForState(() => cut.FindComponents<NamespaceYamlViewCard>().Count == 1);

        Assert.Contains("status-badge online", cut.Markup);
        Assert.Contains("返回列表", cut.Markup);

        var tabs = cut.FindComponent<MudTabs>();
        await cut.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(1));

        Assert.NotEmpty(cut.FindComponents<NamespaceLabelsCard>());
        Assert.NotEmpty(cut.FindComponents<NamespaceAnnotationsCard>());
    }

    [Fact]
    public async Task Detail_missing_namespace_shows_empty_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddNamespaceStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("ns-missing"));
        k8s.SetupReadNamespaceThrows("ghost", K8sMocks.K8sError(404));

        var cut = ctx.Render<NamespaceDetail>(parameters => parameters
            .Add(p => p.ClusterId, cluster.Id)
            .Add(p => p.Name, "ghost"));
        cut.WaitForState(() => cut.Markup.Contains("命名空间不存在或已被删除"));

        Assert.Contains("返回列表", cut.Markup);
    }

    [Fact]
    public async Task Drawer_contains_namespaces_link_with_prefix_match()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);

        var cut = ctx.Render<Drawer>(parameters => parameters.Add(p => p.IsOpen, true));

        var link = cut.FindComponents<MudNavLink>().First(l => l.Instance.Href == "/namespaces");
        Assert.Contains("命名空间管理", link.Markup);
        Assert.Equal(NavLinkMatch.Prefix, link.Instance.Match);

        await Task.CompletedTask;
    }
}
