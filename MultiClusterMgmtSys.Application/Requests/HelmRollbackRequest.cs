namespace MultiClusterMgmtSys.Application.Requests;

/// <summary>
/// Helm release 回滚请求:回到历史中的某个 revision;归属不变。
/// </summary>
/// <param name="ClusterId">目标集群 Id。</param>
/// <param name="Namespace">release 所在命名空间。</param>
/// <param name="ReleaseName">release 名称。</param>
/// <param name="Revision">目标 revision。</param>
public record HelmRollbackRequest(
    int ClusterId,
    string Namespace,
    string ReleaseName,
    int Revision);
