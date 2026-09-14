using k8s;
using System.Text;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Data.Entities;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// Kubernetes 客户端配置构建:由 <see cref="ClusterInfo"/> 生成 <see cref="KubernetesClientConfiguration"/>,
/// 并统一施加所有 REST 调用的超时上限(契约见 kubernetes-call-timeout spec)。
/// </summary>
internal static class KubernetesClientConfig
{
    /// <summary>单次 Kubernetes REST 调用的统一超时上限(10 秒)。</summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>按集群连接方式(kubeconfig 或 Token)构建客户端配置,并设置统一超时。</summary>
    /// <param name="cluster">集群实体(含连接类型与凭据)。</param>
    /// <returns>可直接交给客户端工厂的配置对象。</returns>
    public static KubernetesClientConfiguration Build(ClusterInfo cluster)
    {
        KubernetesClientConfiguration config;
        if (cluster.ConnectionType == ConnectionType.KubeConfig)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cluster.KubeConfig ?? ""));
            config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
        }
        else
        {
            config = new KubernetesClientConfiguration
            {
                Host = cluster.ApiServer ?? "",
                AccessToken = cluster.Token ?? "",
                SkipTlsVerify = cluster.SkipTlsVerify
            };
        }

        config.HttpClientTimeout = RequestTimeout;
        return config;
    }

    /// <summary>按集群连接方式解析 API Server 地址(kubeconfig 方式从文本解析 Host,Token 方式即登记的 ApiServer);供探测后回填使用。</summary>
    /// <param name="cluster">集群实体。</param>
    /// <returns>解析出的 API Server 地址。</returns>
    public static string GetHost(ClusterInfo cluster)
    {
        if (cluster.ConnectionType == ConnectionType.KubeConfig)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cluster.KubeConfig ?? ""));
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(stream).Host;
        }

        return cluster.ApiServer ?? "";
    }
}
