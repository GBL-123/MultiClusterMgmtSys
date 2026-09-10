namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// ConfigMap 列表页展示数据。
/// </summary>
public class ConfigMapListViewModel
{
    /// <summary>ConfigMap 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>数据键数量。</summary>
    public int DataKeyCount { get; set; } = 0;

    /// <summary>数据键预览文本(超过 3 个截断追加省略号)。</summary>
    public string DataKeyPreview { get; set; } = "";

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
