using k8s;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群 Kubernetes 客户端缓存契约:同一集群、凭据未变的调用复用既有客户端,凭据变化自动重建,空闲条目过期释放(契约见 k8s-client-cache spec)。
/// </summary>
public interface IClusterClientCache
{
    /// <summary>取指定集群的 K8s 客户端:命中且凭据指纹未变直接复用,否则经统一配置与既有客户端工厂重建并替换条目。</summary>
    /// <param name="cluster">集群实体(凭据相关字段须为当前值)。</param>
    /// <returns>可并发共享使用的 Kubernetes 客户端。</returns>
    IKubernetes GetOrCreate(ClusterInfo cluster);
}
