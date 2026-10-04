using Bunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using k8s;
using k8s.Models;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Pages;

public class StorageClaimsPageFlowTests
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

    private static V1PersistentVolumeClaim NewClaim(
        string name,
        string ns,
        string phase = "Bound",
        string? owner = null)
        => new()
        {
            ApiVersion = "v1",
            Kind = "PersistentVolumeClaim",
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Labels = owner is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = owner }
            },
            Spec = new V1PersistentVolumeClaimSpec
            {
                StorageClassName = "standard",
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new("5Gi") }
                }
            },
            Status = new V1PersistentVolumeClaimStatus { Phase = phase }
        };

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, IRenderedComponent<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageClaims> Cut)>
        RenderListAsync(bool offline = false)
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddStorageStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster(
            "claim-flow",
            status: offline ? ClusterStatus.Offline : ClusterStatus.Online));
        SetupProbe(k8s);
        if (!offline)
        {
            k8s.SetupListNamespaces("app");
            k8s.SetupListClaims(NewClaim("app-claim", "app"));
        }

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageClaims>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(
            () => cut.Markup.Contains("app-claim") || cut.Markup.Contains("集群不可达"),
            TimeSpan.FromSeconds(10));
        return (ctx, harness, k8s, cut);
    }

    [Fact]
    public async Task List_renders_claim_rows()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            Assert.Contains("app-claim", cut.Markup);
            Assert.Contains("app", cut.Markup);
            Assert.Contains("5Gi", cut.Markup);
            Assert.Contains("2026-01-01", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Empty_state_shown_without_claims()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddStorageStack();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("claim-empty"));
            SetupProbe(k8s);
            k8s.SetupListNamespaces("app");
            k8s.SetupListClaims();

            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageClaims>(
                parameters => parameters.Add(p => p.ClusterId, cluster.Id));

            cut.WaitForState(() => cut.Markup.Contains("暂无持久卷声明"), TimeSpan.FromSeconds(10));
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
            Assert.Contains("集群不可达，无法获取持久卷声明", cut.Markup);
            Assert.DoesNotContain("app-claim", cut.Markup);

            var create = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建声明"));
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

            k8s.SetupReadClaim("app-claim", "app", NewClaim("app-claim", "app", owner: "99"));
            k8s.SetupDeleteClaim("app-claim", "app");

            var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
            confirm.Find("button").Click();

            for (var i = 0; i < 10; i++)
            {
                await provider.InvokeAsync(() => { });
                if (harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete)) break;
            }

            Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Storage && a.Action == AuditAction.Delete));
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

            var createButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("新建声明"));
            createButton.Find("button").Click();

            provider.WaitForState(() => provider.Markup.Contains("mud-dialog-content"), TimeSpan.FromSeconds(5));
            provider.WaitForState(() => provider.Markup.Contains("kind: PersistentVolumeClaim"), TimeSpan.FromSeconds(5));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}

public class StorageVolumesPageFlowTests
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

    private static async Task<(BunitHost Ctx, ServiceHarness Harness, Mock<k8s.IKubernetes> K8s, IRenderedComponent<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageVolumes> Cut)>
        RenderListAsync()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddStorageStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("volume-flow"));
        SetupProbe(k8s);
        k8s.SetupListVolumes(new V1PersistentVolume
        {
            ApiVersion = "v1",
            Kind = "PersistentVolume",
            Metadata = new V1ObjectMeta
            {
                Name = "pv-1",
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Spec = new V1PersistentVolumeSpec
            {
                PersistentVolumeReclaimPolicy = "Retain",
                StorageClassName = "slow-storage",
                Capacity = new Dictionary<string, ResourceQuantity> { ["storage"] = new("10Gi") },
                HostPath = new V1HostPathVolumeSource { Path = "/data/pv-1" }
            },
            Status = new V1PersistentVolumeStatus { Phase = "Available" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageVolumes>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));
        cut.WaitForState(
            () => cut.Markup.Contains("pv-1") || cut.Markup.Contains("集群不可达"),
            TimeSpan.FromSeconds(10));
        return (ctx, harness, k8s, cut);
    }

    [Fact]
    public async Task List_renders_volume_rows_without_mutation_buttons()
    {
        var (ctx, harness, k8s, cut) = await RenderListAsync();
        try
        {
            Assert.Contains("pv-1", cut.Markup);
            Assert.Contains("10Gi", cut.Markup);
            Assert.Contains("Retain", cut.Markup);
            Assert.Contains("slow-storage", cut.Markup);
            Assert.DoesNotContain("删除", cut.Markup);
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }

    [Fact]
    public async Task Empty_state_shown_without_volumes()
    {
        var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddStorageStack();
        try
        {
            var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("volume-empty"));
            SetupProbe(k8s);
            k8s.SetupListVolumes();

            var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageVolumes>(
                parameters => parameters.Add(p => p.ClusterId, cluster.Id));

            cut.WaitForState(() => cut.Markup.Contains("暂无持久卷"), TimeSpan.FromSeconds(10));
        }
        finally
        {
            await ctx.DisposeAsync();
        }
    }
}

