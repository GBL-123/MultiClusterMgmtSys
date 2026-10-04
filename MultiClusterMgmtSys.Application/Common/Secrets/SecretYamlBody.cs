namespace MultiClusterMgmtSys.Application.Common.Secrets;

/// <summary>
/// Secret 的 YAML 解析/序列化载体:与 V1Secret 同形但 data/stringData 均为字符串字典,
/// 使占位符(如 <c>&lt;REDACTED:password&gt;</c>)等非 base64 值也能通过 <see cref="k8s.KubernetesYaml"/> 往返
/// (V1Secret 的 data 为 byte[] 属性,无法承载占位符);创建与编辑合并路径均经它反序列化(契约见 secrets-page)。
/// </summary>
public class SecretYamlBody
{
    /// <summary>apiVersion;固定 v1。</summary>
    public string? ApiVersion { get; set; }

    /// <summary>kind;固定 Secret。</summary>
    public string? Kind { get; set; }

    /// <summary>对象元数据;仅承载解析结果,归属等元数据以服务器现值为准。</summary>
    public k8s.Models.V1ObjectMeta? Metadata { get; set; }

    /// <summary>Secret 类型(如 Opaque、kubernetes.io/tls)。</summary>
    public string? Type { get; set; }

    /// <summary>base64 键值;值可能为占位符(编辑回显)或用户提交的 base64 文本;null = YAML 未提供。</summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>明文键值(kubectl stringData 语义);值可能为占位符或用户明文;null = YAML 未提供。</summary>
    public Dictionary<string, string>? StringData { get; set; }
}
