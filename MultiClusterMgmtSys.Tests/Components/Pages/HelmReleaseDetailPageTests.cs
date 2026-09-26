using Bunit;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class HelmReleaseDetailPageTests
{
    private static void AuthorizeAdmin(BunitContext ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    [Fact]
    public async Task Detail_page_renders_overview_tabs_and_history()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, runner, _) = ctx.AddHelmStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        runner.Handler = RouteFixture;

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.HelmReleaseDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "web")
                .Add(p => p.Name, "nginx"));

        cut.WaitForState(() => cut.Markup.Contains("Upgrade complete"));
        Assert.Contains("概览", cut.Markup);
        Assert.Contains("Values", cut.Markup);
        Assert.Contains("Manifest", cut.Markup);
        Assert.Contains("历史", cut.Markup);
        Assert.Contains("POD_NAME", cut.Markup);
        Assert.Contains("1.2.3", cut.Markup);
    }

    [Fact]
    public async Task Detail_page_reloads_when_route_parameters_change()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, runner, _) = ctx.AddHelmStack();
        var clusterA = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cluster-a"));
        var clusterB = await harness.ClusterRepo.AddAsync(TestData.NewCluster("cluster-b"));
        runner.Handler = RouteFixture;

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.HelmReleaseDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterA.Id)
                .Add(p => p.Namespace, "web")
                .Add(p => p.Name, "nginx"));

        cut.WaitForState(() => cut.Markup.Contains("Upgrade complete"));
        var firstCallCount = runner.Invocations.Count;

        var rerendered = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.HelmReleaseDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterB.Id)
                .Add(p => p.Namespace, "web")
                .Add(p => p.Name, "nginx"));

        rerendered.WaitForState(() => rerendered.Markup.Contains("Upgrade complete"));
        Assert.True(runner.Invocations.Count > firstCallCount);
    }

    [Fact]
    public async Task Detail_page_renders_not_found_when_release_missing()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, runner, _) = ctx.AddHelmStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("k8s-src"));
        runner.Handler = _ => FakeHelmCliRunner.Failed(HelmFixtures.NotFoundError);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Helm.Pages.HelmReleaseDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, cluster.Id)
                .Add(p => p.Namespace, "web")
                .Add(p => p.Name, "missing"));

        cut.WaitForState(() => cut.Markup.Contains("Release 不存在或已被删除"));
    }

    private static readonly Func<HelmCliInvocation, HelmCliResult> RouteFixture = invocation => invocation.Arguments[0] switch
    {
        "status" => FakeHelmCliRunner.Succeeded(HelmFixtures.StatusJson),
        "history" => FakeHelmCliRunner.Succeeded(HelmFixtures.HistoryJson),
        "get" when invocation.Arguments.Count > 1 && invocation.Arguments[1] == "values"
            => FakeHelmCliRunner.Succeeded(HelmFixtures.UserValuesYaml),
        "get" => FakeHelmCliRunner.Succeeded(HelmFixtures.Manifest),
        _ => FakeHelmCliRunner.Succeeded()
    };
}
