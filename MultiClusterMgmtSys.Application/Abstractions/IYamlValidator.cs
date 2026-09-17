using MultiClusterMgmtSys.Application.Enums;

namespace MultiClusterMgmtSys.Application.Abstractions;

/// <summary>
/// YAML 本地校验端口:按资源类型解析 YAML(语法错误抛解析异常),避免 UI 直接依赖 KubernetesClient;
/// 实现位于 Application(k8s 包为 Application 的务实边界内)。
/// </summary>
public interface IYamlValidator
{
    /// <summary>校验 ConfigMap YAML 语法;语法错误抛解析异常(携带原始错误消息)。</summary>
    /// <param name="yaml">YAML 文本。</param>
    void ValidateConfigMap(string yaml);

    /// <summary>校验 Service YAML 语法;语法错误抛解析异常(携带原始错误消息)。</summary>
    /// <param name="yaml">YAML 文本。</param>
    void ValidateService(string yaml);

    /// <summary>校验命名空间 YAML 语法并返回 metadata.name(可能为空);语法错误抛解析异常。</summary>
    /// <param name="yaml">YAML 文本。</param>
    /// <returns>metadata.name;未提供时为 null。</returns>
    string? ValidateNamespace(string yaml);

    /// <summary>校验指定工作负载类型(Deployment/StatefulSet/DaemonSet/ReplicaSet)的 YAML 语法;语法错误抛解析异常。</summary>
    /// <param name="kind">工作负载类型。</param>
    /// <param name="yaml">YAML 文本。</param>
    void ValidateWorkload(WorkloadKind kind, string yaml);
}
