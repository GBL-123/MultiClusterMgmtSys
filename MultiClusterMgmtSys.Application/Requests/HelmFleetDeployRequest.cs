namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm 批量多集群下发请求:一次上传的 chart 包与统一参数,应用到全部目标集群;
/// 由 Helm 服务消费,逐集群按「不存在安装 / 已存在升级」分支执行(契约见 helm-fleet-deploy spec)。
/// </summary>
/// <param name="Namespace">目标命名空间(全部集群一致,DNS-1123)。</param>
/// <param name="ReleaseName">release 名称(全部集群一致,DNS-1123)。</param>
/// <param name="ChartPackage">上传的 chart 包(.tgz)内容。</param>
/// <param name="ValuesYaml">统一 values(YAML 文本,应用到全部目标集群);空白表示不提供。</param>
/// <param name="CreateNamespace">命名空间不存在时是否创建。</param>
/// <param name="Wait">是否等待资源就绪(--wait)。</param>
/// <param name="ClusterIds">目标集群 Id 列表(至少 1 个)。</param>
public record HelmFleetDeployRequest(
    string Namespace,
    string ReleaseName,
    byte[] ChartPackage,
    string ValuesYaml,
    bool CreateNamespace,
    bool Wait,
    IReadOnlyList<int> ClusterIds);
