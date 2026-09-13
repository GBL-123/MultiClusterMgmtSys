namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 命名空间列表页展示数据。
/// </summary>
public class NamespaceListViewModel
{
    /// <summary>命名空间名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>状态显示文案(在线/未知)。</summary>
    public string StatusText { get; set; } = "";

    /// <summary>状态徽章 CSS 类(online/unknown),对应设计系统的淡彩状态徽章。</summary>
    public string StatusCssClass { get; set; } = "unknown";

    /// <summary>标签数量。</summary>
    public int LabelCount { get; set; } = 0;

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;
}
