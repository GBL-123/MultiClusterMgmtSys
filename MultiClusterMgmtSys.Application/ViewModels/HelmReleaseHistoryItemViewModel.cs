using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>Helm release 历史(单个 revision)展示数据。</summary>
public class HelmReleaseHistoryItemViewModel
{
    /// <summary>revision 序号。</summary>
    public int Revision { get; set; }

    /// <summary>该 revision 的 Helm 状态原值。</summary>
    public string Status { get; set; } = "";

    /// <summary>状态中文展示名。</summary>
    public string StatusText => HelmDisplayText.StatusText(Status);

    /// <summary>状态徽章 CSS 类。</summary>
    public string StatusCssClass => HelmDisplayText.StatusCssClass(Status);

    /// <summary>chart 引用(形如 name-version)。</summary>
    public string Chart { get; set; } = "";

    /// <summary>chart 的 appVersion。</summary>
    public string AppVersion { get; set; } = "";

    /// <summary>该 revision 的描述。</summary>
    public string Description { get; set; } = "";

    /// <summary>该 revision 的时间;格式无法识别时为 null。</summary>
    public DateTime? UpdatedAt { get; set; }
}
