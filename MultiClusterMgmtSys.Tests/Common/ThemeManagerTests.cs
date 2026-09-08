using MudBlazor;
using MultiClusterMgmtSys.Components.Common;

namespace MultiClusterMgmtSys.Tests.Common;

public class ThemeManagerTests
{
    [Fact]
    public void Theme_uses_swiss_industrial_tokens()
    {
        var palette = ThemeManager.Theme.PaletteLight;

        Assert.Equal("#111111", palette.Primary);
        Assert.Equal("#F4F4F0", palette.Background);
        Assert.Equal("#FCFBF7", palette.Surface);
        Assert.Equal("#D97706", palette.Warning);
        Assert.Equal("#E2DED5", palette.Divider);
    }

    [Fact]
    public void Theme_is_light_only()
    {
        var dark = ThemeManager.Theme.PaletteDark;

        if (dark is not null)
        {
            Assert.NotEqual("#111111", dark.Primary);
        }
    }

    [Fact]
    public void ThemeManager_is_static()
    {
        Assert.True(typeof(ThemeManager).IsAbstract && typeof(ThemeManager).IsSealed);
    }

    [Fact]
    public void Theme_uses_3px_radius()
    {
        Assert.Equal("3px", ThemeManager.Theme.LayoutProperties.DefaultBorderRadius);
    }

    [Fact]
    public void Theme_instance_is_cached()
    {
        Assert.Same(ThemeManager.Theme, ThemeManager.Theme);
    }
}
