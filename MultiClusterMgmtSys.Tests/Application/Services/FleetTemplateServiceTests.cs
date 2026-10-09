using k8s;
using k8s.Models;
using k8s.Autorest;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Application;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Yaml;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Services;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure.Kubernetes;
using MultiClusterMgmtSys.Infrastructure.Persistence;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Application.Services;

/// <summary>
/// 舰队模板服务单元测试:模板变量纯文本替�?/ 校验拒绝矩阵 / 预览四态判�?/ upsert 下发与舰队审�?契约�?fleet-templates)�?/// FleetTemplateService 逐集群从独立作用域解�?WorkloadService / ConfigMapService / AuditService,
/// 测试用真�?DI 容器(ServiceCollection + AddApplicationServices + 每作用域独立 SQLite 上下�?构建该作用域形态�?/// </summary>
public sealed class FleetTemplateServiceTests : IDisposable
{
    private static readonly string AlphaHost = "https://alpha:6443";

    private static readonly string BetaHost = "https://beta:6443";

    private static readonly string NamespaceName = "app";

    private static readonly string ResourceName = "cm-shared";

    private static readonly string ColorPlaceholder = "{{color}}";

    private static readonly string ReplicasPlaceholder = "{{replicas}}";

    private readonly Mock<IHttpContextAccessor> _accessor = TestHttpContext.ForIdentity("admin", 7, "Admin");

    private readonly Mock<IKubernetes> _alpha = K8sMocks.Create();

    private readonly Mock<IKubernetes> _beta = K8sMocks.Create();

    private readonly Mock<IKubernetes> _gamma = K8sMocks.Create();

    private ServiceProvider? _provider;

    private string? _dbPath;

    public void Dispose()
    {
        _provider?.Dispose();
        if (_dbPath is not null)
        {
            SqliteConnection.ClearPool(new SqliteConnection($"Data Source={_dbPath}"));
            foreach (var leftover in new[] { _dbPath, $"{_dbPath}-shm", $"{_dbPath}-wal" })
            {
                if (File.Exists(leftover))
                {
                    File.Delete(leftover);
                }
            }
        }
    }

    [Fact]
    public async Task Preview_and_deploy_denied_for_member()
    {
        await SeedClustersAsync("alpha");
        var member = TestHttpContext.ForIdentity("member", 9, "Member");
        var service = NewServiceWithAccessor(member.Object);

        var previewEx = await Assert.ThrowsAnyAsync<PermissionException>(
            () => service.PreviewAsync(PreviewRequest([1], ConfigMapTemplate(), ColorMatrix(1, "blue"))));
        var deployEx = await Assert.ThrowsAnyAsync<PermissionException>(
            () => service.DeployToFleetAsync(DeployRequest([1], ConfigMapTemplate(), ColorMatrix(1, "blue"))));

        Assert.Equal("仅管理员可以使用舰队模板下发", previewEx.UserMessage);
        Assert.Equal("仅管理员可以使用舰队模板下发", deployEx.UserMessage);
        Assert.Empty(_alpha.Invocations);
    }

    [Fact]
    public async Task Preview_rejects_empty_clusters_and_missing_variables_without_k8s_calls()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        var service = NewService();

