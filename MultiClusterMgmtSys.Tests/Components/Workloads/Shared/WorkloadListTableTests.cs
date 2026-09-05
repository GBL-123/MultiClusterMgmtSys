using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Workloads.Shared;
using MultiClusterMgmtSys.ViewModels;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Components.Workloads.Shared;

/// <summary>
/// 接线契约:WorkloadListTable 渲染统一行模型,操作列按能力矩阵条件渲染。
/// 断言只针对自有语义(status-badge/事件参数),不碰 .mud-* 内部 DOM。
/// </summary>
public class WorkloadListTableTests : TestContext
{
    public WorkloadListTableTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.AddTestAuthorization();
    }

    private void AuthorizeAsAdmin()
    {
        var auth = this.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles(["Admin"]);
    }

    [Fact]
    public void RendersReadyBadgeAndReadyText()
    {
        var item = new WorkloadListViewModel
        {
            Name = "app",
            Namespace = "default",
            Kind = WorkloadKind.Deployment,
            DesiredCount = 2,
            ReadyCount = 2,
            RolloutState = WorkloadRolloutState.Ready
        };

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.Deployment)
            .Add(x => x.Items, new List<WorkloadListViewModel> { item }));

        var badge = table.Find(".status-badge");
        Assert.Contains("online", badge.ClassList);
        Assert.Contains("就绪", badge.TextContent);
        Assert.Contains("2/2", table.FindAll(".font-mono").Select(e => e.TextContent).ToList());
    }

    [Theory]
    [InlineData(WorkloadRolloutState.Ready, "online")]
    [InlineData(WorkloadRolloutState.Rolling, "unknown")]
    [InlineData(WorkloadRolloutState.NotReady, "offline")]
    public void RolloutState_MapsToBadgeClass(WorkloadRolloutState state, string expectedClass)
    {
        var item = new WorkloadListViewModel
        {
            Name = "app",
            Namespace = "default",
            Kind = WorkloadKind.Deployment,
            DesiredCount = 4,
            ReadyCount = 2,
            RolloutState = state
        };

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.Deployment)
            .Add(x => x.Items, new List<WorkloadListViewModel> { item }));

        var badge = table.Find(".status-badge");
        Assert.Contains(expectedClass, badge.ClassList);
    }

    [Fact]
    public void NameClick_InvokesNavigateDetailWithTuple()
    {
        (string Namespace, string Name) received = default;
        var item = new WorkloadListViewModel
        {
            Name = "app",
            Namespace = "default",
            Kind = WorkloadKind.Deployment,
            DesiredCount = 1,
            ReadyCount = 1,
            RolloutState = WorkloadRolloutState.Ready
        };

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.Deployment)
            .Add(x => x.Items, new List<WorkloadListViewModel> { item })
            .Add(x => x.OnNavigateDetail, args => received = args));

        table.Find(".link-primary").Click();

        Assert.Equal(("default", "app"), received);
    }

    [Fact]
    public void DeploymentRow_HasScaleAndRestartButtons()
    {
        AuthorizeAsAdmin();
        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.Deployment)
            .Add(x => x.Items, new List<WorkloadListViewModel> { DeploymentItem() }));

        var icons = table.FindComponents<MudIconButton>().Select(b => b.Instance.Icon).ToList();

        Assert.Contains(Icons.Material.Filled.OpenInFull, icons);
        Assert.Contains(Icons.Material.Filled.Autorenew, icons);
    }

    [Fact]
    public void DaemonSetRow_HasNoScaleButtonButHasRestart()
    {
        AuthorizeAsAdmin();
        var item = new WorkloadListViewModel
        {
            Name = "agent",
            Namespace = "kube-system",
            Kind = WorkloadKind.DaemonSet,
            DesiredCount = 3,
            ReadyCount = 3,
            RolloutState = WorkloadRolloutState.Ready
        };

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.DaemonSet)
            .Add(x => x.Items, new List<WorkloadListViewModel> { item }));

        var icons = table.FindComponents<MudIconButton>().Select(b => b.Instance.Icon).ToList();

        Assert.DoesNotContain(Icons.Material.Filled.OpenInFull, icons);
        Assert.Contains(Icons.Material.Filled.Autorenew, icons);
    }

    [Fact]
    public void ReplicaSetRow_HasScaleButNoRestart()
    {
        AuthorizeAsAdmin();
        var item = new WorkloadListViewModel
        {
            Name = "app-rs",
            Namespace = "default",
            Kind = WorkloadKind.ReplicaSet,
            DesiredCount = 2,
            ReadyCount = 2,
            RolloutState = WorkloadRolloutState.Ready
        };

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.ReplicaSet)
            .Add(x => x.Items, new List<WorkloadListViewModel> { item }));

        var icons = table.FindComponents<MudIconButton>().Select(b => b.Instance.Icon).ToList();

        Assert.Contains(Icons.Material.Filled.OpenInFull, icons);
        Assert.DoesNotContain(Icons.Material.Filled.Autorenew, icons);
    }

    [Fact]
    public void EmptyItems_ShowsKindAwareEmptyState()
    {
        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.StatefulSet)
            .Add(x => x.Items, new List<WorkloadListViewModel>()));

        Assert.Contains("[ 暂无有状态应用 ]", table.Markup);
    }

    [Fact]
    public void ScaleClick_InvokesScaleEventWithTuple()
    {
        AuthorizeAsAdmin();
        (string Namespace, string Name) received = default;

        var table = RenderComponent<WorkloadListTable>(p => p
            .Add(x => x.Kind, WorkloadKind.Deployment)
            .Add(x => x.Items, new List<WorkloadListViewModel> { DeploymentItem() })
            .Add(x => x.OnScale, args => received = args));

        var scaleButton = table.FindComponents<MudIconButton>()
            .Single(b => b.Instance.Icon == Icons.Material.Filled.OpenInFull);
        table.InvokeAsync(() => scaleButton.Instance.OnClick.InvokeAsync());

        Assert.Equal(("default", "app"), received);
    }

    private static WorkloadListViewModel DeploymentItem() => new()
    {
        Name = "app",
        Namespace = "default",
        Kind = WorkloadKind.Deployment,
        DesiredCount = 1,
        ReadyCount = 1,
        RolloutState = WorkloadRolloutState.Ready
    };
}
