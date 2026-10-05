using Bunit;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MudBlazor;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Components.Compare;

public class CloneConfirmDialogTests
{
    private static readonly Microsoft.AspNetCore.Components.RendererInfo RendererInfo = new("bunit", true);

    private const string NamespaceName = "app";

    private const string ResourceName = "web";

    private static V1ConfigMap OwnedConfigMap(string color) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = ResourceName,
            NamespaceProperty = NamespaceName,
            Uid = "uid-" + color,
            ResourceVersion = "123",
            CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ManagedFields = [new V1ManagedFieldsEntry { Manager = "kubectl" }],
            Labels = new Dictionary<string, string>
            {
                [ResourceOwnershipKeys.OwnerUidLabel] = "7",
                ["app"] = "web"
            },
            Annotations = new Dictionary<string, string>
            {
                [ResourceOwnershipKeys.OwnerNameAnnotation] = "someone"
            }
        },
        Data = new Dictionary<string, string> { ["color"] = color }
    };

    private static async Task<(ServiceHarness Harness, Mock<IKubernetes> K8s, int SourceId, int TargetId)> WireAsync(BunitHost ctx)
    {
        var harness = ctx.AddClusterStack();
        var k8s = new Mock<IKubernetes>();
        ctx.Services.AddSingleton<Func<KubernetesClientConfiguration, IKubernetes>>(K8sMocks.Factory(k8s));
        ctx.AddClientCache();
        ctx.Services.AddScoped<WorkloadService>();
        ctx.Services.AddScoped<ConfigMapService>();
        ctx.Services.AddScoped<ClusterCompareService>();
        var auth = ctx.AddAuthorization();
        auth.SetAuthorized("admin");
        auth.SetRoles("Admin");
        ctx.Renderer.SetRendererInfo(RendererInfo);
        var source = await harness.ClusterRepo.AddAsync(TestData.NewCluster("alpha"));
        var target = await harness.ClusterRepo.AddAsync(TestData.NewCluster("beta"));
        return (harness, k8s, source.Id, target.Id);
    }

    private static async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> ShowAsync(
        BunitHost ctx,
        int sourceId,
        int targetId)
    {
        var pair = new ComparePairViewModel
        {
            Namespace = NamespaceName,
            Name = ResourceName,
            SourceClusterName = "alpha",
            TargetClusterName = "beta",
            SourceExists = true,
            TargetExists = false,
            CanClone = true
        };
        var query = new ComparePairQueryRequest(sourceId, targetId, CompareKind.ConfigMap, NamespaceName, ResourceName);
        var provider = ctx.Render<MudDialogProvider>();
        var reference = await ctx.Services.GetRequiredService<IDialogService>()
            .ShowAsync<MultiClusterMgmtSys.Web.Components.Compare.Shared.CloneConfirmDialog>(
                $"克隆到 {pair.TargetClusterName}",
                new DialogParameters
                {
                    { "Pair", pair },
                    { "Query", query }
                });
        return (provider, reference);
    }

    [Fact]
    public async Task Preview_strips_server_fields_and_keeps_data()
    {
        await using var ctx = new BunitHost();
        var (_, k8s, sourceId, targetId) = await WireAsync(ctx);
        k8s.SetupReadConfigMap(ResourceName, NamespaceName, OwnedConfigMap("blue"));

        var (provider, _) = await ShowAsync(ctx, sourceId, targetId);

        provider.WaitForState(() => provider.Markup.Contains("剥离服务端字段后的 YAML"), TimeSpan.FromSeconds(5));
        var markup = provider.Markup;

        Assert.Multiple(
            () => Assert.Contains("blue", markup),
            () => Assert.DoesNotContain("uid-blue", markup),
            () => Assert.DoesNotContain("resourceVersion", markup),
            () => Assert.DoesNotContain("managedFields", markup),
            () => Assert.DoesNotContain(ResourceOwnershipKeys.OwnerUidLabel, markup),
            () => Assert.DoesNotContain(ResourceOwnershipKeys.OwnerNameAnnotation, markup),
            () => Assert.Contains("确认克隆", markup));
    }

    [Fact]
    public async Task Confirm_invokes_clone_and_closes_with_ok()
    {
        await using var ctx = new BunitHost();
        var (_, k8s, sourceId, targetId) = await WireAsync(ctx);
        k8s.SetupReadConfigMap(ResourceName, NamespaceName, OwnedConfigMap("blue"));
        V1ConfigMap? created = null;
        k8s.Setup(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == NamespaceName),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<V1ConfigMap, string, string?, string?, string?, bool?, IReadOnlyDictionary<string, IReadOnlyList<string>>, CancellationToken>(
                (body, _, _, _, _, _, _, _) => created = body)
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap> { Body = new V1ConfigMap() });

        var (provider, reference) = await ShowAsync(ctx, sourceId, targetId);
        provider.WaitForState(() => provider.Markup.Contains("确认克隆"), TimeSpan.FromSeconds(5));
        var button = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("确认克隆"));

        await provider.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());

        var result = await reference.Result;
        var createdBody = created;
        Assert.Multiple(
            () => Assert.False(result.Canceled),
            () => Assert.Equal(true, result.Data),
            () => Assert.NotNull(createdBody),
            () => Assert.Equal(ResourceName, createdBody!.Metadata!.Name),
            () => Assert.Equal(NamespaceName, createdBody.Metadata.NamespaceProperty),
            () => Assert.Equal("blue", createdBody.Data!["color"]),
            () => Assert.Null(createdBody.Metadata!.Uid),
            () => Assert.Null(createdBody.Metadata.ResourceVersion));
    }

    [Fact]
    public async Task Cancel_does_not_clone()
    {
        await using var ctx = new BunitHost();
        var (_, k8s, sourceId, targetId) = await WireAsync(ctx);
        k8s.SetupReadConfigMap(ResourceName, NamespaceName, OwnedConfigMap("blue"));
        var cloneCalled = false;
        k8s.Setup(x => x.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(),
                It.Is<string>(n => n == NamespaceName),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => cloneCalled = true)
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap>());

        var (provider, reference) = await ShowAsync(ctx, sourceId, targetId);
        provider.WaitForState(() => provider.Markup.Contains("取消"), TimeSpan.FromSeconds(5));
        var button = provider.FindComponents<MudButton>().First(b => b.Instance.ChildContent is not null && b.Markup.Contains("取消"));

        await provider.InvokeAsync(() => button.Instance.OnClick.InvokeAsync());

        var result = await reference.Result;
        Assert.True(result.Canceled);
        Assert.False(cloneCalled);
    }

    [Fact]
    public async Task Preview_failure_disables_confirm()
    {
        await using var ctx = new BunitHost();
        var (_, k8s, sourceId, targetId) = await WireAsync(ctx);
        k8s.SetupReadConfigMapThrows(ResourceName, NamespaceName, K8sMocks.K8sError(404));

        var (provider, _) = await ShowAsync(ctx, sourceId, targetId);

        provider.WaitForState(() => provider.Markup.Contains("源资源不存在或已被删除"), TimeSpan.FromSeconds(5));
        var confirm = provider.FindComponents<MudButton>().First(b => b.Markup.Contains("确认克隆"));
        Assert.True(confirm.Instance.Disabled);
    }
}
