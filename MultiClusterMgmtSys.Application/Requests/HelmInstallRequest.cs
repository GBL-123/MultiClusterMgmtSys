namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm release 安装请求:上传的 chart 包、用户 values 与目标位置,
/// 由 Helm 服务消费并在成功后写入归属与审计。
/// </summary>
/// <param name="ClusterId">目标集群 Id。</param>
/// <param name="Namespace">目标命名空间。</param>
/// <param name="ReleaseName">release 名称(DNS-1123)。</param>
/// <param name="ChartPackage">上传的 chart 包(.tgz)内容。</param>
/// <param name="ValuesYaml">用户 values(YAML 文本);空白表示不提供。</param>
/// <param name="CreateNamespace">命名空间不存在时是否创建。</param>
/// <param name="Wait">是否等待资源就绪(--wait)。</param>
public record HelmInstallRequest(
    int ClusterId,
    string Namespace,
    string ReleaseName,
    byte[] ChartPackage,
    string ValuesYaml,
    bool CreateNamespace,
    bool Wait);
