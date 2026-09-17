using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// YAML 本地校验实现:用 KubernetesClient 的 <see cref="KubernetesYaml"/> 按资源类型反序列化,
/// 语法错误由底层解析器抛出并经上层包装为 <c>ValidationException("YAML 格式错误:...")</c>。
/// </summary>
public class YamlValidator : IYamlValidator
{
    /// <summary>校验 ConfigMap YAML 语法;语法错误抛解析异常。</summary>
    /// <param name="yaml">YAML 文本。</param>
    public void ValidateConfigMap(string yaml) => KubernetesYaml.Deserialize<V1ConfigMap>(yaml);

    /// <summary>校验 Service YAML 语法;语法错误抛解析异常。</summary>
    /// <param name="yaml">YAML 文本。</param>
    public void ValidateService(string yaml) => KubernetesYaml.Deserialize<V1Service>(yaml);

    /// <summary>校验命名空间 YAML 语法并返回 metadata.name(可能为空);语法错误抛解析异常。</summary>
    /// <param name="yaml">YAML 文本。</param>
    /// <returns>metadata.name;未提供时为 null。</returns>
    public string? ValidateNamespace(string yaml)
        => KubernetesYaml.Deserialize<V1Namespace>(yaml).Metadata?.Name;

    /// <summary>校验指定工作负载类型的 YAML 语法;语法错误抛解析异常。</summary>
    /// <param name="kind">工作负载类型。</param>
    /// <param name="yaml">YAML 文本。</param>
    public void ValidateWorkload(WorkloadKind kind, string yaml)
    {
        switch (kind)
        {
            case WorkloadKind.Deployment:
                KubernetesYaml.Deserialize<V1Deployment>(yaml);
                break;
            case WorkloadKind.StatefulSet:
                KubernetesYaml.Deserialize<V1StatefulSet>(yaml);
                break;
            case WorkloadKind.DaemonSet:
                KubernetesYaml.Deserialize<V1DaemonSet>(yaml);
                break;
            default:
                KubernetesYaml.Deserialize<V1ReplicaSet>(yaml);
                break;
        }
    }
}
