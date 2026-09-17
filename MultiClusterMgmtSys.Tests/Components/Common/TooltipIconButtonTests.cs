using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Common;

public class TooltipIconButtonTests
{
    [Fact]
    public async Task Click_hides_tooltip_before_invoking_callback()
    {
        await using var ctx = new BunitHost();
        var clickCount = 0;
        var callback = EventCallback.Factory.Create(this, () => clickCount++);
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Common.TooltipIconButton>(
            parameters => parameters
                .Add(p => p.Text, "编辑分组")
                .Add(p => p.Icon, Icons.Material.Filled.Edit)
                .Add(p => p.OnClick, callback));

        var tooltip = cut.FindComponent<MudTooltip>();
        cut.Find(".mud-tooltip-root").TriggerEvent("onpointerenter", new PointerEventArgs());
        cut.WaitForAssertion(() => Assert.True(tooltip.Instance.Visible));

        cut.Find("button").Click();

        Assert.False(tooltip.Instance.Visible);
        Assert.Equal(1, clickCount);
        Assert.Equal("编辑分组", cut.Find("button").GetAttribute("aria-label"));
    }
}
