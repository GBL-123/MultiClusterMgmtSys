using Bunit;
using MudBlazor;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Clusters;

public class ClusterFilterBarTests
{
    [Fact]
    public async Task DateRangeChanged_writes_query_dates()
    {
        await using var ctx = new BunitHost();
        var query = new ClusterQueryRequest();
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterFilterBar>(
            parameters => parameters.Add(p => p.Query, query));

        var picker = cut.FindComponent<MudDateRangePicker>();
        await cut.InvokeAsync(() => picker.Instance.DateRangeChanged.InvokeAsync(
            new DateRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))));

        Assert.Equal(new DateTime(2026, 1, 1), query.CreatedFrom!.Value.Date);
        Assert.Equal(new DateTime(2026, 1, 31), query.CreatedTo!.Value.Date);
    }

    [Fact]
    public async Task Search_button_invokes_on_filter_changed()
    {
        await using var ctx = new BunitHost();
        var fired = false;
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterFilterBar>(
            parameters => parameters.Add(p => p.OnFilterChanged, () => { fired = true; return Task.CompletedTask; }));

        var search = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("查询"));
        await cut.InvokeAsync(() => search.Instance.OnClick.InvokeAsync());

        Assert.True(fired);
    }

    [Fact]
    public async Task Reset_clears_query_and_invokes_on_reset()
    {
        await using var ctx = new BunitHost();
        var reset = false;
        var query = new ClusterQueryRequest
        {
            Name = "x",
            VersionSelection = "1.29.0",
            CreatedFrom = new DateTime(2026, 1, 1),
            CreatedTo = new DateTime(2026, 1, 2)
        };
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterFilterBar>(
            parameters => parameters
                .Add(p => p.Query, query)
                .Add(p => p.OnReset, () => { reset = true; return Task.CompletedTask; }));

        var resetButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("重置"));
        await cut.InvokeAsync(() => resetButton.Instance.OnClick.InvokeAsync());

        Assert.Null(query.Name);
        Assert.Null(query.Status);
        Assert.Null(query.CreatedFrom);
        Assert.Null(query.CreatedTo);
        Assert.Equal(VersionFilterSentinel.All, query.VersionSelection);
        Assert.True(reset);
    }

    [Fact]
    public async Task AvailableVersions_wired_to_version_select()
    {
        await using var ctx = new BunitHost();
        var cut = ctx.Render<MultiClusterMgmtSys.Components.Clusters.Shared.ClusterFilterBar>(
            parameters => parameters.Add(p => p.AvailableVersions, ["1.29.0", "1.30.0"]));

        var versionSelect = cut.FindComponents<MudSelect<string?>>()
            .Single(s => s.Instance.Label == "版本");

        Assert.NotNull(versionSelect);
        Assert.True(versionSelect.Instance.Items is not null);
    }
}
