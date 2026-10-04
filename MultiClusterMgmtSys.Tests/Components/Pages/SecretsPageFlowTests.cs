using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class SecretsPageFlowTests
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

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, IRenderedComponent<MultiClusterMgmtSys.Web.Components.Secrets.Pages.Secrets> Cut)>
        RenderListAsync(bool offline = false)
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddSecretStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster(
            "secret-flow",
            status: offline ? ClusterStatus.Offline : ClusterStatus.Online));
        SetupProbe(k8s);
        if (!offline)
        {
            k8s.SetupListNamespaces("app");
            k8s.SetupListSecrets(new V1Secret
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "app-secret",
                    NamespaceProperty = "app",
                    CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                Type = "Opaque",
                Data = new Dictionary<string, byte[]> { ["k"] = Encoding.UTF8.GetBytes("v") }
            });
        }

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.Secrets>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(
            () => cut.Markup.Contains("app-secret") || cut.Markup.Contains("集群不可达"),
            TimeSpan.FromSeconds(10));
        return (ctx, harness, k8s, cut);
    }

    [Fact]
    public async Task List_renders_secret_rows()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            Assert.Contains("app-secret", cut.Markup);
            Assert.Contains("app", cut.Markup);
            Assert.Contains("Opaque", cut.Markup);
            Assert.Contains("2026-01-01", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Empty_state_shown_without_secrets()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddSecretStack();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("secret-empty"));
            SetupProbe(k8s);
            k8s.SetupListNamespaces("app");
            k8s.SetupListSecrets();

            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.Secrets>(
                parameters => parameters.Add(p => p.ClusterId, cluster.Id));

            cut.WaitForState(() => cut.Markup.Contains("暂无密钥"), TimeSpan.FromSeconds(10));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Offline_cluster_disables_create_and_shows_unreachable_state()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync(offline: true);
        try
        {
            Assert.Contains("集群不可达，无法获取密钥", cut.Markup);
            Assert.DoesNotContain("app-secret", cut.Markup);

            var create = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建密钥"));
            Assert.True(create.Instance.Disabled);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Delete_via_tooltip_flow_audits_delete()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var deleteTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "删除");
            deleteTooltip.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

            k8s.SetupReadSecret("app-secret", "app", new V1Secret
            {
                Metadata = new V1ObjectMeta { Name = "app-secret", NamespaceProperty = "app" },
                Type = "Opaque",
                Data = new Dictionary<string, byte[]> { ["k"] = Encoding.UTF8.GetBytes("v") }
            });
            k8s.SetupDeleteSecret("app-secret", "app");

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                if (harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete)) break;
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Secret && a.Action == AuditAction.Delete));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Create_dialog_opens_with_template()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            var provider = ctx.Render<MudDialogProvider>();

            var createButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建密钥"));
            createButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));
            provider.WaitForState(() => provider.Markup.Contains("kind: Secret"), TimeSpan.FromSeconds(5));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}

public class SecretDetailPageFlowTests
{
    private static void Authorize(BunitHost ctx, string actor)
    {
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized(actor);
        if (actor == "admin")
        {
            auth.SetRoles("Admin");
        }
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

    private static V1Secret NewSecret(string name, string ns, string? owner = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = owner }
            },
            Type = "Opaque",
            Data = new Dictionary<string, byte[]>
            {
                ["token"] = Encoding.UTF8.GetBytes("old-pass"),
                ["blob"] = [0xFF, 0xFE, 0x00, 0xC3, 0x28]
            }
        };

    private static async Task<int> SeedAsync(ServiceHarness harness, string name)
        => (await harness.ClusterRepo.AddAsync(TestData.NewCluster(name))).Id;

    private async Task<IRenderedComponent<MultiClusterMgmtSys.Web.Components.Secrets.Pages.SecretDetail>> RenderDetailAsync(
        BunitHost ctx, int clusterId, string actor = "admin")
    {
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.SecretDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-secret"));
        cut.WaitForState(
            () => cut.Markup.Contains("app-secret") || cut.Markup.Contains("Secret 不存在或已被删除"),
            TimeSpan.FromSeconds(10));
        return cut;
    }

    [Fact]
    public async Task Restricted_member_sees_content_banner_in_both_tabs()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "member");
        var (harness, k8s) = ctx.AddSecretStack("member");
        var clusterId = await SeedAsync(harness, "secret-restricted");
        SetupProbe(k8s);
        k8s.SetupReadSecret("app-secret", "app", NewSecret("app-secret", "app"));

        var cut = await RenderDetailAsync(ctx, clusterId, "member");

        // YAML tab(默认激活)
        Assert.Contains("无权查看该 Secret 的内容", cut.Markup);
        Assert.DoesNotContain("查看明文", cut.Markup);
        Assert.DoesNotContain("secret-masked-value", cut.Markup);
        Assert.DoesNotContain("编辑 YAML", cut.Markup);

        // 键值 tab(切换后同样受限)
        cut.FindAll(".mud-tab")[1].Click();
        cut.WaitForState(() => cut.Markup.Contains("无权查看该 Secret 的内容"), TimeSpan.FromSeconds(10));
        Assert.DoesNotContain("查看明文", cut.Markup);
    }

    [Fact]
    public async Task Owned_member_reveals_via_page_and_writes_audit()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "member");
        var (harness, k8s) = ctx.AddSecretStack("member");
        var clusterId = await SeedAsync(harness, "secret-reveal");
        SetupProbe(k8s);
        k8s.SetupReadSecret("app-secret", "app", NewSecret("app-secret", "app", owner: "7"));

        var cut = await RenderDetailAsync(ctx, clusterId, "member");

        cut.FindAll(".mud-tab")[1].Click();

        cut.WaitForState(() => cut.Markup.Contains("查看明文"), TimeSpan.FromSeconds(10));

        var revealTooltip = cut.FindComponents<MudTooltip>().First(t => t.Instance.Text == "查看明文");
        await cut.InvokeAsync(() => revealTooltip.Find("button").Click());

        cut.WaitForState(() => cut.Markup.Contains("old-pass"), TimeSpan.FromSeconds(10));
        Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Secret && a.Action == AuditAction.View));
    }

    [Fact]
    public async Task Missing_secret_shows_not_found_state()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddSecretStack();
        var clusterId = await SeedAsync(harness, "secret-missing");
        SetupProbe(k8s);
        k8s.SetupReadSecretThrows("app-secret", "app", K8sMocks.K8sError(404));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Secrets.Pages.SecretDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-secret"));

        cut.WaitForState(() => cut.Markup.Contains("Secret 不存在或已被删除"), TimeSpan.FromSeconds(10));
    }
}
