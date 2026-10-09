using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Common.Yaml;

/// <summary>
/// 服务器侧 YAML 元数据剥离器(静态纯函数):把集群返回的 YAML 反序列化为对应族模型后,
/// 清除 uid/resourceVersion/creationTimestamp/deletionTimestamp/managedFields 与系统归属盖章(label/annotation),
/// 工作负载族额外剥离 status 段,再整体序列化回 YAML;供跨集群对照克隆与舰队模板预览共用此规范化口径。
/// </summary>
public static class ServerYamlSanitizer
{
    /// <summary>剥离服务器侧元数据后序列化回 YAML;资源族不支持时抛 <see cref="ValidationException"/>。序列化会替换 apiVersion 与 kind 头,保证两侧产物形态一致。</summary>
    /// <param name="yaml">原始 YAML 文本(通常来自集群侧读取)。</param>
    /// <param name="kind">资源族。</param>
    /// <returns>剥离服务器侧字段后的 YAML 文本。</returns>
    /// <exception cref="ValidationException">资源族不在支持范围。</exception>
    public static string Sanitize(string yaml, CompareKind kind)
    {
        switch (kind)
        {
            case CompareKind.ConfigMap:
            {
                var body = KubernetesYaml.Deserialize<V1ConfigMap>(yaml);
                StripCommon(body.Metadata);
                body.ApiVersion = V1ConfigMap.KubeApiVersion;
                body.Kind = V1ConfigMap.KubeKind;
                return KubernetesYaml.Serialize(body);
            }
            case CompareKind.Deployment:
            {
                var body = KubernetesYaml.Deserialize<V1Deployment>(yaml);
                StripCommon(body.Metadata);
                body.Status = null;
                body.ApiVersion = KubeApiVersion(V1Deployment.KubeGroup, V1Deployment.KubeApiVersion);
                body.Kind = V1Deployment.KubeKind;
                return KubernetesYaml.Serialize(body);
            }
            case CompareKind.StatefulSet:
            {
                var body = KubernetesYaml.Deserialize<V1StatefulSet>(yaml);
                StripCommon(body.Metadata);
                body.Status = null;
                body.ApiVersion = KubeApiVersion(V1StatefulSet.KubeGroup, V1StatefulSet.KubeApiVersion);
                body.Kind = V1StatefulSet.KubeKind;
                return KubernetesYaml.Serialize(body);
            }
            case CompareKind.DaemonSet:
            {
                var body = KubernetesYaml.Deserialize<V1DaemonSet>(yaml);
                StripCommon(body.Metadata);
                body.Status = null;
                body.ApiVersion = KubeApiVersion(V1DaemonSet.KubeGroup, V1DaemonSet.KubeApiVersion);
                body.Kind = V1DaemonSet.KubeKind;
                return KubernetesYaml.Serialize(body);
            }
            case CompareKind.ReplicaSet:
            {
                var body = KubernetesYaml.Deserialize<V1ReplicaSet>(yaml);
                StripCommon(body.Metadata);
                body.Status = null;
                body.ApiVersion = KubeApiVersion(V1ReplicaSet.KubeGroup, V1ReplicaSet.KubeApiVersion);
                body.Kind = V1ReplicaSet.KubeKind;
                return KubernetesYaml.Serialize(body);
            }
            default:
                throw new ValidationException("暂不支持该资源族");
        }
    }

    /// <summary>合并 apiVersion 的组与版本(如 apps/v1);组为空时仅返回版本。</summary>
    /// <param name="group">API 组.</param>
    /// <param name="version">API 版本.</param>
    /// <returns>组合后的 apiVersion 文本。</returns>
    private static string KubeApiVersion(string group, string version) =>
        string.IsNullOrEmpty(group) ? version : $"{group}/{version}";

    /// <summary>清理直属于 metadata 的服务器侧属性(uid/resourceVersion/creationTimestamp/deletionTimestamp/managedFields)与归属盖章。</summary>
    /// <param name="metadata">对象的 metadata;null 时直接跳过。</param>
    private static void StripCommon(V1ObjectMeta? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        metadata.Uid = null;
        metadata.ResourceVersion = null;
        metadata.CreationTimestamp = null;
        metadata.DeletionTimestamp = null;
        metadata.ManagedFields = null;
        metadata.Labels?.Remove(ResourceOwnershipKeys.OwnerUidLabel);
        metadata.Annotations?.Remove(ResourceOwnershipKeys.OwnerNameAnnotation);
    }
}
