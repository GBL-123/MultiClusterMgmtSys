using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// 命名空间详情页展示数据。
/// </summary>
public class NamespaceDetailViewModel
{
    /// <summary>命名空间名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>命名空间阶段原始值(Active/Terminating);供英文次行展示。</summary>
    public string Phase { get; set; } = "";

    /// <summary>状态显示文案(在线/未知)。</summary>
    public string StatusText => K8sDisplayText.NamespacePhaseText(Phase);

    /// <summary>状态徽章 CSS 类(online/unknown),对应设计系统的淡彩状态徽章。</summary>
    public string StatusCssClass => K8sDisplayText.NamespacePhaseCssClass(Phase);

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>标签键值对。</summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>注解键值对。</summary>
    public Dictionary<string, string> Annotations { get; set; } = new();

    /// <summary>命名空间原始 YAML 文本,用于详情页 YAML 视图。</summary>
    public string Yaml { get; set; } = "";
}
