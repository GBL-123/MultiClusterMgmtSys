namespace MultiClusterMgmtSys.Common.Enums;

/// <summary>
/// 集群端点的类别。端点是管理员登记的集群可达性元数据（VIP / 域名等），
/// 由云厂商在集群外配置，不来自 Kubernetes API。
/// 新增类别时在此处扩展枚举值即可，无需 schema 变更。
/// </summary>
public enum ClusterEndpointKind
{
    Vip = 0,
    Domain = 1
}
