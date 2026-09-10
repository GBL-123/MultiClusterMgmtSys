namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 集群凭据的接入方式(对应 <c>ClusterInfo.ConnectionType</c>):
/// 决定 <c>ClusterService</c> 用 kubeconfig 文本还是 Token 文本构建客户端。
/// </summary>
public enum ConnectionType
{
    /// <summary>kubeconfig 文本(YAML)。</summary>
    KubeConfig,

    /// <summary>直连 Token 文本(API Server 地址 + Bearer Token)。</summary>
    Token
}
