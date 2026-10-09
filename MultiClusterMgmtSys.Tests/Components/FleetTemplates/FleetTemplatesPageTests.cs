using Bunit;
using MudBlazor;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.FleetTemplates;

/// <summary>
/// 舰队模板页接线测试(bUnit):表单骨架、按钮禁用态、变量矩阵、预览行渲染;预览与下发语义由服务层测试覆盖。
/// 矩阵驱动经反射 + SetParametersAndRender(MudSelect 多选菜单在 bUnit 下不可交互,实测);等待条件用页面实例字段而非含糊文案。
/// </summary>
public class FleetTemplatesPageTests
{
    private const string Template = """
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: cm-fleet
          namespace: app
        data:
          color: {{color}}
        """;

    private const string BrokenTemplate = "a: [1,\n";

    [Fact]
    public async Task Initial_render_shows_form_and_disabled_actions()
    {
        await using var ctx = new BunitHost();
        _ = ctx.AddFleetStack();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.FleetTemplates.Pages.FleetTemplates>();
        cut.WaitForState(() => ((List<ClusterViewModel>)ReflectGet(cut, "_clusters")).Count == 0);

        Assert.Multiple(
            () => Assert.Contains("舰队模板下发", cut.Markup),
            () => Assert.Contains("模板 YAML", cut.Markup),
            () => Assert.Contains("[ 未提取到变量 ]", cut.Markup),
            () => Assert.True(cut.FindComponents<MudButton>()
                .Where(b => b.Markup.Contains("预览") || b.Markup.Contains("下发"))
                .All(b => b.Instance.Disabled)));
    }

    [Fact]
    public async Task Template_with_cluster_selects_show_matrix_and_enable_preview()
    {
        await using var ctx = new BunitHost();
        var (harness, _) = ctx.AddFleetStack();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.FleetTemplates.Pages.FleetTemplates>();
        cut.WaitForState(() => ((List<ClusterViewModel>)ReflectGet(cut, "_clusters")).Count == 1);

        ReflectSet(cut, "_templateYaml", Template);
        ReflectSet(cut, "_vars", (IReadOnlyList<string>)new[] { "color" });
        ReflectSet(cut, "_selectedClusterIds", new List<int> { cluster.Id });
        cut.Render();

        Assert.Multiple(
            () => Assert.Contains("变量:", cut.Markup),
            () => Assert.Contains("color", cut.Markup),
            () => Assert.False(cut.FindComponents<MudButton>().First(b => b.Markup.Contains("预览")).Instance.Disabled),
            () => Assert.True(cut.FindComponents<MudButton>().First(b => b.Markup.Contains("下发")).Instance.Disabled));
    }

    [Fact]
    public async Task Preview_flow_renders_per_cluster_rows()
    {
        await using var ctx = new BunitHost();
        var (harness, k8s) = ctx.AddFleetStack();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));
        k8s.SetupReadConfigMapThrows("cm-fleet", "app", K8sMocks.K8sError(404));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.FleetTemplates.Pages.FleetTemplates>();
        cut.WaitForState(() => ((List<ClusterViewModel>)ReflectGet(cut, "_clusters")).Count == 1);

        ReflectSet(cut, "_templateYaml", Template);
        ReflectSet(cut, "_selectedClusterIds", new List<int> { cluster.Id });
        ReflectSet(cut, "_matrix", new Dictionary<int, Dictionary<string, string>> { [cluster.Id] = new() { ["color"] = "blue" } });
        await cut.InvokeAsync(() => (Task)ReflectMethod(cut, "PreviewAsync").Invoke(cut.Instance, null));
        cut.WaitForState(() => !((bool)ReflectGet(cut, "_previewing")));
        cut.Render();

        Assert.Multiple(
            () => Assert.Contains("渲染值", cut.Markup),
            () => Assert.Contains("cm-fleet", cut.Markup),
            () => Assert.Contains("color: blue", cut.Markup),
            () => Assert.False(cut.FindComponents<MudButton>().First(b => b.Markup.Contains("下发")).Instance.Disabled));
    }

    [Fact]
    public async Task Broken_template_preview_reports_error_without_rows()
    {
        await using var ctx = new BunitHost();
        var (harness, _) = ctx.AddFleetStack();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.FleetTemplates.Pages.FleetTemplates>();
        cut.WaitForState(() => ((List<ClusterViewModel>)ReflectGet(cut, "_clusters")).Count == 1);

        ReflectSet(cut, "_templateYaml", BrokenTemplate);
        ReflectSet(cut, "_selectedClusterIds", new List<int> { cluster.Id });
        ReflectSet(cut, "_matrix", new Dictionary<int, Dictionary<string, string>> { [cluster.Id] = new() { ["color"] = "blue" } });
        await cut.InvokeAsync(() => (Task)ReflectMethod(cut, "PreviewAsync").Invoke(cut.Instance, null));
        cut.WaitForState(() => !((bool)ReflectGet(cut, "_previewing")));
        cut.Render();

        Assert.Multiple(
            () => Assert.DoesNotContain("渲染值", cut.Markup),
            () => Assert.True(cut.FindComponents<MudButton>().First(b => b.Markup.Contains("下发")).Instance.Disabled));
    }

    [Fact]
    public async Task Drawer_contains_fleet_templates_nav_entry_for_admin_only()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var adminCut = ctx.Render<MultiClusterMgmtSys.Web.Components.Layout.Drawer>(
            parameters => parameters.Add(p => p.IsOpen, true));
        var adminHasLink = adminCut.Markup.Contains("舰队模板") && adminCut.Markup.Contains("/fleet-templates");

        auth.SetRoles("Member");
        var memberCut = ctx.Render<MultiClusterMgmtSys.Web.Components.Layout.Drawer>(
            parameters => parameters.Add(p => p.IsOpen, true));
        var memberHasLink = memberCut.Markup.Contains("/fleet-templates");

        Assert.Multiple(
            () => Assert.True(adminHasLink),
            () => Assert.False(memberHasLink));
    }

    private static object ReflectGet<TPage>(IRenderedComponent<TPage> cut, string field)
        where TPage : Microsoft.AspNetCore.Components.IComponent
        => cut.Instance.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(cut.Instance)
            ?? throw new InvalidOperationException($"Field {field} not found");

    private static System.Reflection.MethodInfo ReflectMethod<TPage>(IRenderedComponent<TPage> cut, string name)
        where TPage : Microsoft.AspNetCore.Components.IComponent
        => cut.Instance.GetType().GetMethod(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method {name} not found");

    private static void ReflectSet<TPage>(IRenderedComponent<TPage> cut, string field, object value)
        where TPage : Microsoft.AspNetCore.Components.IComponent
    {
        var target = cut.Instance.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field {field} not found");
        target.SetValue(cut.Instance, value);
    }
}
