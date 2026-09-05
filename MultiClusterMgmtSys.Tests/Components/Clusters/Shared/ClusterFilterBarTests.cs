using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MultiClusterMgmtSys.Components.Clusters.Shared;
using MultiClusterMgmtSys.Requests;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Components.Clusters.Shared;

/// <summary>
/// 接线契约:组件如何把 MudDateRangePicker 的事件回写到查询对象(CreatedFrom/CreatedTo)。
/// 不渲染 MudBlazor 内部弹层,只触发组件实例的公开事件。
/// </summary>
public class ClusterFilterBarTests : TestContext
{
    public ClusterFilterBarTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void DateRangeChanged_UpdatesQueryCreatedFromTo()
    {
        var query = new ClusterQueryRequest();
        var bar = RenderComponent<ClusterFilterBar>(p => p.Add(x => x.Query, query));

        var picker = bar.FindComponent<MudDateRangePicker>();
        var start = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 1, 31);

        picker.InvokeAsync(() => picker.Instance.DateRangeChanged.InvokeAsync(new DateRange(start, end)));

        Assert.Equal(start, query.CreatedFrom);
        Assert.Equal(end, query.CreatedTo);
    }

    [Fact]
    public void ResetFilter_ClearsDateRange()
    {
        var query = new ClusterQueryRequest
        {
            Name = "prod",
            CreatedFrom = new DateTime(2026, 1, 1),
            CreatedTo = new DateTime(2026, 1, 31)
        };
        var bar = RenderComponent<ClusterFilterBar>(p => p.Add(x => x.Query, query));

        var resetButton = bar.FindAll("button").Single(b => b.TextContent.Trim() == "重置");
        resetButton.Click();

        Assert.Null(query.CreatedFrom);
        Assert.Null(query.CreatedTo);
        Assert.Null(query.Name);
    }
}