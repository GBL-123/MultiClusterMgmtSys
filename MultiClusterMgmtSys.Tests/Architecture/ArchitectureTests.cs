using System.Reflection;
using MultiClusterMgmtSys.Application;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Models;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Infrastructure;
using NetArchTest.Rules;

namespace MultiClusterMgmtSys.Tests.Architecture;

/// <summary>
/// 架构分层断言(契约 openspec/specs/architecture-layering):程序集依赖方向、端口归位、
/// Web 组件命名空间隔离与 Requests/ViewModels/Models 归属;违规即测试失败并打印类型。
/// </summary>
public class ArchitectureTests
{
    private const string ComponentsNamespace = "MultiClusterMgmtSys.Web.Components";

    private static readonly Assembly _DomainAssembly = typeof(BusinessException).Assembly;

    private static readonly Assembly _ApplicationAssembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

    private static readonly Assembly _InfrastructureAssembly = typeof(InfrastructureServiceCollectionExtensions).Assembly;

    private static readonly Assembly _WebAssembly = typeof(MultiClusterMgmtSys.Web.Components.App).Assembly;

    [Fact]
    public void Web_components_do_not_depend_on_infrastructure_ef_entities_or_k8s()
    {
        var result = Types.InAssembly(_WebAssembly)
            .That().ResideInNamespaceStartingWith(ComponentsNamespace)
            .ShouldNot().HaveDependencyOnAny(
                "MultiClusterMgmtSys.Infrastructure",
                "Microsoft.EntityFrameworkCore",
                "MultiClusterMgmtSys.Domain.Entities",
                "k8s")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Web_components_do_not_inject_repository_ports()
    {
        var forbidden = new[]
        {
            typeof(IClusterRepository),
            typeof(IGroupRepository),
            typeof(IAuditLogRepository),
            typeof(IAppSettingRepository),
            typeof(IAccountQueryRepository),
            typeof(IHelmReleaseOwnershipRepository),
        };

        var violations = new List<string>();
        foreach (var type in _WebAssembly.GetTypes().Where(t => t.Namespace?.StartsWith(ComponentsNamespace, StringComparison.Ordinal) == true))
        {
            var usedTypes = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Select(p => p.PropertyType)
                .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SelectMany(c => c.GetParameters())
                    .Select(p => p.ParameterType));
            foreach (var used in usedTypes)
            {
                if (forbidden.Contains(used))
                {
                    violations.Add($"{type.FullName} -> {used.FullName}");
                }
            }
        }

        Assert.True(violations.Count == 0, "组件不得注入仓库端口: " + string.Join(", ", violations));
    }

    [Fact]
    public void Application_does_not_depend_on_ef_infrastructure_or_web()
    {
        var result = Types.InAssembly(_ApplicationAssembly)
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "MultiClusterMgmtSys.Infrastructure",
                "MultiClusterMgmtSys.Web")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Requests_viewmodels_and_models_reside_in_their_application_folders()
    {
        var requests = Types.InAssembly(_ApplicationAssembly)
            .That().HaveNameEndingWith("Request")
            .Should().ResideInNamespace("MultiClusterMgmtSys.Application.Requests")
            .GetResult();
        Assert.True(requests.IsSuccessful, Describe(requests));

        var viewModels = Types.InAssembly(_ApplicationAssembly)
            .That().HaveNameEndingWith("ViewModel")
            .Should().ResideInNamespace("MultiClusterMgmtSys.Application.ViewModels")
            .GetResult();
        Assert.True(viewModels.IsSuccessful, Describe(viewModels));

        Assert.Equal("MultiClusterMgmtSys.Application.Models", typeof(ClusterPageQuery).Namespace);
        Assert.Equal("MultiClusterMgmtSys.Application.Models", typeof(VersionFilterSentinel).Namespace);
        Assert.Equal("MultiClusterMgmtSys.Application.Requests", typeof(ClusterQueryRequest).Namespace);
        Assert.Equal("MultiClusterMgmtSys.Application.ViewModels", typeof(ClusterViewModel).Namespace);
    }

    [Fact]
    public void Domain_and_web_do_not_depend_on_k8s()
    {
        var domain = Types.InAssembly(_DomainAssembly)
            .ShouldNot().HaveDependencyOn("k8s")
            .GetResult();
        Assert.True(domain.IsSuccessful, Describe(domain));

        // k8s 具体客户端类型仅由 Infrastructure 工厂创建;Web 整体不应再直接依赖 k8s。
        var web = Types.InAssembly(_WebAssembly)
            .ShouldNot().HaveDependencyOn("k8s")
            .GetResult();
        Assert.True(web.IsSuccessful, Describe(web));
    }

    [Fact]
    public void Assembly_references_follow_the_layering()
    {
        AssertNoReference(_ApplicationAssembly, "MultiClusterMgmtSys.Infrastructure", "MultiClusterMgmtSys.Web");
        AssertNoReference(_InfrastructureAssembly, "MultiClusterMgmtSys.Web");

        var domainRefs = _DomainAssembly.GetReferencedAssemblies()
            .Where(a => a.Name?.StartsWith("MultiClusterMgmtSys", StringComparison.Ordinal) == true)
            .Select(a => a.Name)
            .ToList();
        Assert.True(domainRefs.Count == 0, "Domain 不得引用其它生产程序集: " + string.Join(", ", domainRefs));
    }

    private static void AssertNoReference(Assembly assembly, params string[] forbidden)
    {
        var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();
        var violations = forbidden.Where(f => referenced.Contains(f)).ToList();
        Assert.True(violations.Count == 0, $"{assembly.GetName().Name} 不得引用: " + string.Join(", ", violations));
    }

    private static string Describe(NetArchTest.Rules.TestResult result)
        => "架构规则违规类型: " + string.Join(", ", result.FailingTypeNames ?? []);
}