        var emptyEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(new FleetTemplatePreviewRequest(ConfigMapTemplate(), [], ColorMatrix(alphaId, "blue"))));
        var missingEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest(
                [alphaId],
                ConfigMapTemplate(),
                new Dictionary<int, IReadOnlyDictionary<string, string>>())));

        Assert.Contains("至少一个目标集群", emptyEx.Message);
        Assert.Contains("集群 alpha 缺变量 color", missingEx.Message);
        Assert.Empty(_alpha.Invocations);
    }

    [Fact]
    public async Task Preview_rejects_missing_target_cluster()
    {
        var service = NewService();

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => service.PreviewAsync(PreviewRequest([42], ConfigMapTemplate(), ColorMatrix(42, "blue"))));

        Assert.Contains("目标集群不存在", ex.Message);
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public async Task Preview_rejects_syntax_error_and_residuals_and_unsupported_kind()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        var service = NewService();

        var syntaxEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], "a: [1,\n", ColorMatrix(alphaId, "blue"))));
        var residualEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], "a: {{if x}}", ColorMatrix(alphaId, "blue"))));
        var kindEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], IngressTemplate(), ColorMatrix(alphaId, "blue"))));
        var multiEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], ConfigMapTemplate() + "\n---\n" + ConfigMapTemplate(), ColorMatrix(alphaId, "blue"))));
        var emptyEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], "   \n", ColorMatrix(alphaId, "blue"))));
        var namelessEx = await Assert.ThrowsAsync<ValidationException>(
            () => service.PreviewAsync(PreviewRequest([alphaId], NamelessConfigMapTemplate(), ColorMatrix(alphaId, "blue"))));

        Assert.Contains("YAML 格式错误", syntaxEx.Message);
        Assert.Contains("无法解析的占位符", residualEx.Message);
        Assert.Contains("{{if x}}", residualEx.Message);
        Assert.Contains("五类资源", kindEx.Message);
        Assert.Contains("单文档", multiEx.Message);
        Assert.Contains("未包含任何文档", emptyEx.Message);
        Assert.Contains("metadata.name", namelessEx.Message);
        Assert.Contains("alpha", namelessEx.Message);
        Assert.Empty(_alpha.Invocations);
    }

    [Fact]
    public void Extract_returns_ordered_unique_variables_and_skips_structures()
    {
        var service = NewService();

        var names = service.ExtractVariables("a: {{replicas}}\nb: {{replicas}}\nc: {{image}}\nd: {{}}\ne: {{if x}}");

        Assert.Equal(["replicas", "image"], names);
    }

    [Fact]
    public void Variables_render_empty_values_and_keep_missing_ones()
    {
        Assert.Equal("a: 1", FleetTemplateVariables.Render("a: {{color}}", new Dictionary<string, string> { ["color"] = "1" }));
        Assert.Equal("a: ", FleetTemplateVariables.Render("a: {{color}}", new Dictionary<string, string> { ["color"] = "" }));
        Assert.Equal("a: {{color}}", FleetTemplateVariables.Render("a: {{color}}", new Dictionary<string, string>()));

        Assert.Equal(["{{if x}}"], FleetTemplateVariables.FindResiduals("a: {{if x}}\nb: ok"));
        Assert.Empty(FleetTemplateVariables.FindResiduals("a: ok"));
    }

    [Fact]
    public async Task Preview_marks_create_update_and_identical_with_sanitized_sides()
    {
        var (alphaId, betaId, gammaId) = await SeedClustersAsync("alpha", "beta", "gamma");
        _alpha.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _beta.SetupReadDeployment("web", NamespaceName, NoisyDeployment(replicas: 1));
        _gamma.SetupReadDeployment("web", NamespaceName, NoisyDeployment(replicas: 3));
        var service = NewService();

        var result = await service.PreviewAsync(
            PreviewRequest([alphaId, betaId, gammaId], DeploymentTemplate(), ReplicasMatrix(alphaId, 3, betaId, 3, gammaId, 3)));

        Assert.Multiple(
            () =>
            {
                Assert.Equal(FleetTemplateAction.Create, result.Items[0].Action);
                Assert.Equal(alphaId, result.Items[0].ClusterId);
                Assert.Equal("alpha", result.Items[0].ClusterName);
                Assert.Null(result.Items[0].CurrentYaml);
                Assert.Contains("replicas: 3", result.Items[0].RenderedYaml);
            },
            () =>
            {
                Assert.Equal(FleetTemplateAction.Update, result.Items[1].Action);
                Assert.NotNull(result.Items[1].CurrentYaml);
                Assert.DoesNotContain("uid:", result.Items[1].CurrentYaml);
            },
            () => Assert.Equal(FleetTemplateAction.Identical, result.Items[2].Action),
            () => Assert.All(result.Items, item => Assert.Equal("", item.Message)));
    }

    [Fact]
    public async Task Preview_isolates_fetch_failure_per_cluster()
    {
        var (alphaId, betaId, _) = await SeedClustersAsync("alpha", "beta");
        _alpha.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(500));
        _beta.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        var service = NewService();

        var result = await service.PreviewAsync(
            PreviewRequest([alphaId, betaId], DeploymentTemplate(), ReplicasMatrix(alphaId, 3, betaId, 3)));

        Assert.Multiple(
            () =>
            {
                Assert.Equal(FleetTemplateAction.FetchFailed, result.Items[0].Action);
                Assert.Contains("获取现有", result.Items[0].Message);
                Assert.Null(result.Items[0].CurrentYaml);
            },
            () =>
            {
                Assert.Equal(FleetTemplateAction.Create, result.Items[1].Action);
                Assert.Equal("", result.Items[1].Message);
            });
    }

    [Fact]
    public async Task Deploy_creates_missing_object_and_writes_fleet_audit()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        _alpha.SetupReadConfigMapThrows(ResourceName, NamespaceName, K8sMocks.K8sError(404));
        _alpha.SetupCreateConfigMap(NamespaceName);
        var service = NewService();

        var result = await service.DeployToFleetAsync(DeployRequest([alphaId], ConfigMapTemplate(), ColorMatrix(alphaId, "blue")));

        var audits = AuditRecords();
        Assert.Multiple(
            () => Assert.Single(result.Items),
            () =>
            {
                Assert.True(result.Items[0].Succeeded);
                Assert.Equal(FleetTemplateAction.Create, result.Items[0].Action);
                Assert.Equal("", result.Items[0].Message);
                Assert.Equal(1, result.SuccessCount);
                Assert.Equal(0, result.FailureCount);
            },
            () => Assert.Equal(2, audits.Count),
            () =>
            {
                var fleet = audits.Single(audit => audit.Target.Contains("舰队下发"));
                Assert.Equal(AuditCategory.Configmap, fleet.Category);
                Assert.Equal(AuditAction.Create, fleet.Action);
                Assert.Contains("alpha", fleet.Target);
                Assert.Contains("配置", fleet.Target);
                Assert.Contains(ResourceName, fleet.Target);
                Assert.Equal("admin", fleet.UserName);
            });
    }

    [Fact]
    public async Task Deploy_updates_existing_object_via_edit_path()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, NoisyConfigMap("green"));
        _alpha.SetupReplaceConfigMap(ResourceName, NamespaceName);
        var service = NewService();

        var result = await service.DeployToFleetAsync(DeployRequest([alphaId], ConfigMapTemplate(), ColorMatrix(alphaId, "blue")));

        var fleetAudits = AuditRecords().Where(audit => audit.Target.Contains("舰队下发")).ToList();
        Assert.Multiple(
            () => Assert.True(result.Items[0].Succeeded),
            () => Assert.Equal(FleetTemplateAction.Update, result.Items[0].Action),
            () =>
            {
                var fleet = Assert.Single(fleetAudits);
                Assert.Equal(AuditCategory.Configmap, fleet.Category);
                Assert.Equal(AuditAction.Update, fleet.Action);
            });
    }

    [Fact]
    public async Task Deploy_identical_object_skips_write_and_audit()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        _alpha.SetupReadConfigMap(ResourceName, NamespaceName, NoisyConfigMap("blue"));
        var service = NewService();

        var result = await service.DeployToFleetAsync(DeployRequest([alphaId], ConfigMapTemplate(), ColorMatrix(alphaId, "blue")));

        Assert.Multiple(
            () => Assert.True(result.Items[0].Succeeded),
            () => Assert.Equal(FleetTemplateAction.Identical, result.Items[0].Action),
            () => Assert.Empty(AuditRecords()),
            () => Assert.Equal(1, _alpha.Invocations.Count));
    }

    [Fact]
    public async Task Deploy_failures_are_isolated_and_do_not_write_fleet_audit()
    {
        var (alphaId, betaId, _) = await SeedClustersAsync("alpha", "beta");
        _alpha.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _alpha.SetupCreateDeploymentThrows(NamespaceName, K8sMocks.K8sError(500));
        _beta.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _beta.SetupCreateDeployment(NamespaceName);
        var service = NewService();

        var result = await service.DeployToFleetAsync(
            DeployRequest([alphaId, betaId], DeploymentTemplate(), ReplicasMatrix(alphaId, 3, betaId, 3)));

        var audits = AuditRecords();
        Assert.Multiple(
            () =>
            {
                var failed = Assert.Single(result.Items, item => !item.Succeeded);
                Assert.Equal(alphaId, failed.ClusterId);
                Assert.Equal("下发失败,请稍后重试", failed.Message);
            },
            () =>
            {
                var succeeded = Assert.Single(result.Items, item => item.Succeeded);
                Assert.Equal(betaId, succeeded.ClusterId);
                Assert.Equal(FleetTemplateAction.Create, succeeded.Action);
            },
            () => Assert.Equal(2, audits.Count),
            () => Assert.All(audits, audit => Assert.Contains("beta", audit.Target)));
    }

    [Fact]
    public async Task Deploy_renders_per_cluster_values_and_reports_progress()
    {
        var (alphaId, betaId, gammaId) = await SeedClustersAsync("alpha", "beta", "gamma");
        _alpha.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _beta.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _gamma.SetupReadDeploymentThrows("web", NamespaceName, K8sMocks.K8sError(404));
        _alpha.SetupCreateDeployment(NamespaceName);
        _beta.SetupCreateDeployment(NamespaceName);
        _gamma.SetupCreateDeployment(NamespaceName);
        var service = NewService();
        var reports = new List<(int Current, int Total)>();
        var reportsGate = new object();
        var progress = new Progress<(int Current, int Total)>(report =>
        {
            lock (reportsGate)
            {
                reports.Add(report);
            }
        });

        var preview = await service.PreviewAsync(
            PreviewRequest([alphaId, betaId, gammaId], DeploymentTemplate(), ReplicasMatrix(alphaId, 1, betaId, 3, gammaId, 5)));
        var result = await service.DeployToFleetAsync(
            DeployRequest([alphaId, betaId, gammaId], DeploymentTemplate(), ReplicasMatrix(alphaId, 1, betaId, 3, gammaId, 5)),
            progress);

        var fleetAudits = AuditRecords().Where(audit => audit.Target.Contains("舰队下发")).ToList();
        Assert.Multiple(
            () => Assert.All(result.Items, item => Assert.True(item.Succeeded)),
            () => Assert.Contains("replicas: 1", preview.Items[0].RenderedYaml),
            () => Assert.Contains("replicas: 3", preview.Items[1].RenderedYaml),
            () => Assert.Contains("replicas: 5", preview.Items[2].RenderedYaml),
            () =>
            {
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    lock (reportsGate)
                    {
                        if (reports.Contains((3, 3)))
                        {
                            break;
                        }
                    }

                    Thread.Sleep(50);
                }
                lock (reportsGate)
                {
                    Assert.Equal((3, 3), reports[^1]);
                }
            },
            () =>
            {
                Assert.Equal(3, fleetAudits.Count);
                Assert.All(fleetAudits, audit => Assert.Equal(AuditCategory.Workload, audit.Category));
                Assert.All(fleetAudits, audit => Assert.Equal(AuditAction.Create, audit.Action));
            });
    }

    [Fact]
    public async Task Repeat_deploy_of_unchanged_template_returns_all_identical()
    {
        var (alphaId, _, _) = await SeedClustersAsync("alpha");
        _alpha.SetupSequence(x => x.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                It.Is<string>(n => n == ResourceName),
                It.Is<string>(n => n == NamespaceName),
                It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(K8sMocks.K8sError(404))
            .ReturnsAsync(new HttpOperationResponse<V1ConfigMap> { Body = NoisyConfigMap("blue") });
        _alpha.SetupCreateConfigMap(NamespaceName);
        var service = NewService();
        var request = DeployRequest([alphaId], ConfigMapTemplate(), ColorMatrix(alphaId, "blue"));

        var first = await service.DeployToFleetAsync(request);
        var second = await service.DeployToFleetAsync(request);

        Assert.Multiple(
            () => Assert.Equal(FleetTemplateAction.Create, first.Items[0].Action),
            () => Assert.Equal(FleetTemplateAction.Identical, second.Items[0].Action),
            () => Assert.True(second.Items[0].Succeeded),
            () => Assert.Equal(0, second.FailureCount));
    }

    private static string ConfigMapTemplate() => $"""
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: {ResourceName}
          namespace: {NamespaceName}
        data:
          color: {ColorPlaceholder}
        """;

    private static string NamelessConfigMapTemplate() => """
        apiVersion: v1
        kind: ConfigMap
        data:
          color: blue
        """;

    private static string IngressTemplate() => """
        apiVersion: networking.k8s.io/v1
        kind: Ingress
        metadata:
          name: web
        spec:
          rules: []
        """;

    private static string DeploymentTemplate() => $"""
        apiVersion: apps/v1
        kind: Deployment
        metadata:
          name: web
          namespace: {NamespaceName}
        spec:
          replicas: {ReplicasPlaceholder}
          selector:
            matchLabels:
              app: web
          template:
            metadata:
              labels:
                app: web
            spec:
              containers:
                - name: web
                  image: nginx:1.0
        """;

    private static FleetTemplatePreviewRequest PreviewRequest(IReadOnlyList<int> clusterIds, string templateYaml, IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> variables) =>
        new(templateYaml, clusterIds, variables);

    private static FleetTemplateDeployRequest DeployRequest(IReadOnlyList<int> clusterIds, string templateYaml, IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> variables) =>
        new(templateYaml, clusterIds, variables);

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> ColorMatrix(int clusterId, string color) =>
        new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [clusterId] = new Dictionary<string, string> { ["color"] = color }
        };

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> ReplicasMatrix(
        int alphaId,
        int alphaReplicas,
        int betaId,
        int betaReplicas,
        int gammaId = 0,
        int gammaReplicas = 0) => new Dictionary<int, IReadOnlyDictionary<string, string>>
    {
        [alphaId] = new Dictionary<string, string> { ["replicas"] = alphaReplicas.ToString() },
        [betaId] = new Dictionary<string, string> { ["replicas"] = betaReplicas.ToString() },
        [gammaId] = new Dictionary<string, string> { ["replicas"] = gammaReplicas.ToString() }
    };

    private static V1ConfigMap NoisyConfigMap(string color) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = ResourceName,
            NamespaceProperty = NamespaceName,
            Uid = "uid-server",
            ResourceVersion = "123",
            CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ManagedFields = [new V1ManagedFieldsEntry { Manager = "kubectl" }]
        },
        Data = new Dictionary<string, string> { ["color"] = color }
    };

    private static V1Deployment NoisyDeployment(int replicas) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = "web",
            NamespaceProperty = NamespaceName,
            Uid = "uid-server",
            ResourceVersion = "123",
            CreationTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ManagedFields = [new V1ManagedFieldsEntry { Manager = "kubectl" }]
        },
        Spec = new V1DeploymentSpec
        {
            Replicas = replicas,
            Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = "web" } },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { ["app"] = "web" } },
                Spec = new V1PodSpec
                {
                    Containers = [new V1Container { Name = "web", Image = "nginx:1.0" }]
                }
            }
        },
        Status = new V1DeploymentStatus { ObservedGeneration = 1 }
    };

    private async Task<(int alpha, int beta, int gamma)> SeedClustersAsync(string alphaName = "alpha", string betaName = "beta", string gammaName = "gamma")
    {
        EnsureProvider(TestData.NewCluster(alphaName), TestData.NewCluster(betaName), TestData.NewCluster(gammaName));
        using var scope = _provider!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clusters = await db.Clusters.AsNoTracking().OrderBy(cluster => cluster.Id).ToListAsync();
        return (
            clusters.Single(cluster => cluster.Name == alphaName).Id,
            clusters.Single(cluster => cluster.Name == betaName).Id,
            clusters.Single(cluster => cluster.Name == gammaName).Id);
    }

    private List<AuditLog> AuditRecords()
    {
        using var scope = _provider!.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return db.AuditLogs.AsNoTracking().ToList();
    }

    private FleetTemplateService NewService()
    {
        if (_provider is null)
        {
            BuildProvider(_accessor.Object, RoutingClientCache(), []);
        }

        return _provider!.GetRequiredService<FleetTemplateService>();
    }

    private FleetTemplateService NewServiceWithAccessor(IHttpContextAccessor accessor)
    {
        _provider?.Dispose();
        BuildProvider(accessor, RoutingClientCache(), []);
        return NewService();
    }

    private void EnsureProvider(params ClusterInfo[] seedClusters) => BuildProvider(_accessor.Object, RoutingClientCache(), seedClusters);

    private void BuildProvider(IHttpContextAccessor accessor, IClusterClientCache cache, ClusterInfo[] seedClusters)
    {
        _dbPath ??= Path.Combine(Path.GetTempPath(), $"mcms-fleet-{Guid.NewGuid():N}.db");
        var dbPath = _dbPath;
        var services = new ServiceCollection();
        services.AddScoped(_ => new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={dbPath}").Options));
        services.AddScoped<IClusterRepository>(sp => new ClusterRepository(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<IHelmReleaseOwnershipRepository>(sp => new HelmReleaseOwnershipRepository(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<IAuditLogRepository>(sp => new AuditLogRepository(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddSingleton(accessor);
        services.AddSingleton(cache);
        services.AddLogging();
        services.AddApplicationServices();
        _provider = services.BuildServiceProvider();

        using (var bootstrap = _provider.CreateScope())
        {
            bootstrap.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
            if (seedClusters.Length > 0)
            {
                var repo = bootstrap.ServiceProvider.GetRequiredService<IClusterRepository>();
                foreach (var cluster in seedClusters)
                {
                    repo.AddAsync(cluster).GetAwaiter().GetResult();
                }
            }
        }
    }

    private IClusterClientCache RoutingClientCache() => new ClusterClientCache(
        conf => conf.Host == AlphaHost ? _alpha.Object : conf.Host == BetaHost ? _beta.Object : _gamma.Object,
        NullLogger<ClusterClientCache>.Instance);
}
