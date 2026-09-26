namespace MultiClusterMgmtSys.Application.ViewModels.Mappings;

/// <summary>
/// Helm release 状态的中文展示文本与状态徽章 CSS 类的纯函数映射
/// (契约见 display-conventions 与 ui-theme):deployed 用在线色系、failed 用离线色系、其余用未知色系;
/// 未登记状态回退原始文本,不做臆造翻译。
/// </summary>
public static class HelmDisplayText
{
    /// <summary>release 状态中文展示名;未知值回退原文。</summary>
    /// <param name="status">Helm 状态原值(如 deployed)。</param>
    /// <returns>中文展示名或原始文本。</returns>
    public static string StatusText(string status) => status switch
    {
        "deployed" => "已部署",
        "failed" => "已失败",
        "pending-install" => "安装中",
        "pending-upgrade" => "升级中",
        "pending-rollback" => "回滚中",
        "superseded" => "已取代",
        "uninstalling" => "卸载中",
        "uninstalled" => "已卸载",
        _ => status
    };

    /// <summary>release 状态对应的状态徽章 CSS 类:deployed→online、failed→offline、其余→unknown。</summary>
    /// <param name="status">Helm 状态原值。</param>
    /// <returns>在线/离线/未知三态 CSS 类。</returns>
    public static string StatusCssClass(string status) => status switch
    {
        "deployed" => "online",
        "failed" => "offline",
        _ => "unknown"
    };
}
