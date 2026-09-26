using Bunit;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using MultiClusterMgmtSys.Web.Components.Common;
using MultiClusterMgmtSys.Web.Components.Helm.Shared;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class HelmPageTests
{
    private static void AuthorizeAdmin(BunitContext ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static void SetupOnlineCluster(Mock<IKubernetes> k8s)
    {
        k8s.SetupGetVersion("v1.30.2");
        k8s.SetupListNodes(new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });
    }

    [Fact]
    public async Task Helm_page_shows_hint_when_no_cluster_selected()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        ctx.AddHelmStack();

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.Helm>();

        cut.WaitForState(() => cut.Markup.Contains("请从左侧选择一个集群"));
        Assert.Contains("请从左侧选择一个集群", cut.Markup);
    }

    [Fact]
    public async Task Helm_page_offline_cluster_shows_unreachable_card()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _, _) = ctx.AddHelmStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("offline-src", status: ClusterStatus.Offline));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.Helm>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("集群不可达"));
    }

    [Fact]
    public async Task Helm_page_renders_releases_with_admin_actions()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, runner, k8s) = ctx.AddHelmStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        SetupOnlineCluster(k8s);
        runner.Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.ReleaseListJson);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.Helm>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("nginx"));
        Assert.Contains("已部署", cut.Markup);
        Assert.Contains("已失败", cut.Markup);
        Assert.Contains("helm-table", cut.Markup);
        Assert.Equal(2, cut.FindComponents<TooltipIconButton>().Count(button => button.Instance.Text == "升级"));
    }

    [Fact]
    public async Task Helm_page_member_sees_actions_only_for_own_release()
    {
        await using var ctx = new BunitHost();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("alice");
        var (harness, runner, k8s) = ctx.AddHelmStack(actor: "alice", userId: 7);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        SetupOnlineCluster(k8s);
        await harness.OwnershipRepo.UpsertAsync(new HelmReleaseOwnership
        {
            ClusterId = cluster.Id,
            Namespace = "web",
            ReleaseName = "nginx",
            OwnerUserId = 7,
            OwnerUserName = "alice",
            InstalledAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
            InstalledRevision = 3
        });
        runner.Handler = _ => FakeHelmCliRunner.Succeeded(HelmFixtures.ReleaseListJson);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.Helm>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("nginx"));
        var buttons = cut.FindComponents<TooltipIconButton>();
        Assert.Single(buttons.Where(button => button.Instance.Text == "升级"));
        Assert.Single(buttons.Where(button => button.Instance.Text == "回滚"));
        Assert.Single(buttons.Where(button => button.Instance.Text == "卸载"));
    }

    [Fact]
    public async Task Drawer_contains_helm_nav_entry()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Layout.Drawer>(
            parameters => parameters.Add(p => p.IsOpen, true));

        Assert.Contains("应用管理", cut.Markup);
        Assert.Contains("/helm", cut.Markup);
    }

    [Fact]
    public async Task Install_dialog_renders_upload_and_fields()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, _, k8s) = ctx.AddHelmStack();
        k8s.SetupListNamespaces("web");
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        var provider = ctx.Render<MudDialogProvider>();
        var dialogService = ctx.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { { "ClusterId", cluster.Id } };

        await dialogService.ShowAsync<InstallHelmReleaseDialog>("安装 Chart 包", parameters);

        provider.WaitForState(() => provider.Markup.Contains("Release 名称"));
        Assert.Contains("命名空间", provider.Markup);
        Assert.Contains(".tgz", provider.Markup);
    }
}
