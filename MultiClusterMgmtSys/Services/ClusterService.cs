using k8s;
using System.Text;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Services;

/// <summary>
/// 集群信息服务:集群 CRUD、端点维护、连通性探测与状态刷新,以及带过滤/排序/分页的集群查询。
/// 探测失败按优雅降级处理(状态置 Offline),不向调用方抛 K8s 异常。
/// </summary>
public class ClusterService(ClusterRepository repo, ClusterNodeService nodeService, AuditService auditService, ILogger<ClusterService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private static readonly SemaphoreSlim syncGate = new(1, 1);

    private readonly ClusterRepository repo = repo;

    private readonly ClusterNodeService nodeService = nodeService;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<ClusterService> logger = logger;

    /// <summary>分页查询集群列表,支持分组/名称/状态/版本/创建时间范围过滤与排序;版本筛选走哨兵语义(见 <see cref="VersionFilterSentinel"/>)。</summary>
    public async Task<PagedResult<ClusterViewModel>> GetPagedAsync(ClusterQueryRequest request)
    {
        var query = ToPageQuery(request);
        logger.LogInformation("GetPagedClusters page={Page} size={PageSize} groupId={GroupId} nameContains={NameContains}",
            query.Page, query.PageSize, query.GroupId, query.NameContains);
        var (items, total) = await repo.GetPagedAsync(query);
        logger.LogInformation("GetPagedClusters returned {Count} of {Total}", items.Count, total);
        return new PagedResult<ClusterViewModel>(
            [.. items.Select(c => c.ToViewModel())],
            total);
    }

    /// <summary>查询集群表中已登记的不重复版本号列表,供版本筛选下拉使用。</summary>
    public async Task<List<string>> GetAvailableVersionsAsync()
    {
        logger.LogInformation("GetAvailableVersions");
        var versions = await repo.GetDistinctVersionsAsync();
        logger.LogInformation("GetAvailableVersions returned {Count}", versions.Count);
        return versions;
    }

    /// <summary>查询单个集群详情;非 Offline 状态时实时拉取节点列表,拉取失败降级为 IsReachable=false 而不报错。集群不存在返回 null。</summary>
    /// <param name="id">集群 ID。</param>
    public async Task<ClusterDetailViewModel?> GetClusterDetailAsync(int id)
    {
        logger.LogInformation("GetClusterDetail id={ClusterId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", id);
            return null;
        }

        var vm = entity.ToDetailViewModel();
        if (entity.Status != ClusterStatus.Offline)
        {
            try
            {
                vm.Nodes = await nodeService.GetClusterNodesAsync(id);
                vm.IsReachable = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load nodes for cluster {ClusterId}", id);
                vm.IsReachable = false;
            }
        }
        logger.LogInformation("GetClusterDetail id={ClusterId} reachable={IsReachable}", id, vm.IsReachable);
        return vm;
    }

    /// <summary>查询集群的编辑用数据(含连接类型与凭据),不做连通性探测;集群不存在返回 null。</summary>
    public async Task<ClusterEditViewModel?> GetClusterForEditAsync(int id)
    {
        logger.LogInformation("GetClusterForEdit id={ClusterId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", id);
            return null;
        }
        return entity.ToEditViewModel();
    }

    /// <summary>新建集群:先入库,再应用端点列表并立即连通性探测(结果回写状态/版本/节点数),最后写创建审计。探测失败仅置 Offline,不抛异常。</summary>
    /// <param name="request">集群基本信息、连接凭据(KubeConfig 或 Token,按连接类型二选一)与端点列表。</param>
    public async Task<ClusterViewModel> AddClusterAsync(ClusterCreateRequest request)
    {
        logger.LogInformation("AddCluster name={Name} groupId={GroupId}", request.Name, request.GroupId);
        var entity = new ClusterInfo
        {
            Name = request.Name,
            GroupId = request.GroupId,
            ApiServer = request.ApiServer,
            ConnectionType = request.ConnectionType,
            KubeConfig = request.ConnectionType == ConnectionType.KubeConfig ? request.KubeConfig : null,
            Token = request.ConnectionType == ConnectionType.Token ? request.Token : null,
            SkipTlsVerify = request.SkipTlsVerify,
            Status = ClusterStatus.Unknown,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        logger.LogInformation("AddCluster created id={ClusterId}", entity.Id);
        entity.ApplyEndpoints(request.Endpoints);
        await ProbeAsync(entity);
        logger.LogInformation("AddCluster probed id={ClusterId} status={Status}", entity.Id, entity.Status);
        await repo.UpdateAsync(entity);
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Create, $"集群: {entity.Name}");
        return entity.ToViewModel();
    }

    /// <summary>更新集群;连接配置(连接类型/ApiServer/凭据/跳过 TLS 校验)发生变化时重新探测并回写结果。集群不存在抛 <see cref="NotFoundException"/>,成功后写更新审计。</summary>
    public async Task<ClusterViewModel> UpdateClusterAsync(ClusterUpdateRequest request)
    {
        logger.LogInformation("UpdateCluster id={ClusterId}", request.Id);
        var entity = await repo.GetByIdAsync(request.Id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", request.Id);
            throw new NotFoundException($"集群 {request.Id} 不存在");
        }

        var configChanged = entity.ConnectionType != request.ConnectionType
            || entity.ApiServer != request.ApiServer
            || entity.KubeConfig != request.KubeConfig
            || entity.Token != request.Token
            || entity.SkipTlsVerify != request.SkipTlsVerify;

        entity.Name = request.Name;
        entity.GroupId = request.GroupId;
        entity.ApiServer = request.ApiServer;
        entity.ConnectionType = request.ConnectionType;
        entity.SkipTlsVerify = request.SkipTlsVerify;
        entity.KubeConfig = request.ConnectionType == ConnectionType.KubeConfig ? request.KubeConfig : null;
        entity.Token = request.ConnectionType == ConnectionType.Token ? request.Token : null;

        if (configChanged)
        {
            logger.LogInformation("UpdateCluster id={ClusterId} config changed, probing", request.Id);
            await ProbeAsync(entity);
        }

        await repo.UpdateAsync(entity);
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群: {entity.Name}");
        return entity.ToViewModel();
    }

    /// <summary>删除集群及其端点、节点备注(级联);集群不存在时静默返回,删除成功后写删除审计。</summary>
    public async Task DeleteClusterAsync(int id)
    {
        logger.LogInformation("DeleteCluster id={ClusterId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is not null)
        {
            await repo.DeleteAsync(id);
            await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Delete, $"集群: {entity.Name}");
        }
    }

    /// <summary>整体替换集群的管理端点(VIP/域名元数据)列表;集群不存在抛 <see cref="NotFoundException"/>,成功后写更新审计。</summary>
    public async Task UpdateClusterEndpointsAsync(ClusterEndpointsUpdateRequest request)
    {
        logger.LogInformation("UpdateClusterEndpoints id={ClusterId} count={Count}", request.ClusterId, request.Items.Count);
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        entity.ApplyEndpoints(request.Items);
        await repo.UpdateAsync(entity);
        logger.LogInformation("UpdateClusterEndpoints persisted id={ClusterId}", request.ClusterId);
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群: {entity.Name} 端点");
    }

    /// <summary>立即探测单个集群连通性并回写状态/版本/节点数;探测失败置 Offline,不抛异常。</summary>
    public async Task<ClusterViewModel> RefreshClusterStatusAsync(int id)
    {
        logger.LogInformation("RefreshClusterStatus id={ClusterId}", id);
        var (entity, _) = await RefreshClusterStatusCoreAsync(id);
        return entity.ToViewModel();
    }

    /// <summary>串行探测全部集群并回写状态;以信号量防止并发重复执行,单个集群失败仅记警告、不中断整轮,状态发生变化时按来源写审计。</summary>
    /// <param name="progress">可选进度回调,报告(当前完成数, 总数)。</param>
    /// <param name="source">触发来源,用于审计文案区分(见 <see cref="ClusterSyncSource"/>)。</param>
    /// <returns>本轮探测成功的集群数量。</returns>
    public async Task<int> RefreshAllClustersStatusAsync(IProgress<(int current, int total)>? progress = null, string source = ClusterSyncSource.Manual)
    {
        logger.LogInformation("RefreshAllClustersStatus start source={Source}", source);
        await syncGate.WaitAsync();
        try
        {
            var ids = await repo.GetAllIdsAsync();
            var total = ids.Count;
            var succeeded = 0;
            var current = 0;
            progress?.Report((0, total));
            foreach (var id in ids)
            {
                try
                {
                    var (entity, previousStatus) = await RefreshClusterStatusCoreAsync(id);
                    succeeded++;
                    if (previousStatus != entity.Status)
                    {
                        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update,
                            $"集群 {entity.Name} 状态由 {previousStatus.ToChineseText()} 变为 {entity.Status.ToChineseText()}({source})");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "RefreshAllClustersStatus id={ClusterId} failed", id);
                }
                current++;
                progress?.Report((current, total));
            }
            logger.LogInformation("RefreshAllClustersStatus done succeeded={Succeeded} of {Total}", succeeded, total);
            return succeeded;
        }
        finally
        {
            syncGate.Release();
        }
    }

    private async Task<(ClusterInfo Entity, ClusterStatus PreviousStatus)> RefreshClusterStatusCoreAsync(int id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", id);
            throw new NotFoundException($"集群 {id} 不存在");
        }

        var previousStatus = entity.Status;
        await ProbeAsync(entity);
        logger.LogInformation("RefreshClusterStatus id={ClusterId} status={Status}", id, entity.Status);
        await repo.UpdateAsync(entity);
        return (entity, previousStatus);
    }

    // ---- Private k8s helpers ----

    private KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)
    {
        if (cluster.ConnectionType == ConnectionType.KubeConfig)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(cluster.KubeConfig ?? ""));
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
        }

        return new KubernetesClientConfiguration
        {
            Host = cluster.ApiServer ?? "",
            AccessToken = cluster.Token ?? "",
            SkipTlsVerify = cluster.SkipTlsVerify
        };
    }

    private async Task ProbeAsync(ClusterInfo cluster)
    {
        logger.LogInformation("Probe cluster {ClusterName} id={ClusterId}", cluster.Name, cluster.Id);
        try
        {
            var config = BuildConfig(cluster);
            using var client = clientFactory(config);
            var versionInfo = await client.Version.GetCodeAsync();
            var nodeList = await client.CoreV1.ListNodeAsync();

            cluster.Status = ClusterStatus.Online;
            cluster.Version = versionInfo.GitVersion;
            cluster.NodeCount = nodeList.Items.Count;

            if (string.IsNullOrEmpty(cluster.ApiServer))
                cluster.ApiServer = config.Host;

            cluster.LastCheckedAt = DateTime.UtcNow;
            logger.LogInformation("Probe succeeded id={ClusterId} status={Status} version={Version} nodes={NodeCount}",
                cluster.Id, cluster.Status, cluster.Version, cluster.NodeCount);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Probe failed for cluster {ClusterName} (Id={ClusterId})", cluster.Name, cluster.Id);
            cluster.Status = ClusterStatus.Offline;
            cluster.Version = null;
            cluster.NodeCount = 0;
            cluster.LastCheckedAt = DateTime.UtcNow;
        }
    }

    private static ClusterPageQuery ToPageQuery(ClusterQueryRequest r)
    {
        // GroupId sentinel: null = no filter; 0 = ungrouped (translated by ClusterRepository to WHERE GroupId IS NULL); >0 = equality.
        string? version = r.VersionSelection switch
        {
            VersionFilterSentinel.All => null,
            VersionFilterSentinel.OnlyNull => VersionFilterSentinel.OnlyNull,
            _ => r.VersionSelection
        };

        DateTime? createdAfter = r.CreatedFrom is not null
            ? DateTime.SpecifyKind(r.CreatedFrom.Value, DateTimeKind.Utc)
            : null;
        DateTime? createdBefore = r.CreatedTo is not null
            ? DateTime.SpecifyKind(r.CreatedTo.Value, DateTimeKind.Utc)
            : null;

        return new ClusterPageQuery
        {
            GroupId = r.GroupId,
            NameContains = r.Name,
            Status = r.Status,
            Version = version,
            CreatedAfter = createdAfter,
            CreatedBefore = createdBefore,
            SortBy = r.SortBy,
            SortDescending = r.SortDescending,
            Page = Math.Max(r.Page, 1),
            PageSize = Math.Max(r.PageSize, 1)
        };
    }
}
