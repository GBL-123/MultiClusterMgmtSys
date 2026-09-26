using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// Helm release 归属仓储:按集群/命名空间/release 三键读写归属记录,
/// 供安装成功记账、卸载清理与列表批量判定权限(契约见 helm-release-management spec)。
/// </summary>
public interface IHelmReleaseOwnershipRepository
{
    /// <summary>按三键取单条归属记录;不存在返回 null。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="releaseName">release 名称。</param>
    /// <returns>归属记录;不存在时为 null。</returns>
    Task<HelmReleaseOwnership?> GetAsync(int clusterId, string namespaceName, string releaseName);

    /// <summary>取某集群的全部归属记录,供列表页批量判定。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <returns>该集群的归属记录列表。</returns>
    Task<List<HelmReleaseOwnership>> GetByClusterAsync(int clusterId);

    /// <summary>写入或覆盖归属记录:同键已存在时更新安装者、时间与 revision(系统外重装后由本系统再安装的场景)。</summary>
    /// <param name="ownership">归属记录。</param>
    Task UpsertAsync(HelmReleaseOwnership ownership);

    /// <summary>按三键删除归属记录;不存在时静默返回。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <param name="namespaceName">命名空间。</param>
    /// <param name="releaseName">release 名称。</param>
    Task DeleteAsync(int clusterId, string namespaceName, string releaseName);
}
