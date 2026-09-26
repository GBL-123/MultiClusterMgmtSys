using MultiClusterMgmtSys.Application.Common.Helm;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmOptionsTests
{
    [Fact]
    public void FromValues_uses_defaults_when_missing()
    {
        var options = HelmOptions.FromValues(null, null);

        Assert.Equal("helm", options.CliPath);
        Assert.Equal(52_428_800, options.MaxPackageBytes);
    }

    [Fact]
    public void FromValues_applies_overrides()
    {
        var options = HelmOptions.FromValues("/opt/helm", "1024");

        Assert.Equal("/opt/helm", options.CliPath);
        Assert.Equal(1024, options.MaxPackageBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("-5")]
    [InlineData("0")]
    public void FromValues_falls_back_on_invalid_limit(string value)
    {
        var options = HelmOptions.FromValues(null, value);

        Assert.Equal(52_428_800, options.MaxPackageBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromValues_falls_back_on_blank_path(string value)
    {
        var options = HelmOptions.FromValues(value, null);

        Assert.Equal("helm", options.CliPath);
    }
}
