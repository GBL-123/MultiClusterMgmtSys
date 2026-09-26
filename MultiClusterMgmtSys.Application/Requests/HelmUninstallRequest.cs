namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm release 卸载请求:成功后删除归属记录并写审计。
/// </summary>
/// <param name="ClusterId">目标集群 Id。</param>
/// <param name="Namespace">release 所在命名空间。</param>
/// <param name="ReleaseName">release 名称。</param>
/// <param name="KeepHistory">是否保留 release 历史(--keep-history)。</param>
public record HelmUninstallRequest(
    int ClusterId,
    string Namespace,
    string ReleaseName,
    bool KeepHistory);
