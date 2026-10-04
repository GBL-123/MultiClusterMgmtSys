using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class EditSecretYamlPageTests
{
    private static void AuthorizeAdmin(BunitHost ctx)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
    }

    private static void SetupProbe(Mock<k8s.IKubernetes> k8s)
    {
        k8s.SetupListNodes(new V1Node
        {
            Metadata = new V1ObjectMeta { Name = "n1" },
            Status = new V1NodeStatus
            {
                Conditions = [new V1NodeCondition { Type = "Ready", Status = "True" }]
            }
        });
        k8s.SetupGetVersion("v1.30.2");
    }

    private static V1Secret NewSecret(string name, string ns, string token = "old-pass")
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
            Type = "Opaque",
            Data = new Dictionary<string, byte[]> { ["token"] = Encoding.UTF8.GetBytes(token) }
        };

    private static async Task<int> SeedAsync(ServiceHarness harness, string name)
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    [Fact]
    public async Task Placeholder_yaml_saves_with_merge_backfill()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddSecretStack();
        var clusterId = await SeedAsync(harness, "secret-edit-src");
        SetupProbe(k8s);

        k8s.SetupReadSecret("app-secret", "app", NewSecret("app-secret", "app"));
        V1Secret? replaced = null;
        k8s.SetupReplaceSecret("app-secret", "app", s => replaced = s);

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.EditSecretYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-secret"));

        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"), TimeSpan.FromSeconds(10));

        // 服务器明文不回显,仅出现占位符(bUnit 属性值不转义,占位符为原文)
        Assert.Contains("<REDACTED:token>", cut.Markup);
        Assert.DoesNotContain("b2xkLXBhc3M=", cut.Markup);

        var save = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("保存"));
        await cut.InvokeAsync(async () => await save.Instance.OnClick.InvokeAsync());

        cut.WaitForState(
            () => harness.Db.AuditLogs.Any(a => a.Action == Domain.Enums.AuditAction.Update),
            TimeSpan.FromSeconds(10));

        Assert.NotNull(replaced);
        Assert.Equal("old-pass", Encoding.UTF8.GetString(replaced!.Data!["token"]));
        Assert.Null(replaced.StringData);
    }

    [Fact]
    public async Task Missing_secret_shows_error_state()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddSecretStack();
        var clusterId = await SeedAsync(harness, "secret-edit-miss");
        SetupProbe(k8s);

        k8s.SetupReadSecretThrows("app-secret", "app", K8sMocks.K8sError(404));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.EditSecretYaml>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-secret"));

        cut.WaitForState(() => cut.Markup.Contains("Secret 不存在或已被删除"), TimeSpan.FromSeconds(10));
    }
}
