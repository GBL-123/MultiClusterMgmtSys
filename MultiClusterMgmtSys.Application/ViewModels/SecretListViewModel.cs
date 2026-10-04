namespace MultiClusterMgmtSys.Application.ViewModels;

/// <summary>
/// Secret 列表页展示数据。
/// </summary>
public class SecretListViewModel
{
    /// <summary>Secret 名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属 Kubernetes 命名空间。</summary>
    public string Namespace { get; set; } = "";

    /// <summary>Secret 类型(如 Opaque、kubernetes.io/tls)。</summary>
    public string Type { get; set; } = "";

    /// <summary>键值对数量(data + stringData)。</summary>
    public int KeyCount { get; set; } = 0;

    /// <summary>创建时间;null 表示 API 未返回。</summary>
    public DateTime? CreatedAt { get; set; } = null;

    /// <summary>当前用户是否可操作该资源(Admin 或创建者本人,fail-closed;契约见 k8s-resource-ownership)。</summary>
    public bool CanOperate { get; set; }
}
