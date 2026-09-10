using MultiClusterMgmtSys.Common.Enums;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 集群编辑表单回填展示数据(编辑页初始值)。
/// </summary>
public class ClusterEditViewModel
{
    /// <summary>集群主键。</summary>
    public int Id { get; set; }

    /// <summary>集群名称。</summary>
    public string Name { get; set; } = "";

    /// <summary>所属分组主键;未分组为 null。</summary>
    public int? GroupId { get; set; }

    /// <summary>API Server 地址;未登记时为 null。</summary>
    public string? ApiServer { get; set; }

    /// <summary>连接方式(kubeconfig 或 Token)。</summary>
    public ConnectionType ConnectionType { get; set; }

    /// <summary>是否跳过 TLS 证书校验。</summary>
    public bool SkipTlsVerify { get; set; }

    /// <summary>kubeconfig 原文;Token 方式或未配置时为 null。</summary>
    public string? KubeConfig { get; set; }

    /// <summary>ServiceAccount Token 原文;kubeconfig 方式或未配置时为 null。</summary>
    public string? Token { get; set; }
}
