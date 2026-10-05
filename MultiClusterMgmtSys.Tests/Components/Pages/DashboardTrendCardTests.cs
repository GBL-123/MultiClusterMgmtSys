using System.Text.Json;
using Bunit;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using MultiClusterMgmtSys.Web.Components.Dashboard.Shared;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class DashboardTrendCardTests
{
    private static readonly DateTime Base = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

    private const string ModulePath = "/js/dashboard-trend.js";

    [Fact]
    public async Task Trend_card_mounts_echarts_with_step_series_payload()
    {
        await using var ctx = new BunitHost();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var series = new List<DashboardTrendPointViewModel>
        {
            new(Base.AddHours(-2), 4, 0),
            new(Base.AddHours(-1), 10, 2),
            new(Base, 9, 3)
        };

        var cut = ctx.Render<DashboardNodeTrendCard>(parameters => parameters
            .Add(p => p.Series, series)
            .Add(p => p.NowUtc, Base));

        var chart = cut.Find(".dashboard-trend-chart");
        Assert.Equal("节点就绪趋势", chart.GetAttribute("aria-label"));

        var mounts = InvocationsOf(module, "mount");
        Assert.Single(mounts);
        var payload = AsJson(mounts[^1].Arguments![^1]);
        Assert.Equal(FormatIso(Base.AddHours(-24)), payload.GetProperty("windowStart").GetString());
        Assert.Equal(FormatIso(Base), payload.GetProperty("windowEnd").GetString());

        var seriesPayload = payload.GetProperty("series");
        Assert.Equal(2, seriesPayload.GetArrayLength());
        var ready = seriesPayload[0];
        Assert.Equal("就绪", ready.GetProperty("name").GetString());
        Assert.Equal("#346538", ready.GetProperty("color").GetString());
        var readyPoints = ready.GetProperty("points");
        Assert.Equal(3, readyPoints.GetArrayLength());
        Assert.Equal(FormatIso(Base.AddHours(-2)), readyPoints[0].GetProperty("t").GetString());
        Assert.Equal(4, readyPoints[0].GetProperty("v").GetInt32());
        Assert.Equal(9, readyPoints[2].GetProperty("v").GetInt32());

        var notReady = seriesPayload[1];
        Assert.Equal("未就绪", notReady.GetProperty("name").GetString());
        Assert.Equal("#9F2F2D", notReady.GetProperty("color").GetString());
        Assert.Equal(2, notReady.GetProperty("points")[1].GetProperty("v").GetInt32());

        var head = cut.Find(".dashboard-card-head").TextContent;
        Assert.Contains("节点就绪趋势", head);
        Assert.Contains("就绪", head);
        Assert.Contains("未就绪", head);
        Assert.Contains("近 24 小时", head);
        Assert.Single(cut.FindAll(".dashboard-trend-swatch.is-online"));
        Assert.Single(cut.FindAll(".dashboard-trend-swatch.is-offline"));
    }

    [Fact]
    public async Task Trend_card_shows_empty_state_without_series()
    {
        await using var ctx = new BunitHost();

        var cut = ctx.Render<DashboardNodeTrendCard>(parameters => parameters
            .Add(p => p.Series, Array.Empty<DashboardTrendPointViewModel>()));

        var empty = cut.Find(".empty-state");
        Assert.Contains("暂无趋势数据", empty.TextContent);
        Assert.Empty(cut.FindAll(".dashboard-trend-chart"));
    }

    [Fact]
    public async Task Trend_card_updates_chart_when_series_changes()
    {
        await using var ctx = new BunitHost();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        var first = new List<DashboardTrendPointViewModel> { new(Base.AddHours(-1), 2, 0) };
        var second = new List<DashboardTrendPointViewModel> { new(Base.AddHours(-1), 2, 0), new(Base, 5, 1) };

        var cut = ctx.Render<DashboardNodeTrendCard>(parameters => parameters
            .Add(p => p.Series, first)
            .Add(p => p.NowUtc, Base));

        cut.Render(parameters => parameters.Add(p => p.Series, second));

        Assert.Single(InvocationsOf(module, "mount"));
        var updates = InvocationsOf(module, "update");
        Assert.Single(updates);
        var updatePayload = AsJson(updates[^1].Arguments![^1]);
        Assert.Equal(2, updatePayload.GetProperty("series")[0].GetProperty("points").GetArrayLength());
    }

    [Fact]
    public async Task Trend_card_disposes_chart_on_component_dispose()
    {
        BunitJSModuleInterop module;
        await using (var ctx = new BunitHost())
        {
            module = ctx.JSInterop.SetupModule(ModulePath);
            ctx.Render<DashboardNodeTrendCard>(parameters => parameters
                .Add(p => p.Series, new List<DashboardTrendPointViewModel> { new(Base, 1, 0) })
                .Add(p => p.NowUtc, Base));
        }

        Assert.Single(InvocationsOf(module, "dispose"));
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

    private static string FormatIso(DateTime value)
        => value.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
