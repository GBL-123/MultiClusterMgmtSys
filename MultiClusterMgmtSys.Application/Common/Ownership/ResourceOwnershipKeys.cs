namespace MultiClusterMgmtSys.Application.Common.Ownership;

/// <summary>
/// 原生资源归属元数据与 Helm 托管标识的键名契约(契约见 k8s-resource-ownership):
/// 归属以 label 存创建者账号 Id、注解存用户名快照;Helm 托管对象经标准注解识别后改查归属表。
/// </summary>
public static class ResourceOwnershipKeys
{
    /// <summary>创建者账号 Id 的 label 键(值为账号 Id,恒为合法 label 值)。</summary>
    public const string OwnerUidLabel = "mcms.ms/owner-uid";

    /// <summary>创建者用户名快照的注解键(注解值无字符集限制)。</summary>
    public const string OwnerNameAnnotation = "mcms.ms/owner-name";

    /// <summary>Helm 写入的 release 名称注解键。</summary>
    public const string HelmReleaseNameAnnotation = "meta.helm.sh/release-name";

    /// <summary>Helm 写入的 release 命名空间注解键。</summary>
    public const string HelmReleaseNamespaceAnnotation = "meta.helm.sh/release-namespace";
}
