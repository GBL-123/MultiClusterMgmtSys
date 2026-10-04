namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Secret YAML 编辑页展示数据(值以 <c>&lt;REDACTED:key&gt;</c> 占位符回显,契约见 secrets-page)。
/// </summary>
public class SecretEditViewModel
{
    /// <summary>Secret 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>Secret 类型。</summary>
    public string Type { get; set; } = "";

    /// <summary>带回显占位符的 YAML 文本。</summary>
    public string Yaml { get; set; } = "";

    /// <summary>当前用户是否可操作该资源(Admin 或创建者本人,fail-closed;契约见 k8s-resource-ownership)。</summary>
    public bool CanOperate { get; set; }
}
