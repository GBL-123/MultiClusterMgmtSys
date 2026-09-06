using Bunit;
using Bunit.TestDoubles;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Nodes.Shared;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;
using NodeDetailPage = MultiClusterMgmtSys.Components.Nodes.Pages.NodeDetail;

namespace MultiClusterMgmtSys.Tests.Components.Nodes.Pages;

/// <summary>
/// 接线契约:节点详情页 tab 化——五面板、默认第一个、「标签与注解」共用同一 tab。
/// K8s 侧 mock ReadNodeWithHttpMessagesAsync 返回最小 V1Node。
/// </summary>
public class NodeDetailPageTests
{
    private static TestContext CreateContext()
    {
        var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("测试集群", ClusterStatus.Online));
        db.SaveChanges();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.ReadNodeWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Node>
            {
                Body = new V1Node
                {
                    Metadata = new V1ObjectMeta { Name = "node-a" },
                    Status = new V1NodeStatus
                    {
                        NodeInfo = new V1NodeSystemInfo(),
                        Conditions = new List<V1NodeCondition> { new() { Type = "Ready", Status = "True" } }
                    }
                }
            });

        var ctx = BunitHost.Create(db);
        ctx.Services.AddScoped<ClusterNodeService>(_ => TestServices.NodeService(db, _ => fake.Object));

        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles(["Member"]);
        return ctx;
    }

    [Fact]
    public void Tabs_DefaultShowsFirstOfFive()
    {
        using var ctx = CreateContext();

        var page = ctx.RenderComponent<NodeDetailPage>(p =>
        {
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.NodeName, "node-a");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        Assert.Equal(5, tabs.Instance.Panels.Count);
        Assert.Single(page.FindComponents<NodeOverviewCard>());
        Assert.Empty(page.FindComponents<NodeResourcesCard>());
        Assert.Empty(page.FindComponents<NodeConditionsCard>());
        Assert.Empty(page.FindComponents<NodeSystemInfoCard>());
    }

    [Fact]
    public async Task LabelsAndAnnotations_ShareOneTab()
    {
        using var ctx = CreateContext();

        var page = ctx.RenderComponent<NodeDetailPage>(p =>
        {
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.NodeName, "node-a");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        await page.InvokeAsync(() => tabs.Instance.ActivatePanelAsync(3));

        Assert.Single(page.FindComponents<NodeLabelsCard>());
        Assert.Single(page.FindComponents<NodeAnnotationsCard>());
        Assert.Empty(page.FindComponents<NodeOverviewCard>());
    }
}
