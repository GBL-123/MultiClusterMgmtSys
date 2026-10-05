using Bunit;
using MudBlazor;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Compare;

public class ComparePageTests
{
    [Fact]
    public async Task Initial_render_shows_toolbar_hint_and_disabled_button()
    {
        await using var ctx = new BunitHost();
        _ = ctx.AddCompareStack();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");

        var providers = ctx.Render<MultiClusterMgmtSys.Web.Components.Compare.Pages.Compare>();

        Assert.Multiple(
            () => Assert.Contains("选择要对照的资源后点击「对照」", providers.Markup),
            () => Assert.Contains("源集群", providers.Markup),
            () => Assert.Contains("对照集群", providers.Markup),
            () => Assert.Contains("资源族", providers.Markup),
            () => Assert.Contains("命名空间", providers.Markup),
            () => Assert.Contains("资源名称", providers.Markup),
            () => Assert.True(providers.FindComponents<MudButton>()
                .Any(b => b.Markup.Contains("对照") && b.Instance.Disabled)));
    }

    [Fact]
    public async Task Cluster_select_items_match_select_nullable_type()
    {
        // 回归:MudSelect<int?> 里的 MudSelectItem 若被推断为 <int>,
        // 点击选项时会在 MudSelectItem.get_MudSelect 处抛 InvalidCastException。
        // bUnit 下选项渲染在 MudPopoverProvider 中,需显式渲染该 provider。
        await using var ctx = new BunitHost();
        var harness = ctx.AddCompareStack().Harness;
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));
        await harness.ClusterRepo.AddAsync(TestData.NewCluster("beta"));

        // MudSelect 对 ChildContent 会额外渲染一份隐藏 shadow items(不依赖 popover 打开),
        // 直接在页面子树断言泛型即可;错配时 item 会是 MudSelectItem<int> 而非 <int?>。
        var providers = ctx.Render<MultiClusterMgmtSys.Web.Components.Compare.Pages.Compare>();

        // 两个集群 Select 各渲染一份 shadow 副本:2 Select × 2 集群 = 4
        Assert.Equal(4, providers.FindComponents<MudSelectItem<int?>>().Count);
        Assert.Equal(5, providers.FindComponents<MudSelectItem<CompareKind?>>().Count);
    }
}
