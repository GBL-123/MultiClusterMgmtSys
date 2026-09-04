using k8s;
using k8s.Models;
using Moq;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Tests.TestInfrastructure;
using Xunit;

namespace MultiClusterMgmtSys.Tests.Services;

public class ConfigMapServiceTests
{
    [Fact]
    public async Task GetNamespaces_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetNamespacesAsync(999));
    }

    [Fact]
    public async Task ListConfigMaps_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(() => svc.ListConfigMapsAsync(999, "default"));
    }

    [Fact]
    public async Task GetConfigMap_MissingCluster_ReturnsNull()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        var result = await svc.GetConfigMapAsync(999, "cm", "default");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteConfigMap_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteConfigMapAsync(999, "cm", "default"));
    }

    [Fact]
    public async Task UpdateConfigMap_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(() => svc.UpdateConfigMapFromYamlAsync(999, "cm", "default", "{}"));
    }

    [Fact]
    public async Task CreateConfigMap_MissingCluster_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        await Assert.ThrowsAsync<NotFoundException>(() => svc.CreateConfigMapFromYamlAsync(999, "{}"));
    }

    [Fact]
    public async Task DeleteConfigMap_K8s404_ThrowsNotFound()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.DeleteNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<V1DeleteOptions>(), It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 404 }));

        var svc = TestServices.ConfigMap(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteConfigMapAsync(1, "cm", "default"));

        Assert.Contains("删除配置", ex.UserMessage);
    }

    [Fact]
    public async Task UpdateConfigMap_K8s409_ThrowsConflict()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.ReadNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 409 }));

        var svc = TestServices.ConfigMap(db, _ => fake.Object);

        await Assert.ThrowsAsync<ConflictException>(
            () => svc.UpdateConfigMapFromYamlAsync(1, "cm", "default", "apiVersion: v1\nkind: ConfigMap\nmetadata:\n  name: cm"));
    }

    [Fact]
    public async Task CreateConfigMap_K8s400_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();

        var fake = new Mock<IKubernetes>();
        fake.Setup(c => c.CoreV1.CreateNamespacedConfigMapWithHttpMessagesAsync(
                It.IsAny<V1ConfigMap>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KubernetesException(new V1Status { Code = 400, Message = "字段非法" }));

        var svc = TestServices.ConfigMap(db, _ => fake.Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateConfigMapFromYamlAsync(1, "apiVersion: v1\nkind: ConfigMap\nmetadata:\n  name: cm\n  namespace: default"));

        Assert.Contains("字段非法", ex.UserMessage);
    }

    [Fact]
    public async Task CreateConfigMap_YamlWithoutNamespace_ThrowsValidation()
    {
        using var db = SqliteDbFactory.CreateContext();
        db.Clusters.Add(TestData.NewCluster("c"));
        await db.SaveChangesAsync();
        var svc = TestServices.ConfigMap(db, TestServices.ThrowingFactory());

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => svc.CreateConfigMapFromYamlAsync(1, "apiVersion: v1\nkind: ConfigMap\nmetadata:\n  name: cm"));

        Assert.Contains("namespace", ex.UserMessage);
    }
}