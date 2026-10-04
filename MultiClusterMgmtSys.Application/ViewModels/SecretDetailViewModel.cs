namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Secret 详情页展示数据。
/// </summary>
public class SecretDetailViewModel
{
    /// <summary>Secret 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>API 对象唯一标识(Uid)。</summary>
    public string Uid { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>Secret 类型(如 Opaque、kubernetes.io/tls)。</summary>
    public string Type { get; set; } = "";

    /// <summary>键值条目(data 逐键,值掩码);受限态为空列表。</summary>
    public List<SecretKeyItemViewModel> Entries { get; set; } = [];

    /// <summary>Secret 原始 YAML 文本(base64 原文,kubectl 等价视图);受限态为空串。</summary>
    public string Yaml { get; set; } = "";

    /// <summary>当前用户是否可操作该资源(Admin 或创建者本人,fail-closed;契约见 k8s-resource-ownership)。</summary>
    public bool CanOperate { get; set; }
}
