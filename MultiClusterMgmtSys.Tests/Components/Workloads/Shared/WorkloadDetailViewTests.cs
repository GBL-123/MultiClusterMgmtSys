using Bunit;
using Bunit.TestDoubles;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Components.Workloads.Shared;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Components.Workloads.Shared;

/// <summary>
/// 接线契约:工作负载详情页 tab 化——默认「运行状态」tab、双面板、
/// 工具栏动作(按可用性矩阵)仍在 tab 区之外。
/// </summary>
public class WorkloadDetailViewTests
{
    private static TestContext CreateContext(string role)
    {
        var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("测试集群", ClusterStatus.Online));
        db.SaveChanges();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.AppsV1.ReadNamespacedDeploymentWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HttpOperationResponse<V1Deployment>
            {
                Body = new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = "web", NamespaceProperty = "default" },
                    Spec = new V1DeploymentSpec { Replicas = 2 },
                    Status = new V1DeploymentStatus { ReadyReplicas = 2, UpdatedReplicas = 2, ObservedGeneration = 1 }
                }
            });

        var ctx = BunitHost.Create(db);
        ctx.Services.AddScoped<WorkloadService>(_ => TestServices.Workload(db, _ => fake.Object));

        var auth = ctx.AddTestAuthorization();
        auth.SetAuthorized("tester", new AuthorizationState());
        auth.SetRoles([role]);
        return ctx;
    }

    [Fact]
    public void Tabs_DefaultShowsStatusOfTwo()
    {
        using var ctx = CreateContext("Member");

        var page = ctx.RenderComponent<WorkloadDetailView>(p =>
        {
            p.Add(x => x.Kind, WorkloadKind.Deployment);
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.Namespace, "default");
            p.Add(x => x.Name, "web");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var tabs = page.FindComponent<MudTabs>();
        Assert.Equal(2, tabs.Instance.Panels.Count);
        Assert.Single(page.FindComponents<WorkloadStatusCard>());
        Assert.Empty(page.FindComponents<WorkloadYamlViewCard>());
    }

    [Fact]
    public void Admin_ActionsRenderBeforeTabArea()
    {
        using var ctx = CreateContext("Admin");

        var page = ctx.RenderComponent<WorkloadDetailView>(p =>
        {
            p.Add(x => x.Kind, WorkloadKind.Deployment);
            p.Add(x => x.ClusterId, 1);
            p.Add(x => x.Namespace, "default");
            p.Add(x => x.Name, "web");
        });
        page.WaitForState(() => page.FindComponents<MudTabs>().Count == 1);

        var markup = page.Markup;
        Assert.Single(page.FindComponents<WorkloadDetailToolbar>());
        Assert.Single(page.FindComponents<MudTabs>());
        Assert.Contains("扩缩容", markup);
        Assert.Contains("重启", markup);
        Assert.True(markup.IndexOf("扩缩容", StringComparison.Ordinal) < markup.IndexOf("detail-tabs", StringComparison.Ordinal));
    }
}
