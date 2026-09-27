using System.Security.Claims;
using k8s.Models;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Ownership;

/// <summary>
/// 服务端归属守卫(契约见 k8s-resource-ownership):组合操作者身份读取、Helm 托管分流查询与归属判定,
/// 为原生资源服务的读投影(CanOperate)与写强制(RequireOperate)提供唯一点位。
/// Admin 短路放行;无身份与无主对象均 fail-closed;初始判定失败写审计之外仅记警告日志(与 Helm 归属一致)。
/// </summary>
public sealed class ResourceOwnershipGuard(
    IHelmReleaseOwnershipRepository ownershipRepo,
    IHttpContextAccessor httpContextAccessor,
    ILogger<ResourceOwnershipGuard> logger)
{
    /// <summary>当前用户是否 Admin。</summary>
    public bool IsAdmin() => httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;

    /// <summary>当前账号 Id;无登录身份为 null。</summary>
    public int? TryGetUserId()
    {
        var idText = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idText, out var id) ? id : null;
    }

    /// <summary>创建者身份(账号 Id + 用户名);取不到登录身份抛 <see cref="PermissionException"/>(fail-closed)。</summary>
    /// <returns>(账号 Id, 用户名)。</returns>
    public (int UserId, string UserName) RequireCreator()
    {
        var userId = TryGetUserId() ?? throw new PermissionException("无法获取当前登录账号信息");
        var userName = httpContextAccessor.HttpContext?.User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            throw new PermissionException("无法获取当前登录账号信息");
        }

        return (userId, userName);
    }

    /// <summary>读投影:当前用户是否可操作该对象(不抛异常,Admin 短路)。</summary>
    /// <param name="clusterId">对象所在集群 Id。</param>
    /// <param name="metadata">对象元数据。</param>
    /// <returns>是否可操作。</returns>
    public async Task<bool> CanOperateAsync(int clusterId, V1ObjectMeta? metadata)
        => IsAdmin()
            || ResourceOwnershipPolicy.CanOperate(metadata, TryGetUserId(), await ResolveHelmOwnershipAsync(clusterId, metadata));

    /// <summary>写强制:不可操作时记录警告并抛 <see cref="PermissionException"/>(不写审计,与 Helm 失败姿态一致)。</summary>
    /// <param name="clusterId">对象所在集群 Id。</param>
    /// <param name="namespaceName">对象命名空间(日志上下文)。</param>
    /// <param name="name">对象名称(日志上下文)。</param>
    /// <param name="metadata">对象元数据(归属判定依据)。</param>
    public async Task RequireOperateAsync(int clusterId, string? namespaceName, string? name, V1ObjectMeta? metadata)
    {
        if (IsAdmin())
        {
            return;
        }

        var userId = TryGetUserId();
        if (!ResourceOwnershipPolicy.CanOperate(metadata, userId, await ResolveHelmOwnershipAsync(clusterId, metadata)))
        {
            logger.LogWarning("Resource ownership denied clusterId={ClusterId} ns={Namespace} name={Name} userId={UserId}",
                clusterId, namespaceName, name, userId);
            throw new PermissionException("仅可操作自己创建的资源");
        }
    }

    /// <summary>取该集群的 Helm release 归属索引((命名空间, release 名称) → 记录),批量投影用。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <returns>归属索引;无记录为空字典。</returns>
    public async Task<IReadOnlyDictionary<(string Namespace, string ReleaseName), HelmReleaseOwnership>> GetHelmOwnershipIndexAsync(int clusterId)
        => (await ownershipRepo.GetByClusterAsync(clusterId))
            .ToDictionary(ownership => (ownership.Namespace, ownership.ReleaseName));

    /// <summary>按对象上的 Helm 托管标识查归属记录;非 Helm 托管为 null。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <param name="metadata">对象元数据。</param>
    /// <returns>归属记录;未命中为 null。</returns>
    public async Task<HelmReleaseOwnership?> ResolveHelmOwnershipAsync(int clusterId, V1ObjectMeta? metadata)
        => ResourceOwnershipPolicy.TryGetHelmReleaseReference(metadata) is { } release
            && !string.IsNullOrWhiteSpace(release.ReleaseName)
            && !string.IsNullOrWhiteSpace(release.ReleaseNamespace)
            ? await ownershipRepo.GetAsync(clusterId, release.ReleaseNamespace.Trim(), release.ReleaseName.Trim())
            : null;
}

