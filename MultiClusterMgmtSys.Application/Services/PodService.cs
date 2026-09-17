using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// Pod 管理服务:基于所选集群的凭据实时读取 Kubernetes core/v1 Pod。
/// 只读能力——列表(全量拉取后轻投影)、详情(基本信息与容器状态),不提供写操作,不写审计(契约见 pod-management spec)。
/// </summary>
public class PodService(IClusterRepository repo, ILogger<PodService> logger, IClusterClientCache clientCache)
{
    private readonly IClusterRepository repo = repo;

    private readonly ILogger<PodService> logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供 Pod 页命名空间筛选下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    /// <param name="clusterId">目标集群 Id(数据库主键)。</param>
    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            var nsList = await client.CoreV1.ListNamespaceAsync();
            return nsList.Items.Select(n => n.Metadata?.Name ?? "").OrderBy(n => n).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListNamespaces failed clusterId={ClusterId}", clusterId);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间");
        }
    }

    /// <summary>查询 Pod 列表(轻投影);Namespace 为 null 时查全部命名空间。集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    /// <param name="request">列举入参(目标集群 Id 与可空命名空间过滤)。</param>
    public async Task<List<PodListViewModel>> ListPodsAsync(PodQueryRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            V1PodList list;
            if (!string.IsNullOrWhiteSpace(request.LabelSelector))
            {
                list = await client.CoreV1.ListNamespacedPodAsync(request.Namespace!, labelSelector: request.LabelSelector);
            }
            else if (request.Namespace is null)
            {
                list = await client.CoreV1.ListPodForAllNamespacesAsync();
            }
            else
            {
                list = await client.CoreV1.ListNamespacedPodAsync(request.Namespace);
            }
            return list.Items.Select(p => p.ToPodListViewModel(request.ClusterId)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListPods failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载 Pod 列表");
        }
    }

    /// <summary>读取单个 Pod 详情(基本信息、条件与容器状态);集群不存在返回 null,K8s 失败经翻译后抛业务异常(404 → 未找到)。</summary>
    /// <param name="request">定位入参(集群 Id + 命名空间 + 名称)。</param>
    public async Task<PodDetailViewModel?> GetPodAsync(PodKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = clientCache.GetOrCreate(entity);
        try
        {
            var pod = await client.CoreV1.ReadNamespacedPodAsync(request.Name, request.Namespace);
            return pod.ToPodDetailViewModel(request.ClusterId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadPod failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载 Pod 详情");
        }
    }

    /// <summary>读取单个容器的日志文本(行数上限与重启前语义透传 K8s);集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常,不写审计。</summary>
    /// <param name="request">读取入参(集群 Id、命名空间、Pod 名、容器、行数、是否重启前)。</param>
    public async Task<PodLogViewModel> GetPodLogAsync(PodLogRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            await using var stream = await client.CoreV1.ReadNamespacedPodLogAsync(
                request.Name,
                request.Namespace,
                container: request.Container,
                previous: request.Previous,
                tailLines: request.TailLines);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return new PodLogViewModel { Content = content };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadPodLog failed clusterId={ClusterId} ns={Namespace} name={Name} container={Container} previous={Previous}",
                request.ClusterId, request.Namespace, request.Name, request.Container, request.Previous);
            throw K8sExceptionMapper.Translate(ex, "加载 Pod 日志");
        }
    }
}
