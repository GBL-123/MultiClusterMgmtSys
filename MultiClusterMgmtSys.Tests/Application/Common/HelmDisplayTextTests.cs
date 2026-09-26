using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Tests.Application.Common;

public class HelmDisplayTextTests
{
    [Theory]
    [InlineData("deployed", "已部署", "online")]
    [InlineData("failed", "已失败", "offline")]
    [InlineData("pending-install", "安装中", "unknown")]
    [InlineData("pending-upgrade", "升级中", "unknown")]
    [InlineData("pending-rollback", "回滚中", "unknown")]
    [InlineData("superseded", "已取代", "unknown")]
    [InlineData("uninstalling", "卸载中", "unknown")]
    [InlineData("uninstalled", "已卸载", "unknown")]
    public void StatusText_and_css_class_map_known_statuses(string status, string expectedText, string expectedCssClass)
    {
        Assert.Equal(expectedText, HelmDisplayText.StatusText(status));
        Assert.Equal(expectedCssClass, HelmDisplayText.StatusCssClass(status));
    }

    [Fact]
    public void StatusText_falls_back_to_raw_value_for_unknown_status()
    {
        Assert.Equal("custom-state", HelmDisplayText.StatusText("custom-state"));
        Assert.Equal("unknown", HelmDisplayText.StatusCssClass("custom-state"));
    }
}
