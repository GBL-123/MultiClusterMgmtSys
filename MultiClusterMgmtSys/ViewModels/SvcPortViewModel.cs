namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 服务端口条目展示数据,用于服务列表与详情页的端口列。
/// </summary>
public class SvcPortViewModel
{
    /// <summary>端口名称;未命名端口为 null。</summary>
    public string? Name { get; set; } = null;

    /// <summary>服务对外暴露的端口号。</summary>
    public int Port { get; set; }

    /// <summary>后端目标端口(数字或具名端口);null 表示 API 未返回。</summary>
    public string? TargetPort { get; set; } = null;

    /// <summary>传输协议,通常为 TCP/UDP/SCTP。</summary>
    public string Protocol { get; set; } = "TCP";

    /// <summary>NodePort/LoadBalancer 类型分配的节点端口;其他类型为 null。</summary>
    public int? NodePort { get; set; } = null;
}
