namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm release 升级请求:上传的新版本 chart 包与 values 模式;归属不变。
/// </summary>
/// <param name="ClusterId">目标集群 Id。</param>
/// <param name="Namespace">release 所在命名空间。</param>
/// <param name="ReleaseName">release 名称。</param>
/// <param name="ChartPackage">上传的新版本 chart 包(.tgz)内容。</param>
/// <param name="ValuesYaml">重新编辑模式下的用户 values(YAML 文本);沿用模式忽略。</param>
/// <param name="ReuseValues">是否沿用现存用户 values(--reuse-values);为 false 时以 ValuesYaml 提交(-f)。</param>
/// <param name="Wait">是否等待资源就绪(--wait)。</param>
public record HelmUpgradeRequest(
    int ClusterId,
    string Namespace,
    string ReleaseName,
    byte[] ChartPackage,
    string ValuesYaml,
    bool ReuseValues,
    bool Wait);
