using Bunit;
using Bunit.TestDoubles;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Common;
using MultiClusterMgmtSys.Components.Configmaps.Shared;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;
using ConfigMapDetailPage = MultiClusterMgmtSys.Components.Configmaps.Pages.ConfigMapDetail;

namespace MultiClusterMgmtSys.Tests.Components.Configmaps.Shared;

/// <summary>
/// 接线契约:ConfigMap 键值卡——行数与 Data 键数一致、空态分支、
/// 点击行打开全文查看对话框;详情页默认 YAML tab、键值 tab 挂载。
/// </summary>
public class ConfigMapDataViewCardTests
{
    private static TestContext CreateContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddScoped<ExceptionPresenter>();
        return ctx;
    }

    [Fact]
    public void Rows_MatchDataKeys_MonoValueCell()
    {
        using var ctx = CreateContext();
        var data = new Dictionary<string, string>
        {
            ["b.properties"] = "key2=value2",
            ["a.conf"] = "key1=value1"
        };

        var card = ctx.RenderComponent<ConfigMapDataViewCard>(p => p.Add(x => x.Data, data));

        var cells = card.FindAll(".cm-value-cell");
        Assert.Equal(2, cells.Count);
        Assert.Contains("key1=value1", cells[0].TextContent);
    }

    [Fact]
    public void EmptyData_ShowsEmptyState()
    {
        using var ctx = CreateContext();

        var card = ctx.RenderComponent<ConfigMapDataViewCard>(p => p.Add(x => x.Data, new Dictionary<string, string>()));

        Assert.Single(card.FindAll(".empty-state"));
        Assert.Contains("暂无键值", card.Markup);
        Assert.Empty(card.FindAll(".cm-value-cell"));
    }

    [Fact]
    public void RowClick_OpensValueDialog()
    {
        using var ctx = CreateContext();
        var provider = ctx.RenderComponent<MudDialogProvider>();

        var data = new Dictionary<string, string> { ["app.conf"] = "key=value" };
        var card = ctx.RenderComponent<ConfigMapDataViewCard>(p => p.Add(x => x.Data, data));

        card.FindAll("tbody tr")[0].Click();
        provider.WaitForState(() => provider.FindComponents<ConfigMapValueDialog>().Count == 1);

        var dialog = provider.FindComponent<ConfigMapValueDialog>();
        Assert.Equal("app.conf", dialog.Instance.Key);
        Assert.Equal("key=value", dialog.Instance.Value);
    }
}

/// <summary>
/// 接线契约:ConfigMap 详情页双 tab——默认 YAML tab,键值 tab 切换后挂载数据卡。
/// </summary>
public class ConfigMapDetailPageTests
{
    private static TestContext CreateContext(Dictionary<string, string> data)
    {
        var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("测试集群", ClusterStatus.Online));
        db.SaveChanges();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap>
            {
                Body = new V1ConfigMap
                {
                    Metadata = new V1ObjectMeta { Name = "cm-1", NamespaceProperty = "default" },
                    Data = data
                }
            });

        var ctx = BunitHost.Create(db);
        ctx.Services.AddScoped<ConfigMapService>(_ => TestServices.ConfigMap(db, _ => fake.Object));

        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles(["Member"]);
        return ctx;
    }

    [Fact]
    public void Tabs_DefaultShowsYamlOfTwo()
    {
        using var ctx = CreateContext(new Dictionary<string, string> { ["app.conf"] = "key=value" });

        var page = ctx.RenderComponent<ConfigMapDetailPage>(p =>
        {
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.Namespace, "default");
            p.Add(x => x.Name, "cm-1");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        Assert.Equal(2, tabs.Instance.Panels.Count);
        Assert.Single(page.FindComponents<ConfigMapYamlViewCard>());
        Assert.Empty(page.FindComponents<ConfigMapDataViewCard>());
    }

    [Fact]
    public async Task SwitchToKeyValueTab_ShowsDataCard()
    {
        using var ctx = CreateContext(new Dictionary<string, string> { ["app.conf"] = "key=value" });

        var page = ctx.RenderComponent<ConfigMapDetailPage>(p =>
        {
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.Namespace, "default");
            p.Add(x => x.Name, "cm-1");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        await page.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(1));

        Assert.Single(page.FindComponents<ConfigMapDataViewCard>());
        Assert.Empty(page.FindComponents<ConfigMapYamlViewCard>());
    }
}
