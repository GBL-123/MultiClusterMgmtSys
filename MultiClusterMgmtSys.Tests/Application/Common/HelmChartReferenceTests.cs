using MultiClusterMgmtSys.Application.Common.Helm;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmChartReferenceTests
{
    [Theory]
    [InlineData("nginx-1.2.3", "nginx", "1.2.3")]
    [InlineData("my-app-1.2.3", "my-app", "1.2.3")]
    [InlineData("redis-20.0.0", "redis", "20.0.0")]
    public void Parse_splits_name_and_version(string chart, string expectedName, string expectedVersion)
    {
        var reference = HelmChartReference.Parse(chart);

        Assert.Equal(expectedName, reference.Name);
        Assert.Equal(expectedVersion, reference.Version);
    }

    [Theory]
    [InlineData("mychart", "mychart")]
    [InlineData("chart-abc", "chart-abc")]
    [InlineData("", "")]
    public void Parse_falls_back_to_raw_name_without_version(string chart, string expectedName)
    {
        var reference = HelmChartReference.Parse(chart);

        Assert.Equal(expectedName, reference.Name);
        Assert.Equal("", reference.Version);
    }
}
