using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.ViewModels;

/// <summary>
/// 服务后端端点(Endpoint)条目展示数据,用于详情页端点列表。
/// </summary>
public class SvcEndpointViewModel
{
    /// <summary>端点地址,格式为 IP 或 IP:端口(EndpointSlice 来源时带端口)。</summary>
    public string Address { get; set; } = "";

    /// <summary>与地址成对的端口文本;仅旧版 Endpoints 来源为 null。</summary>
    public string? Port { get; set; } = null;

    /// <summary>后端是否就绪(Ready)。</summary>
    public bool Ready { get; set; }

    /// <summary>就绪状态中文展示名(就绪/未就绪)。</summary>
    public string StatusText => K8sDisplayText.EndpointStatusText(Ready);

    /// <summary>就绪状态英文原值(Ready/NotReady)。</summary>
    public string StatusRaw => K8sDisplayText.EndpointStatusRaw(Ready);

    /// <summary>就绪状态对应的状态徽章 CSS 类(online/offline)。</summary>
    public string StatusCssClass => K8sDisplayText.EndpointStatusCssClass(Ready);
}