public class StorageClassesPageFlowTests
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

    [Fact]
    public async Task List_renders_class_rows()
    {
        await using var ctx = new BunitHost();
        AuthorizeAdmin(ctx);
        var (harness, k8s) = ctx.AddStorageStack();
        var cluster = await harness.ClusterRepo.AddAsync(TestData.NewCluster("class-flow"));
        SetupProbe(k8s);
        k8s.SetupListStorageClasses(new V1StorageClass
        {
            ApiVersion = "storage.k8s.io/v1",
            Kind = "StorageClass",
            Metadata = new V1ObjectMeta
            {
                Name = "slow-storage",
                CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Provisioner = "kubernetes.io/no-provisioner",
            ReclaimPolicy = "Delete",
            VolumeBindingMode = "Immediate",
            AllowVolumeExpansion = true
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.StorageClasses>(
            parameters => parameters.Add(p => p.ClusterId, cluster.Id));

        cut.WaitForState(() => cut.Markup.Contains("slow-storage"), TimeSpan.FromSeconds(10));
        Assert.Contains("kubernetes.io/no-provisioner", cut.Markup);
        Assert.Contains("Immediate", cut.Markup);
        Assert.DoesNotContain("删除", cut.Markup);
    }
}

public class StorageDetailPageFlowTests
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

    private static V1PersistentVolumeClaim NewClaim(string name, string ns, string? owner = null)
        => new()
        {
            ApiVersion = "v1",
            Kind = "PersistentVolumeClaim",
            Metadata = new V1ObjectMeta
            {
                Name = name,
                NamespaceProperty = ns,
                Labels = owner is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string> { [ResourceOwnershipKeys.OwnerUidLabel] = owner }
            },
            Spec = new V1PersistentVolumeClaimSpec
            {
                StorageClassName = "standard",
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new("5Gi") }
                }
            },
            Status = new V1PersistentVolumeClaimStatus { Phase = "Bound" }
        };

    private static V1Pod NewMountedPod(string name)
        => new()
        {
            Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = "app" },
            Spec = new V1PodSpec
            {
                Volumes =
                [
                    new V1Volume
                    {
                        Name = "data",
                        PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = "app-claim" }
                    }
                ]
            },
            Status = new V1PodStatus { Phase = "Running", StartTime = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc) }
        };

    [Fact]
    public async Task Claim_detail_shows_overview_mounted_pods_and_yaml()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddStorageStack();
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("claim-detail"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadClaim("app-claim", "app", NewClaim("app-claim", "app", owner: "99"));
        k8s.SetupListNamespacedPods("app", NewMountedPod("web-1"));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.ClaimDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-claim"));
        cut.WaitForState(() => cut.Markup.Contains("app-claim"), TimeSpan.FromSeconds(10));

        Assert.Contains("web-1", cut.Markup);
        Assert.Contains("5Gi", cut.Markup);
        Assert.Contains("删除", cut.Markup);
        Assert.DoesNotContain("yaml-textarea", cut.Markup);

        cut.FindAll(".mud-tab")[1].Click();
        cut.WaitForState(() => cut.Markup.Contains("yaml-textarea"), TimeSpan.FromSeconds(10));
        Assert.Contains("kind: PersistentVolumeClaim", cut.Markup);
    }

    [Fact]
    public async Task Claim_detail_delete_via_toolbar_audits_delete()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddStorageStack();
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("claim-detail-del"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadClaim("app-claim", "app", NewClaim("app-claim", "app", owner: "99"));
        k8s.SetupListNamespacedPods("app");

        var provider = ctx.Render<MudDialogProvider>();
        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.ClaimDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-claim"));
        cut.WaitForState(() => cut.Markup.Contains("app-claim"), TimeSpan.FromSeconds(10));

        var deleteButton = cut.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        deleteButton.Find("button").Click();
        await provider.InvokeAsync(() => { });

        provider.WaitForState(() => provider.Markup.Contains("确认删除"), TimeSpan.FromSeconds(5));

        k8s.SetupDeleteClaim("app-claim", "app");

        var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("删除"));
        confirm.Find("button").Click();

        for (var i = 0; i < 10; i++)
        {
            await provider.InvokeAsync(() => { });
            if (harness.Db.AuditLogs.Any(a => a.Action == AuditAction.Delete)) break;
        }

        Assert.True(harness.Db.AuditLogs.Any(a => a.Category == AuditCategory.Storage && a.Action == AuditAction.Delete));
    }

    [Fact]
    public async Task Unowned_member_sees_deleteless_toolbar()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "member");
        var (harness, k8s) = ctx.AddStorageStack("member");
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("claim-detail-unowned"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadClaim("app-claim", "app", NewClaim("app-claim", "app", owner: "99"));
        k8s.SetupListNamespacedPods("app");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.ClaimDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-claim"));
        cut.WaitForState(() => cut.Markup.Contains("app-claim"), TimeSpan.FromSeconds(10));

        Assert.DoesNotContain("删除", cut.Markup);
        Assert.Contains("5Gi", cut.Markup);
    }

    [Fact]
    public async Task Missing_claim_shows_not_found_state()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddStorageStack();
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("claim-detail-missing"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadClaimThrows("app-claim", "app", K8sMocks.K8sError(404));
        k8s.SetupListNamespacedPods("app");

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.ClaimDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Namespace, "app")
                .Add(p => p.Name, "app-claim"));

        cut.WaitForState(() => cut.Markup.Contains("持久卷声明不存在或已被删除"), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Volume_detail_shows_fields_and_yaml_without_actions()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddStorageStack();
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("volume-detail"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadVolume("pv-1", new V1PersistentVolume
        {
            ApiVersion = "v1",
            Kind = "PersistentVolume",
            Metadata = new V1ObjectMeta { Name = "pv-1" },
            Spec = new V1PersistentVolumeSpec
            {
                PersistentVolumeReclaimPolicy = "Retain",
                StorageClassName = "slow-storage",
                Capacity = new Dictionary<string, ResourceQuantity> { ["storage"] = new("10Gi") },
                HostPath = new V1HostPathVolumeSource { Path = "/data/pv-1" }
            },
            Status = new V1PersistentVolumeStatus { Phase = "Available" }
        });

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.VolumeDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Name, "pv-1"));
        cut.WaitForState(() => cut.Markup.Contains("pv-1"), TimeSpan.FromSeconds(10));

        Assert.Contains("10Gi", cut.Markup);
        Assert.Contains("Retain", cut.Markup);
        Assert.Contains("HostPath:/data/pv-1", cut.Markup);
        Assert.Contains("yaml-textarea", cut.Markup);
        Assert.Contains("kind: PersistentVolume", cut.Markup);
        Assert.DoesNotContain("删除", cut.Markup);
    }

    [Fact]
    public async Task Missing_volume_shows_not_found_state()
    {
        await using var ctx = new BunitHost();
        Authorize(ctx, "admin");
        var (harness, k8s) = ctx.AddStorageStack();
        var clusterId = (await harness.ClusterRepo.AddAsync(TestData.NewCluster("volume-detail-missing"))).Id;
        SetupProbe(k8s);
        k8s.SetupReadVolumeThrows("pv-1", K8sMocks.K8sError(404));

        var cut = ctx.Render<MultiClusterMgmtSys.Web.Components.Storage.Pages.VolumeDetail>(
            parameters => parameters
                .Add(p => p.ClusterId, clusterId)
                .Add(p => p.Name, "pv-1"));

        cut.WaitForState(() => cut.Markup.Contains("未找到该持久卷"), TimeSpan.FromSeconds(10));
    }
}
