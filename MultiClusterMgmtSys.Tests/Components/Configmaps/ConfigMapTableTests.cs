using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Configmaps;

public class ConfigMapListTableTests
{
    private static ConfigMapListViewModel Item(string name, string ns = "app", int keys = 2)
        => new()
        {
            Name = name,
            Namespace = ns,
            DataKeyCount = keys,
            DataKeyPreview = "k1, k2",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    [Fact]
    public async Task Renders_items_with_preview()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("cm-a"), Item("cm-b", "kube-system", 5)]));

        Assert.Contains("cm-a", cut.Markup);
        Assert.Contains("app", cut.Markup);
        Assert.Contains("k1, k2", cut.Markup);
        Assert.Contains("kube-system", cut.Markup);
    }

    [Fact]
    public async Task Empty_state_shown_when_no_items()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapListTable>(
            parameters => parameters.Add(p => p.Items, Array.Empty<ConfigMapListViewModel>()));

        Assert.Contains("暂无 ConfigMap", cut.Markup);
    }

    [Fact]
    public async Task Admin_buttons_disappear_when_role_downgraded()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("cm")])
                .Add(p => p.OnNavigateDetail, _ => Task.CompletedTask));

        var adminIcons = cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除");

        auth.SetRoles("Member");
        cut.Render();
        var memberIcons = cut.FindComponents<MudTooltip>().Count(t => t.Instance.Text is "编辑 YAML" or "删除");

        Assert.Equal(2, adminIcons);
        Assert.Equal(0, memberIcons);
    }

    [Fact]
    public async Task Row_click_navigates()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        (string ns, string name)? navigated = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapListTable>(
            parameters => parameters
                .Add(p => p.Items, [Item("web")])
                .Add(p => p.OnNavigateDetail, args => { navigated = args; return Task.CompletedTask; }));

        cut.FindAll(".link-primary").First(e => e.TextContent.Contains("web")).Click();

        Assert.Equal(("app", "web"), navigated!.Value);
    }
}

public class ConfigMapDataViewCardTests
{
    [Fact]
    public async Task Shows_key_value_rows()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapDataViewCard>(
            parameters => parameters.Add(p => p.Data, new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }));

        Assert.Contains("key1", cut.Markup);
        Assert.Contains("value1", cut.Markup);
        Assert.Contains("key2", cut.Markup);
    }

    [Fact]
    public async Task Empty_data_shows_empty_state()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapDataViewCard>(
            parameters => parameters.Add(p => p.Data, new Dictionary<string, string>()));

        Assert.Contains("暂无键", cut.Markup);
    }
}

public class ConfigMapYamlViewCardTests
{
    [Fact]
    public async Task Shows_yaml_in_readonly_textarea()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapYamlViewCard>(
            parameters => parameters.Add(p => p.Yaml, "apiVersion: v1\nkind: ConfigMap"));

        Assert.Contains("apiVersion: v1", cut.Markup);
        Assert.Contains("yaml-textarea", cut.Markup);
        Assert.DoesNotContain("暂无 YAML", cut.Markup);
    }

    [Fact]
    public async Task Empty_yaml_shows_empty_state()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<MultiClusterMgmtSys.Components.Configmaps.Shared.ConfigMapYamlViewCard>(
            parameters => parameters.Add(p => p.Yaml, ""));

        Assert.Contains("暂无 YAML", cut.Markup);
    }
}
