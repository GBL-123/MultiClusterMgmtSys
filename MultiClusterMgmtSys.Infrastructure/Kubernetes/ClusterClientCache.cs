using MultiClusterMgmtSys.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;
using k8s;
using Microsoft.Extensions.Caching.Memory;
using MultiClusterMgmtSys.Domain.Entities;

namespace MultiClusterMgmtSys.Infrastructure.Kubernetes;

/// <summary>
/// 按集群 Id 缓存 Kubernetes 客户端(单例,跨用户会话共享):以凭据五项(连接方式/kubeconfig 文本/API 地址/Token/跳过 TLS 校验)的
/// 哈希为指纹,指纹变化即重建并替换旧条目,失效由调用时对实体的观察得出,不依赖编辑路径显式通知;条目按最近使用时间空闲过期
/// (默认 10 分钟,容量上限 100 远超实际集群规模),驱逐时释放客户端归还连接资源。未命中或指纹变化一律经
/// <see cref="KubernetesClientConfig"/> 与既有客户端工厂创建,统一请求超时契约与测试注入契约不受影响。
/// </summary>
/// <param name="clientFactory">既有 Kubernetes 客户端工厂(缓存未命中的唯一创建路径)。</param>
/// <param name="logger">日志。</param>
/// <param name="idleEviction">条目空闲过期时长,缺省 10 分钟(测试可传更短值驱动驱逐)。</param>
public class ClusterClientCache(
    Func<KubernetesClientConfiguration, IKubernetes> clientFactory,
    ILogger<ClusterClientCache> logger,
    TimeSpan? idleEviction = null) : IClusterClientCache, IDisposable
{
    private static readonly TimeSpan DefaultIdleEviction = TimeSpan.FromMinutes(10);

    private const int MaxEntries = 100;

    private readonly TimeSpan idle = idleEviction ?? DefaultIdleEviction;

    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });

    private readonly object gate = new();

    /// <summary>按集群取 K8s 客户端:指纹命中直接复用(零握手);未命中或指纹变化时经统一配置与工厂重建,旧客户端由驱逐回调释放。</summary>
    /// <param name="cluster">集群实体(凭据相关字段须为当前值)。</param>
    /// <returns>可并发共享使用的 Kubernetes 客户端。</returns>
    public IKubernetes GetOrCreate(ClusterInfo cluster)
    {
        var fingerprint = Fingerprint(cluster);
        lock (gate)
        {
            if (cache.TryGetValue(cluster.Id, out CacheEntry? cached) && cached is not null && cached.Fingerprint == fingerprint)
            {
                return cached.Client;
            }

            var config = KubernetesClientConfig.Build(cluster);
            var client = clientFactory(config);
            logger.LogInformation("K8s client {Action} for cluster {ClusterName} id={ClusterId}",
                cached is null ? "created" : "rebuilt", cluster.Name, cluster.Id);
            cache.Set(cluster.Id, new CacheEntry(fingerprint, client), CreateEntryOptions());
            return client;
        }
    }

    /// <summary>释放缓存容器;其驱逐回调会同步释放全部在存客户端。</summary>
    public void Dispose() => cache.Dispose();

    /// <summary>按集群凭据解析用于连接的 API Server 地址(kubeconfig 解析结果或 Token 方式的地址),供探测成功后回填集群记录。</summary>
    /// <param name="cluster">集群实体(凭据相关字段须为当前值)。</param>
    /// <returns>API Server 地址。</returns>
    public string ResolveApiServer(ClusterInfo cluster) => KubernetesClientConfig.GetHost(cluster);

    private MemoryCacheEntryOptions CreateEntryOptions() => new()
    {
        Size = 1,
        SlidingExpiration = idle,
        PostEvictionCallbacks = { new PostEvictionCallbackRegistration
        {
            EvictionCallback = (_, value, _, _) =>
            {
                if (value is CacheEntry { Client: IDisposable disposable })
                {
                    disposable.Dispose();
                }
            }
        } }
    };

    /// <summary>以 BinaryWriter 的长度前缀编码顺序写入凭据五项后整体哈希,字段内容含分隔符也不会产生指纹碰撞。</summary>
    private static string Fingerprint(ClusterInfo cluster)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(cluster.ConnectionType.ToString());
            writer.Write(cluster.KubeConfig ?? "");
            writer.Write(cluster.ApiServer ?? "");
            writer.Write(cluster.Token ?? "");
            writer.Write(cluster.SkipTlsVerify);
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private sealed record CacheEntry(string Fingerprint, IKubernetes Client);
}
