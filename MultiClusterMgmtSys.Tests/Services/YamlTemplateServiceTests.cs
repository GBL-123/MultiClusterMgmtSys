using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MultiClusterMgmtSys.Services;
using MultiClusterMgmtSys.Tests.TestInfrastructure;

namespace MultiClusterMgmtSys.Tests.Services;

public class YamlTemplateServiceTests : IDisposable
{
    private readonly string tempRoot = Directory.CreateTempSubdirectory("yaml-templates").FullName;

    public void Dispose()
    {
        Directory.Delete(tempRoot, recursive: true);
    }

    private YamlTemplateService CreateProvider(string? webRoot)
        => new(Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == webRoot), NullLogger<YamlTemplateService>.Instance);

    [Fact]
    public async Task Reads_template_file_from_wwwroot()
    {
        var dir = Path.Combine(tempRoot, "templates", "service");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "clusterip.yaml");
        await File.WriteAllTextAsync(file, "apiVersion: v1\nkind: Service\nmetadata:\n  name: \n");

        var template = await CreateProvider(tempRoot).GetTemplateAsync("service", "clusterip");

        Assert.Contains("kind: Service", template);
    }

    [Fact]
    public async Task Missing_file_falls_back_to_skeleton_with_warning()
    {
        var template = await CreateProvider(tempRoot).GetTemplateAsync("workload", "deployment");

        Assert.Contains("templates/workload/deployment.yaml", template);
        Assert.Contains("kind: Deployment", template);
        Assert.Contains("metadata:", template);
    }

    [Fact]
    public async Task Null_webroot_falls_back_gracefully()
    {
        var template = await CreateProvider(null).GetTemplateAsync("configmap", "default");

        Assert.Contains("templates/configmap/default.yaml", template);
        Assert.Contains("kind: Default", template);
        Assert.Contains("metadata:", template);
    }
}

public class TemplateFilesValidationTests
{
    private static YamlTemplateService CreateProvider()
        => new(Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == TestPaths.RepoWwwRoot),
               NullLogger<YamlTemplateService>.Instance);

    public static IEnumerable<object[]> TemplateKeys => new List<object[]>
    {
        new[] { "service", "clusterip" }, new[] { "service", "nodeport" }, new[] { "service", "loadbalancer" }, new[] { "service", "externalname" },
        new[] { "configmap", "default" },
        new[] { "workload", "deployment" }, new[] { "workload", "statefulset" }, new[] { "workload", "daemonset" }, new[] { "workload", "replicaset" }
    };

    [Theory]
    [MemberData(nameof(TemplateKeys))]
    public async Task Builtin_template_files_are_valid_yaml(string category, string name)
    {
        var template = await CreateProvider().GetTemplateAsync(category, name);

        Assert.DoesNotContain("模板文件缺失", template);
        var kind = template.Split('\n').First(l => l.StartsWith("kind: ")).Split(' ')[1].Trim();
        switch (category)
        {
            case "service":
                var svc = KubernetesYaml.Deserialize<k8s.Models.V1Service>(template);
                Assert.Equal("Service", kind);
                Assert.NotNull(svc.Spec);
                break;
            case "configmap":
                KubernetesYaml.Deserialize<k8s.Models.V1ConfigMap>(template);
                Assert.Equal("ConfigMap", kind);
                break;
            default:
                switch (name)
                {
                    case "deployment":
                        KubernetesYaml.Deserialize<k8s.Models.V1Deployment>(template);
                        break;
                    case "statefulset":
                        KubernetesYaml.Deserialize<k8s.Models.V1StatefulSet>(template);
                        break;
                    case "daemonset":
                        KubernetesYaml.Deserialize<k8s.Models.V1DaemonSet>(template);
                        break;
                    default:
                        KubernetesYaml.Deserialize<k8s.Models.V1ReplicaSet>(template);
                        break;
                }
                Assert.Contains("apiVersion: apps/v1", template);
                break;
        }
    }
}
