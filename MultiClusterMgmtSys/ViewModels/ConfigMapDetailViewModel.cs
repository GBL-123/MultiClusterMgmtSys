namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// ConfigMap 详情页展示数据。
/// </summary>
public class ConfigMapDetailViewModel
{
    /// <summary>ConfigMap 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>API 对象唯一标识(Uid)。</summary>
    public string Uid { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>键值对数据。</summary>
    public Dictionary<string, string> Data { get; set; } = new();

    /// <summary>ConfigMap 原始 YAML 文本,用于详情页 YAML 视图。</summary>
    public string Yaml { get; set; } = "";
}
