using k8s.Models;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Ownership;

/// <summary>
/// 归属判定纯函数(契约见 k8s-resource-ownership):Admin 短路由调用方处理,这里只做对象级判定——
/// 无身份 false、Helm 托管对象按归属表记录匹 owner uid、原生归属 label 匹 owner uid、无主 false(fail-closed)。
/// </summary>
public static class ResourceOwnershipPolicy
{
    /// <summary>创建目标命名空间黑名单:Member 禁止创建进 kube- 前缀系统命名空间,default 允许,Admin 不受限。</summary>
    /// <param name="namespaceName">目标命名空间。</param>
    /// <param name="isAdmin">当前操作者是否 Admin。</param>
    public static void EnsureCreationTargetNamespaceAllowed(string namespaceName, bool isAdmin)
    {
        if (isAdmin || !namespaceName.StartsWith("kube-", StringComparison.Ordinal))
        {
            return;
        }

        throw new ValidationException($"不允许在系统命名空间「{namespaceName}」中创建资源");
    }

    /// <summary>提取 Helm 托管 release 引用(两个 release 注解齐全才返回)。</summary>
    /// <param name="metadata">对象元数据。</param>
    /// <returns>(release 名称, release 命名空间);非 Helm 托管为 null。</returns>
    public static (string ReleaseName, string ReleaseNamespace)? TryGetHelmReleaseReference(V1ObjectMeta? metadata)
    {
        if (metadata?.Annotations is not { } annotations
            || !annotations.TryGetValue(ResourceOwnershipKeys.HelmReleaseNameAnnotation, out var releaseName)
            || !annotations.TryGetValue(ResourceOwnershipKeys.HelmReleaseNamespaceAnnotation, out var releaseNamespace)
            || string.IsNullOrWhiteSpace(releaseName)
            || string.IsNullOrWhiteSpace(releaseNamespace))
        {
            return null;
        }

        return new(releaseName, releaseNamespace);
    }

    /// <summary>读取对象创建者账号 Id 归属 label;无归属或值不合法为 null。</summary>
    /// <param name="metadata">对象元数据。</param>
    /// <returns>创建者账号 Id;无归属为 null。</returns>
    public static int? TryGetOwnerUid(V1ObjectMeta? metadata)
    {
        if (metadata?.Labels is not { } labels
            || !labels.TryGetValue(ResourceOwnershipKeys.OwnerUidLabel, out var value)
            || !int.TryParse(value, out var uid))
        {
            return null;
        }

        return uid;
    }

    /// <summary>归属判定(读投影与写强制共用):helmOwnership 优先,其次创建者 label,均不匹配或无身份为 false。</summary>
    /// <param name="metadata">对象元数据。</param>
    /// <param name="currentUserId">当前账号 Id;未登录为 null。</param>
    /// <param name="helmOwnership">该对象关联的 Helm release 归属记录(非 Helm 托管为 null)。</param>
    /// <returns>是否可操作。</returns>
    public static bool CanOperate(V1ObjectMeta? metadata, int? currentUserId, HelmReleaseOwnership? helmOwnership)
    {
        if (currentUserId is null)
        {
            return false;
        }

        return helmOwnership is not null
            ? helmOwnership.OwnerUserId == currentUserId.Value
            : TryGetOwnerUid(metadata) == currentUserId.Value;
    }

    /// <summary>按 Helm 归属索引判定:命中 release 记录按记录,否则按创建者 label。</summary>
    /// <param name="metadata">对象元数据。</param>
    /// <param name="currentUserId">当前账号 Id;未登录为 null。</param>
    /// <param name="helmIndex">该集群的 Helm release 归属索引((命名空间, release 名称) → 记录)。</param>
    /// <returns>是否可操作(fail-closed:未命中一律 false;Admin 由调用方短路)。</returns>
    public static bool CanOperateForIndex(
        V1ObjectMeta? metadata,
        int? currentUserId,
        IReadOnlyDictionary<(string Namespace, string ReleaseName), HelmReleaseOwnership> helmIndex)
        => CanOperate(metadata, currentUserId, TryGetHelmOwnership(metadata, helmIndex));

    /// <summary>从索引取该对象关联的 Helm release 归属记录;非 Helm 托管或索引未命中为 null。</summary>
    /// <param name="metadata">对象元数据。</param>
    /// <param name="helmIndex">该集群的 Helm release 归属索引。</param>
    /// <returns>归属记录;未命中为 null。</returns>
    public static HelmReleaseOwnership? TryGetHelmOwnership(
        V1ObjectMeta? metadata,
        IReadOnlyDictionary<(string Namespace, string ReleaseName), HelmReleaseOwnership> helmIndex)
        => TryGetHelmReleaseReference(metadata) is { } release
            ? helmIndex.GetValueOrDefault((release.ReleaseNamespace, release.ReleaseName))
            : null;
}
