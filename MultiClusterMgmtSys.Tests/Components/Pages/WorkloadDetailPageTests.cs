using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class WorkloadDetailPageTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static V1Deployment HealthyDeployment()
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = "web",
                NamespaceProperty = "app",
                Uid = "uid-detail",
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus
            {
                ReadyReplicas = 3,
                UpdatedReplicas = 3,
                ObservedGeneration = 1,
                Conditions = [new V1DeploymentCondition { Type = "Available", Status = "True" }]
            }
        };

    [Fact]
    public async Task Deployment_detail_page_shows_yaml_default_and_status_cards()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("detail-ok"));

        k8s.SetupReadDeployment("web", "app", new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app", Uid = "uid-detail" },
            Spec = new V1DeploymentSpec { Replicas = 2 },
            Status = new V1DeploymentStatus
            {
                ReadyReplicas = 2,
                UpdatedReplicas = 2,
                ObservedGeneration = 1,
                Conditions = [new V1DeploymentCondition { Type = "Available", Status = "True" }]
            }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Workloads.Pages.DeploymentDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.FindComponents<MudTabPanel>().Count == 2);

        var tabs = cut.FindComponents<MudTabPanel>();
        Assert.Equal("YAML", tabs[0].Instance.Text);
        Assert.Equal("运行状态", tabs[1].Instance.Text);
        Assert.Equal(0, cut.FindComponent<MudTabs>().Instance.ActivePanelIndex);
        Assert.Single(cut.FindComponents<MultiClusterMgmtSys.Web.Components.Workloads.Shared.WorkloadYamlViewCard>());

        cut.FindAll(".mud-tab")[1].Click();
        cut.WaitForState(() =>
            cut.FindComponents<MultiClusterMgmtSys.Web.Components.Workloads.Shared.WorkloadConditionsCard>().Count == 1);

        Assert.Single(cut.FindComponents<MultiClusterMgmtSys.Web.Components.Workloads.Shared.WorkloadStatusCard>());
        Assert.Single(cut.FindComponents<MultiClusterMgmtSys.Web.Components.Workloads.Shared.WorkloadConditionsCard>());
        var statusIndex = cut.Markup.IndexOf(">运行状态<", StringComparison.Ordinal);
        var conditionsIndex = cut.Markup.IndexOf(">条件<", StringComparison.Ordinal);
        Assert.True(statusIndex >= 0 && conditionsIndex > statusIndex);
        Assert.Contains("uid-detail", cut.Markup);
        Assert.Contains("副本数", cut.Markup);
        Assert.Contains("Available", cut.Markup);
    }

    [Fact]
    public async Task Yaml_edit_page_loads_yaml_and_shows_editor()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("yaml-src"));

        k8s.SetupReadDeployment("web", "app", new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "app" },
            Spec = new V1DeploymentSpec { Replicas = 2 }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Workloads.Pages.DeploymentYamlEdit>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "web"));

        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"));

        Assert.Contains("编辑 YAML", cut.Markup);
        Assert.Contains("保存", cut.Markup);
    }

    [Fact]
    public async Task Yaml_edit_page_missing_workload_shows_error_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var harness = ctx.AddClusterStack();
        ctx.AddGroupAndSyncStack(harness);
        var k8s = new Mock<k8s.IKubernetes>();
        ctx.AddWorkloadServices(k8s, harness);
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("yaml-missing"));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Workloads.Pages.DeploymentYamlEdit>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "ghost"));

        cut.WaitForState(() => cut.Markup.Contains("不存在或已被删除"));
    }

    [Fact]
    public async Task Yaml_edit_card_binds_two_way()
    {
        await using var ctx = new BunitHost();

        string? changed = null;
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Workloads.Shared.WorkloadYamlEditCard>(
            parameters => parameters
                .Add(p => p.Yaml, "a: 1")
                .Add(p => p.YamlChanged, v => { changed = v; return Task.CompletedTask; }));

        var textarea = cut.Find(".yaml-textarea");
        await textarea.InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "a: 2" });

        Assert.Equal("a: 2", changed);
    }
}
