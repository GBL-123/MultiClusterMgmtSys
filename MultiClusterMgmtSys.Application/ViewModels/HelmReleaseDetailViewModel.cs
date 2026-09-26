using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>Helm release 详情展示数据(状态、NOTES、values 与 manifest 分栏展示)。</summary>
public class HelmReleaseDetailViewModel
{
    /// <summary>release 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>当前 revision。</summary>
    public int Revision { get; set; }

    /// <summary>Helm 状态原值。</summary>
    public string Status { get; set; } = "";

    /// <summary>状态中文展示名。</summary>
    public string StatusText => HelmDisplayText.StatusText(Status);

    /// <summary>状态徽章 CSS 类。</summary>
    public string StatusCssClass => HelmDisplayText.StatusCssClass(Status);

    /// <summary>状态描述(如 Upgrade complete)。</summary>
    public string Description { get; set; } = "";

    /// <summary>chart NOTES 文本。</summary>
    public string Notes { get; set; } = "";

    /// <summary>chart 名称。</summary>
    public string ChartName { get; set; } = "";

    /// <summary>chart 版本。</summary>
    public string ChartVersion { get; set; } = "";

    /// <summary>chart 的 appVersion。</summary>
    public string AppVersion { get; set; } = "";

    /// <summary>渲染后的 manifest YAML 文本。</summary>
    public string Manifest { get; set; } = "";

    /// <summary>最后部署时间;格式无法识别时为 null。</summary>
    public DateTime? LastDeployedAt { get; set; }

    /// <summary>当前用户是否可操作该 release(Admin、或安装者本人且未被系统外重装)。</summary>
    public bool CanOperate { get; set; }
}
