namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

/// <summary>
/// 按 Helm 4 CLI 输出结构构造的测试样本:-o json 输出覆盖列表/状态/历史,
/// 时间同时覆盖 RFC3339 与 Go time.Time.String() 两种形态;文本样本覆盖 values/manifest 与典型失败输出。
/// </summary>
public static class HelmFixtures
{
    /// <summary>`helm list -A -o json` 输出样本(两条:Go 时间格式的 deployed 与 RFC3339 的 failed)。</summary>
    public const string ReleaseListJson = """
        [
          {
            "name": "nginx",
            "namespace": "web",
            "revision": "3",
            "updated": "2026-09-20 18:12:33.123456789 +0800 CST",
            "status": "deployed",
            "chart": "nginx-1.2.3",
            "app_version": "1.25.0"
          },
          {
            "name": "redis",
            "namespace": "cache",
            "revision": "1",
            "updated": "2026-09-19T08:01:02.000000Z",
            "status": "failed",
            "chart": "redis-20.0.0",
            "app_version": "7.4.0"
          }
        ]
        """;

    /// <summary>`helm status <name> -o json` 输出样本(Helm 4:不含 chart 元数据)。</summary>
    public const string StatusJson = """
        {
          "name": "nginx",
          "info": {
            "first_deployed": "2026-09-19T10:00:00.000000Z",
            "last_deployed": "2026-09-20T10:12:33.000000Z",
            "deleted": "",
            "description": "Upgrade complete",
            "status": "deployed",
            "notes": "1. Get the application URL by running these commands:\n  export POD_NAME=nginx\n"
          },
          "config": { "replicaCount": 2 },
          "manifest": "---\n# Source: nginx/templates/deployment.yaml\napiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: nginx\n",
          "version": 3,
          "namespace": "web",
          "apply_method": "ssa"
        }
        """;

    /// <summary>`helm get metadata <name> -o json` 输出样本(详情页 chart 信息的数据源)。</summary>
    public const string MetadataJson = """
        {
          "name": "nginx",
          "chart": "nginx",
          "version": "1.2.3",
          "appVersion": "1.25.0",
          "namespace": "web",
          "revision": 3,
          "status": "deployed",
          "deployedAt": "2026-09-20T10:12:33+08:00"
        }
        """;

    /// <summary>`helm history <name> -o json` 输出样本(两条 revision)。</summary>
    public const string HistoryJson = """
        [
          {
            "revision": 1,
            "updated": "2026-09-19T10:00:00.000000Z",
            "status": "superseded",
            "chart": "nginx-1.2.2",
            "app_version": "1.24.0",
            "description": "Install complete"
          },
          {
            "revision": 3,
            "updated": "2026-09-20T10:12:33.000000Z",
            "status": "deployed",
            "chart": "nginx-1.2.3",
            "app_version": "1.25.0",
            "description": "Upgrade complete"
          }
        ]
        """;

    /// <summary>`helm get values <name> -o yaml` 输出样本。</summary>
    public const string UserValuesYaml = """
        replicaCount: 2
        image:
          tag: "1.25.0"
        """;

    /// <summary>`helm get manifest <name>` 输出样本。</summary>
    public const string Manifest = "---\n# Source: nginx/templates/deployment.yaml\napiVersion: apps/v1\nkind: Deployment\n";

    /// <summary>release 不存在失败输出。</summary>
    public const string NotFoundError = "Error: release: not found";

    /// <summary>同名 release 冲突失败输出。</summary>
    public const string ConflictError = "Error: INSTALLATION FAILED: cannot re-use a name that is still in use";

    /// <summary>同名 release 仍有未完成操作(如进程被杀后停留在 pending-install)的失败输出。</summary>
    public const string OngoingOperationError = "Error: UPGRADE FAILED: another operation (install/upgrade/rollback) is in progress";

    /// <summary>集群权限不足失败输出。</summary>
    public const string ForbiddenError = "Error: INSTALLATION FAILED: deployments.apps \"nginx\" is forbidden: User \"x\" cannot create resource \"deployments\" in API group \"apps\"";

    /// <summary>集群不可达失败输出。</summary>
    public const string UnreachableError = "Error: INSTALLATION FAILED: Kubernetes cluster unreachable: dial tcp 10.0.0.1:6443: connect: connection refused";

    /// <summary>等待资源就绪超时(如 hook Job 镜像拉取失败)的失败输出。</summary>
    public const string WaitTimeoutError = "Error: INSTALLATION FAILED: timed out waiting for the condition";

    /// <summary>模板渲染等无法归类的失败输出。</summary>
    public const string TemplateError = "Error: INSTALLATION FAILED: template: nginx/templates/deployment.yaml:13:16: executing \"nginx/templates/deployment.yaml\" at <wrongtype>: nil pointer evaluating interface {}.replicaCount";
}
