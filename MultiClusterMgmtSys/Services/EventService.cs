using k8s;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 事件管理服务:基于所选集群的凭据实时读取 Kubernetes core/v1 事件。
/// 只读能力——单次列举全部命名空间,不提供增删改,不写审计。
/// </summary>
public class EventService(ClusterRepository repo, ILogger<EventService> logger, IClusterClientCache clientCache)
{
    private readonly ClusterRepository repo = repo;

    private readonly ILogger<EventService> logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供事件页命名空间筛选下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
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

    /// <summary>列举所选集群全部命名空间的 core/v1 事件(保留 K8s 聚合计数语义);集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    /// <param name="request">列举入参(目标集群 Id)。</param>
    public async Task<List<EventListViewModel>> ListEventsAsync(EventQueryRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            var list = await client.CoreV1.ListEventForAllNamespacesAsync();
            return list.Items.Select(e => e.ToEventListViewModel(request.ClusterId)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListEvents failed clusterId={ClusterId}", request.ClusterId);
            throw K8sExceptionMapper.Translate(ex, "加载事件列表");
        }
    }
}
