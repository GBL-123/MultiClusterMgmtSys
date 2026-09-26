using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>Helm release 列表项展示数据。</summary>
public class HelmReleaseListViewModel
{
    /// <summary>release 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>chart 引用(形如 name-version)。</summary>
    public string Chart { get; set; } = "";

    /// <summary>chart 名称(由 chart 引用拆分;无法拆分时为空)。</summary>
    public string ChartName { get; set; } = "";

    /// <summary>chart 版本(由 chart 引用拆分;无法拆分时为空)。</summary>
    public string ChartVersion { get; set; } = "";

    /// <summary>chart 的 appVersion。</summary>
    public string AppVersion { get; set; } = "";

    /// <summary>当前 revision。</summary>
    public int Revision { get; set; }

    /// <summary>Helm 状态原值(如 deployed)。</summary>
    public string Status { get; set; } = "";

    /// <summary>状态中文展示名。</summary>
    public string StatusText => HelmDisplayText.StatusText(Status);

    /// <summary>状态徽章 CSS 类(在线/离线/未知三态)。</summary>
    public string StatusCssClass => HelmDisplayText.StatusCssClass(Status);

    /// <summary>最后更新时间;格式无法识别时为 null。</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>当前用户是否可操作该 release(Admin、或安装者本人且未被系统外重装)。</summary>
    public bool CanOperate { get; set; }
}
