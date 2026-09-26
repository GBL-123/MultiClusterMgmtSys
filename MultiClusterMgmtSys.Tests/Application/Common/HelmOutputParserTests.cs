using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmOutputParserTests
{
    [Fact]
    public void ParseReleaseList_reads_fields_and_both_time_formats()
    {
        var items = HelmOutputParser.ParseReleaseList(HelmFixtures.ReleaseListJson);

        Assert.Equal(2, items.Count);
        var nginx = items[0];
        Assert.Equal("nginx", nginx.Name);
        Assert.Equal("web", nginx.Namespace);
        Assert.Equal(3, nginx.Revision);
        Assert.Equal("deployed", nginx.Status);
        Assert.Equal("nginx-1.2.3", nginx.Chart);
        Assert.Equal("1.25.0", nginx.AppVersion);
        Assert.NotNull(nginx.UpdatedAt);
        Assert.Equal(
            new DateTime(2026, 9, 20, 10, 12, 33, 123, DateTimeKind.Utc),
            nginx.UpdatedAt!.Value,
            TimeSpan.FromMilliseconds(1));
        Assert.NotNull(items[1].UpdatedAt);
        Assert.Equal("failed", items[1].Status);
    }

    [Fact]
    public void ParseReleaseList_returns_empty_for_empty_array()
    {
        Assert.Empty(HelmOutputParser.ParseReleaseList("[]"));
    }

    [Fact]
    public void ParseReleaseList_throws_business_exception_on_invalid_json()
    {
        Assert.Throws<HelmOperationException>(() => HelmOutputParser.ParseReleaseList("not json"));
    }

    [Fact]
    public void ParseStatus_reads_nested_fields()
    {
        var status = HelmOutputParser.ParseStatus(HelmFixtures.StatusJson);

        Assert.Equal("nginx", status.Name);
        Assert.Equal("web", status.Namespace);
        Assert.Equal(3, status.Revision);
        Assert.Equal("deployed", status.Status);
        Assert.Equal("Upgrade complete", status.Description);
        Assert.Contains("POD_NAME", status.Notes);
        Assert.Equal("nginx", status.ChartName);
        Assert.Equal("1.2.3", status.ChartVersion);
        Assert.Equal("1.25.0", status.AppVersion);
        Assert.Contains("kind: Deployment", status.Manifest);
        Assert.NotNull(status.LastDeployedAt);
    }

    [Fact]
    public void ParseHistory_reads_items_in_order()
    {
        var history = HelmOutputParser.ParseHistory(HelmFixtures.HistoryJson);

        Assert.Equal(2, history.Count);
        Assert.Equal(1, history[0].Revision);
        Assert.Equal("superseded", history[0].Status);
        Assert.Equal("Install complete", history[0].Description);
        Assert.Equal("nginx-1.2.2", history[0].Chart);
        Assert.Equal(3, history[1].Revision);
        Assert.NotNull(history[1].UpdatedAt);
    }

    [Theory]
    [InlineData("2026-09-20T10:12:33.123456Z")]
    [InlineData("2026-09-20 10:12:33.123456789 +0000 UTC")]
    [InlineData("2026-09-20T10:12:33Z")]
    [InlineData("2026-09-20 10:12:33 +0000 UTC")]
    public void TryParseTime_parses_supported_formats(string value)
    {
        var parsed = HelmOutputParser.TryParseTime(value);

        Assert.NotNull(parsed);
        Assert.Equal(2026, parsed!.Value.Year);
        Assert.Equal(DateTimeKind.Utc, parsed.Value.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a time")]
    public void TryParseTime_returns_null_for_unrecognized(string? value)
    {
        Assert.Null(HelmOutputParser.TryParseTime(value));
    }
}
