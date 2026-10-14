using MultiClusterMgmtSys.Application.Common.Yaml;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Tests.Application.Common.Yaml;

/// <summary>ServerYamlSanitizer 单元测试:归属盖章剥离(含空集合清理)、其他元数据保留、未支持资源族拒绝。</summary>
public class ServerYamlSanitizerTests
{
    private const string StampedYaml = """
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: demo
          namespace: app
          uid: uid-1
          resourceVersion: "9"
          creationTimestamp: "2026-01-01T00:00:00Z"
          managedFields:
            - manager: kubectl
          labels:
            mcms.ms/owner-uid: "7"
          annotations:
            mcms.ms/owner-name: admin
        data:
          color: blue
        """;

    private const string PlainYaml = """
        apiVersion: v1
        kind: ConfigMap
        metadata:
          name: demo
          namespace: app
        data:
          color: blue
        """;

    [Fact]
    public void Sanitize_strips_ownership_stamps_so_stamped_and_plain_are_equal()
    {
        var stamped = ServerYamlSanitizer.Sanitize(StampedYaml, CompareKind.ConfigMap);
        var plain = ServerYamlSanitizer.Sanitize(PlainYaml, CompareKind.ConfigMap);

        Assert.Multiple(
            () => Assert.Equal(plain, stamped),
            () => Assert.DoesNotContain("owner-uid", stamped),
            () => Assert.DoesNotContain("labels: {}", stamped),
            () => Assert.DoesNotContain("annotations: {}", stamped));
    }

    [Fact]
    public void Sanitize_keeps_unrelated_labels_and_annotations()
    {
        var yaml = """
            apiVersion: v1
            kind: ConfigMap
            metadata:
              name: demo
              namespace: app
              labels:
                mcms.ms/owner-uid: "7"
                app: web
              annotations:
                mcms.ms/owner-name: admin
                note: keep
            data:
              color: blue
            """;

        var result = ServerYamlSanitizer.Sanitize(yaml, CompareKind.ConfigMap);

        Assert.Multiple(
            () => Assert.Contains("app: web", result),
            () => Assert.Contains("note: keep", result),
            () => Assert.DoesNotContain("owner-uid", result),
            () => Assert.DoesNotContain("owner-name", result));
    }

    [Fact]
    public void Sanitize_rejects_unsupported_kind()
        => Assert.Throws<ValidationException>(() => ServerYamlSanitizer.Sanitize("apiVersion: v1\nkind: Pod\n", (CompareKind)999));
}
