using Bunit;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Components.Events.Pages;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class EventsPageTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static void SetupProbe(Mock<k8s.IKubernetes> k8s)
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

    private static Corev1Event NewEvent(
        string name = "api-7f9",
        string ns = "app",
        string type = "Normal",
        string reason = "Scheduled",
        string message = "Successfully assigned",
        int? count = null,
        DateTime? lastTimestamp = null,
        string sourceComponent = "kubelet",
        string kind = "Pod")
        => new()
        {
            Type = type,
            Reason = reason,
            Message = message,
            Count = count,
            LastTimestamp = lastTimestamp,
            Metadata = new V1ObjectMeta { NamespaceProperty = ns },
            InvolvedObject = new V1ObjectReference { Kind = kind, Name = name, NamespaceProperty = ns },
            Source = new V1EventSource { Component = sourceComponent, Host = "node-1" }
        };

    [Fact]
    public async Task Events_page_shows_hint_when_no_cluster()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        ctx.AddEventStack();

        var cut = ctx.Render<Events>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Events_page_offline_shows_unreachable_and_skips_event_api()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(
            TestData.NewCluster("event-offline", status: ClusterStatus.Offline));

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));
        Assert.Contains("集群不可达，无法获取事件", cut.Markup);

        k8s.Verify(x => x.CoreV1.ListEventForAllNamespacesWithHttpMessagesAsync(
            It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<bool?>(),
            It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Events_page_lists_events_with_badge_count_and_time()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-source"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListEvents(
            NewEvent(type: "Warning", reason: "BackOff", message: "Back-off restarting failed container",
                count: 12, lastTimestamp: DateTime.Now.AddMinutes(-3)),
            NewEvent(name: "cache-0", reason: "Scheduled", lastTimestamp: new DateTime(2026, 9, 14, 10, 31, 2)));

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("BackOff"));

        Assert.Contains("警告", cut.Markup);
        Assert.Contains("Warning", cut.Markup);
        Assert.Contains("×12", cut.Markup);
        Assert.Contains("分钟前", cut.Markup);
        Assert.Contains("Pod/api-7f9", cut.Markup);
        Assert.Contains("共 2 条", cut.Markup);

        var tooltips = cut.FindComponents<TextTooltip>();
        Assert.Contains(tooltips, t => t.Instance.Text == "2026-09-14 10:31:02");
    }

    [Fact]
    public async Task Events_page_keyword_and_type_filters_are_instant()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-filter"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListEvents(
            NewEvent(type: "Warning", reason: "BackOff", message: "Back-off restarting failed container"),
            NewEvent(name: "cache-0", reason: "Scheduled", message: "Successfully assigned"));

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("BackOff") && cut.Markup.Contains("Scheduled"));

        var keywordField = cut.FindComponents<MudTextField<string>>().First();
        await cut.InvokeAsync(() => keywordField.Instance.ValueChanged!.InvokeAsync("BackOff"));
        cut.WaitForState(() => !cut.Markup.Contains("Scheduled"));
        Assert.Contains("BackOff", cut.Markup);

        await cut.InvokeAsync(() => keywordField.Instance.ValueChanged!.InvokeAsync(""));
        cut.WaitForState(() => cut.Markup.Contains("Scheduled"));

        var typeSelect = cut.FindComponents<MudSelect<string>>().Last();
        await cut.InvokeAsync(() => typeSelect.Instance.ValueChanged!.InvokeAsync("Warning"));
        cut.WaitForState(() => !cut.Markup.Contains("Scheduled"));
        Assert.Contains("警告", cut.Markup);
    }

    [Fact]
    public async Task Events_page_opens_detail_dialog_and_closes()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-detail"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        var ev = NewEvent(type: "Warning", reason: "BackOff", message: "Back-off restarting failed container",
            count: 12, lastTimestamp: new DateTime(2026, 9, 14, 10, 31, 2));
        ev.InvolvedObject.FieldPath = "spec.containers{app}";
        k8s.SetupListEvents(ev);

        var provider = ctx.Render<MudDialogProvider>();
        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("BackOff"));
        cut.FindAll("tbody tr").First().Click();

        provider.WaitForState(() => provider.Markup.Contains("字段路径"), TimeSpan.FromSeconds(5));
        Assert.Contains("spec.containers{app}", provider.Markup);
        Assert.Contains("kubelet", provider.Markup);
        Assert.Contains("×12", provider.Markup);
        Assert.Contains("Back-off restarting failed container", provider.Markup);

        provider.FindAll("button").First(b => b.TextContent.Contains("关闭")).Click();
        provider.WaitForState(() => !provider.Markup.Contains("字段路径"), TimeSpan.FromSeconds(5));

        Assert.Contains("BackOff", cut.Markup);
    }

    [Fact]
    public async Task Events_page_visible_to_member_without_mutation_affordances()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("member");
        var (harness, k8s) = ctx.AddEventStack("member");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-member"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        k8s.SetupListEvents(NewEvent());

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("api-7f9"));
        Assert.Contains("刷新", cut.Markup);
        Assert.DoesNotContain("新建", cut.Markup);
        Assert.DoesNotContain("删除", cut.Markup);
    }

    private static List<Corev1Event> KindFixtureEvents()
        => new()
        {
            NewEvent(name: "api-1", type: "Warning", reason: "BackOff", message: "Back-off restarting failed container"),
            NewEvent(name: "api-2", type: "Normal", reason: "Scheduled", message: "Successfully assigned to node"),
            NewEvent(name: "apiserver-0", ns: "default", kind: "Node", reason: "NodeReady", message: "Node is ready")
        };

    [Fact]
    public async Task Events_page_kind_chips_render_counts_and_filter_instantly()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-kind"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app", "default");
        k8s.SetupListEvents([.. KindFixtureEvents()]);

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("对象类型"));
        Assert.Contains("全部 3", cut.Markup);
        Assert.Contains("Pod 2", cut.Markup);
        Assert.Contains("Node 1", cut.Markup);

        var chips = cut.FindComponent<MudChipSet<string>>();
        await cut.InvokeAsync(() => chips.Instance.SelectedValueChanged!.InvokeAsync("Pod"));

        cut.WaitForState(() => !cut.Markup.Contains("apiserver-0"));
        Assert.Contains("api-1", cut.Markup);
        Assert.Contains("api-2", cut.Markup);
    }

    [Fact]
    public async Task Events_page_kind_counts_reflect_level_filter_and_do_not_collapse()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-kind-lv"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app", "default");
        k8s.SetupListEvents([.. KindFixtureEvents()]);

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("对象类型"));

        var levelSelect = cut.FindComponents<MudSelect<string>>().Last();
        await cut.InvokeAsync(() => levelSelect.Instance.ValueChanged!.InvokeAsync("Warning"));

        cut.WaitForState(() => cut.Markup.Contains("全部 1"));
        Assert.Contains("Pod 1", cut.Markup);
        Assert.DoesNotContain("Node 1", cut.Markup);

        var chips = cut.FindComponent<MudChipSet<string>>();
        await cut.InvokeAsync(() => chips.Instance.SelectedValueChanged!.InvokeAsync("Pod"));

        cut.WaitForState(() => !cut.Markup.Contains("api-2"));
        Assert.Contains("api-1", cut.Markup);
        Assert.Contains("全部 1", cut.Markup);
        Assert.Contains("Pod 1", cut.Markup);
    }

    [Fact]
    public async Task Events_page_reset_returns_kind_to_all()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-kind-reset"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app", "default");
        k8s.SetupListEvents([.. KindFixtureEvents()]);

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("对象类型"));

        var chips = cut.FindComponent<MudChipSet<string>>();
        await cut.InvokeAsync(() => chips.Instance.SelectedValueChanged!.InvokeAsync("Pod"));
        cut.WaitForState(() => !cut.Markup.Contains("apiserver-0"));

        cut.FindAll("button").First(b => b.TextContent.Contains("重置")).Click();
        cut.WaitForState(() => cut.Markup.Contains("apiserver-0"));

        Assert.Contains("全部 3", cut.Markup);
    }

    [Fact]
    public async Task Events_page_empty_kind_events_only_visible_under_all()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddEventStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("event-kind-empty"));
        SetupProbe(k8s);
        k8s.SetupListNamespaces("app");
        var mystery = NewEvent(name: "mystery", reason: "Unknown", message: "Object without kind");
        mystery.InvolvedObject = new V1ObjectReference { Name = "mystery" };
        k8s.SetupListEvents(NewEvent(name: "pod-1"), mystery);

        var cut = ctx.Render<Events>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("mystery"));

        var chipset = cut.FindComponent<MudChipSet<string>>();
        var chipValues = chipset.FindComponents<MudChip<string>>().Select(c => c.Instance.Value).ToList();
        Assert.Equal(2, chipValues.Count);
        Assert.Contains(string.Empty, chipValues);
        Assert.Contains("Pod", chipValues);
        Assert.Contains("全部 2", cut.Markup);
        Assert.Contains("Pod 1", cut.Markup);
    }
}
