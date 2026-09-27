using System.Globalization;
using k8s.Models;

namespace MultiClusterMgmtSys.Application.Common.Ownership;

/// <summary>
/// 创建盖章:把当前账号无条件写入对象归属元数据(契约见 k8s-resource-ownership)。
/// 无条件覆盖用户 YAML 中自带的同名/同键归属元数据,防止伪造;Admin 创建的同样盖章。
/// </summary>
public static class ResourceOwnershipStamp
{
    /// <summary>按当前身份覆盖写入对象的归属 label 与注解(传入的元数据须非空)。</summary>
    /// <param name="metadata">目标对象元数据(创建路径已校验 namespace,必非空)。</param>
    /// <param name="ownerUserId">创建者账号 Id。</param>
    /// <param name="ownerUserName">创建者用户名。</param>
    public static void Stamp(V1ObjectMeta metadata, int ownerUserId, string ownerUserName)
    {
        var labels = metadata.Labels ??= new Dictionary<string, string>();
        labels[ResourceOwnershipKeys.OwnerUidLabel] = ownerUserId.ToString(CultureInfo.InvariantCulture);

        var annotations = metadata.Annotations ??= new Dictionary<string, string>();
        annotations[ResourceOwnershipKeys.OwnerNameAnnotation] = ownerUserName;
    }
}
