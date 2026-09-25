using k8s;
using k8s.Models;
using System.Collections.Concurrent;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.Models;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 集群信息服务:集群 CRUD、端点维护、连通性探测与状态刷新,以及带过滤/排序/分页的集群查询。
/// 探测失败按优雅降级处理(状态置 Offline),不向调用方抛 K8s 异常;
/// 探测成功时顺带采集节点就绪统计并追加一条健康快照,采集复用当轮节点列表结果,不额外调用 K8s。
/// </summary>
public class ClusterService(IClusterRepository repo, ClusterNodeService nodeService, AuditService auditService, ILogger<ClusterService> logger, IClusterClientCache clientCache, IClusterHealthRepository healthRepo)
{
    private static readonly SemaphoreSlim _syncGate = new(1, 1);

    private const int MaxProbeConcurrency = 4;

    private readonly IClusterRepository _repo = repo;

    private readonly ClusterNodeService _nodeService = nodeService;

    private readonly AuditService _auditService = auditService;

    private readonly ILogger<ClusterService> _logger = logger;

    /// <summary>分页查询集群列表,支持分组/名称/状态/版本/创建时间范围过滤与排序;版本筛选走哨兵语义(见 <see cref="VersionFilterSentinel"/>)。</summary>
    public async Task<PagedResult<ClusterViewModel>> GetPagedAsync(ClusterQueryRequest request)
    {
        var query = ToPageQuery(request);
        _logger.LogInformation("GetPagedClusters page={Page} size={PageSize} groupId={GroupId} nameContains={NameContains}",
            query.Page, query.PageSize, query.GroupId, query.NameContains);
        var (items, total) = await _repo.GetPagedAsync(query);
        _logger.LogInformation("GetPagedClusters returned {Count} of {Total}", items.Count, total);
        return new PagedResult<ClusterViewModel>(
            [.. items.Select(c => c.ToViewModel())],
            total);
    }

    /// <summary>查询集群表中已登记的不重复版本号列表,供版本筛选下拉使用。</summary>
    public async Task<List<string>> GetAvailableVersionsAsync()
    {
        _logger.LogInformation("GetAvailableVersions");
        var versions = await _repo.GetDistinctVersionsAsync();
        _logger.LogInformation("GetAvailableVersions returned {Count}", versions.Count);
        return versions;
    }

    /// <summary>查询单个集群详情;非 Offline 状态时实时拉取节点列表,拉取失败降级为 IsReachable=false 而不报错。集群不存在返回 null。</summary>
    /// <param name="id">集群 ID。</param>
    public async Task<ClusterDetailViewModel?> GetClusterDetailAsync(int id)
    {
        _logger.LogInformation("GetClusterDetail id={ClusterId}", id);
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", id);
            return null;
        }

        var vm = entity.ToDetailViewModel();
        if (entity.Status != ClusterStatus.Offline)
        {
            try
            {
                vm.Nodes = await _nodeService.GetClusterNodesAsync(id);
                vm.IsReachable = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load nodes for cluster {ClusterId}", id);
                vm.IsReachable = false;
            }
        }
        _logger.LogInformation("GetClusterDetail id={ClusterId} reachable={IsReachable}", id, vm.IsReachable);
        return vm;
    }

    /// <summary>查询集群的编辑用数据(含连接类型与凭据),不做连通性探测;集群不存在返回 null。</summary>
    public async Task<ClusterEditViewModel?> GetClusterForEditAsync(int id)
    {
        _logger.LogInformation("GetClusterForEdit id={ClusterId}", id);
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", id);
            return null;
        }
        return entity.ToEditViewModel();
    }

    /// <summary>新建集群:先入库,再应用端点列表并立即连通性探测(结果回写状态/版本/节点数),最后写创建审计。探测失败仅置 Offline,不抛异常。</summary>
    /// <param name="request">集群基本信息、连接凭据(KubeConfig 或 Token,按连接类型二选一)与端点列表。</param>
    public async Task<ClusterViewModel> AddClusterAsync(ClusterCreateRequest request)
    {
        _logger.LogInformation("AddCluster name={Name} groupId={GroupId}", request.Name, request.GroupId);
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

        await _repo.AddAsync(entity);
        _logger.LogInformation("AddCluster created id={ClusterId}", entity.Id);
        entity.ApplyEndpoints(request.Endpoints);
        await ProbeAsync(entity);
        _logger.LogInformation("AddCluster probed id={ClusterId} status={Status}", entity.Id, entity.Status);
        await _repo.UpdateAsync(entity);
        await _auditService.LogAsync(AuditCategory.Cluster, AuditAction.Create, $"集群: {entity.Name}");
        return entity.ToViewModel();
    }

    /// <summary>更新集群;连接配置(连接类型/ApiServer/凭据/跳过 TLS 校验)发生变化时重新探测并回写结果。集群不存在抛 <see cref="NotFoundException"/>,成功后写更新审计。</summary>
    public async Task<ClusterViewModel> UpdateClusterAsync(ClusterUpdateRequest request)
    {
        _logger.LogInformation("UpdateCluster id={ClusterId}", request.Id);
        var entity = await _repo.GetByIdAsync(request.Id);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.Id);
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
            _logger.LogInformation("UpdateCluster id={ClusterId} config changed, probing", request.Id);
            await ProbeAsync(entity);
        }

        await _repo.UpdateAsync(entity);
        await _auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群: {entity.Name}");
        return entity.ToViewModel();
    }

    /// <summary>删除集群及其端点、节点备注(级联);集群不存在时静默返回,删除成功后写删除审计。</summary>
    public async Task DeleteClusterAsync(int id)
    {
        _logger.LogInformation("DeleteCluster id={ClusterId}", id);
        var entity = await _repo.GetByIdAsync(id);
        if (entity is not null)
        {
            await _repo.DeleteAsync(id);
            await _auditService.LogAsync(AuditCategory.Cluster, AuditAction.Delete, $"集群: {entity.Name}");
        }
    }

    /// <summary>整体替换集群的管理端点(VIP/域名元数据)列表;集群不存在抛 <see cref="NotFoundException"/>,成功后写更新审计。</summary>
    public async Task UpdateClusterEndpointsAsync(ClusterEndpointsUpdateRequest request)
    {
        _logger.LogInformation("UpdateClusterEndpoints id={ClusterId} count={Count}", request.ClusterId, request.Items.Count);
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", request.ClusterId);
            throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        }

        entity.ApplyEndpoints(request.Items);
        await _repo.UpdateAsync(entity);
        _logger.LogInformation("UpdateClusterEndpoints persisted id={ClusterId}", request.ClusterId);
        await _auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update, $"集群: {entity.Name} 端点");
    }

    /// <summary>立即探测单个集群连通性并回写状态/版本/节点数;探测失败置 Offline,不抛异常。</summary>
    public async Task<ClusterViewModel> RefreshClusterStatusAsync(int id)
    {
        _logger.LogInformation("RefreshClusterStatus id={ClusterId}", id);
        var (entity, _) = await RefreshClusterStatusCoreAsync(id);
        return entity.ToViewModel();
    }

    /// <summary>有界并发探测全部集群并回写状态;以信号量防止并发重复执行,单集群失败仅记警告、不中断整轮,状态发生变化时按来源写审计;取消时先持久化已完成结果再上抛。</summary>
    /// <param name="progress">可选进度回调,报告(当前完成数, 总数)。</param>
    /// <param name="source">触发来源,用于审计文案区分(见 <see cref="ClusterSyncSource"/>)。</param>
    /// <param name="cancellationToken">停机取消令牌;取消不被视为探测失败。</param>
    /// <returns>本轮探测成功的集群数量。</returns>
    public async Task<int> RefreshAllClustersStatusAsync(IProgress<(int current, int total)>? progress = null, string source = ClusterSyncSource.Manual, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RefreshAllClustersStatus start source={Source}", source);
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            var entities = await _repo.GetAllForSyncAsync();
            var total = entities.Count;
            var previousStatuses = entities.ToDictionary(e => e.Id, e => e.Status);
            var probedIds = new ConcurrentDictionary<int, byte>();
            var succeeded = 0;
            var current = 0;
            progress?.Report((0, total));

            try
            {
                await Parallel.ForEachAsync(
                    entities,
                    new ParallelOptions { MaxDegreeOfParallelism = MaxProbeConcurrency, CancellationToken = cancellationToken },
                    async (entity, token) =>
                    {
                        await ProbeAsync(entity, token);
                        probedIds.TryAdd(entity.Id, 0);
                        Interlocked.Increment(ref succeeded);
                        progress?.Report((Interlocked.Increment(ref current), total));
                    });
            }
            catch (OperationCanceledException)
            {
                await PersistProbedAsync([.. entities.Where(e => probedIds.ContainsKey(e.Id))], previousStatuses, source);
                throw;
            }

            await PersistProbedAsync(entities, previousStatuses, source);
            _logger.LogInformation("RefreshAllClustersStatus done succeeded={Succeeded} of {Total}", succeeded, total);
            return succeeded;
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task PersistProbedAsync(IEnumerable<ClusterInfo> entities, IReadOnlyDictionary<int, ClusterStatus> previousStatuses, string source)
    {
        foreach (var entity in entities)
        {
            try
            {
                await _repo.UpdateAsync(entity);
                if (previousStatuses[entity.Id] != entity.Status)
                {
                    await _auditService.LogAsync(AuditCategory.Cluster, AuditAction.Update,
                        $"集群 {entity.Name} 状态由 {previousStatuses[entity.Id].ToChineseText()} 变为 {entity.Status.ToChineseText()}({source})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RefreshAllClustersStatus persist id={ClusterId} failed", entity.Id);
            }
        }
    }

    private async Task<(ClusterInfo Entity, ClusterStatus PreviousStatus)> RefreshClusterStatusCoreAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
        {
            _logger.LogWarning("Cluster {ClusterId} not found", id);
            throw new NotFoundException($"集群 {id} 不存在");
        }

        var previousStatus = entity.Status;
        await ProbeAsync(entity);
        _logger.LogInformation("RefreshClusterStatus id={ClusterId} status={Status}", id, entity.Status);
        await _repo.UpdateAsync(entity);
        return (entity, previousStatus);
    }

    // ---- Private k8s helpers ----

    private async Task ProbeAsync(ClusterInfo cluster, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Probe cluster {ClusterName} id={ClusterId}", cluster.Name, cluster.Id);
        try
        {
            var client = clientCache.GetOrCreate(cluster);
            var versionInfo = await client.Version.GetCodeAsync(cancellationToken);
            var nodeList = await client.CoreV1.ListNodeAsync(cancellationToken: cancellationToken);

            cluster.Status = ClusterStatus.Online;
            cluster.Version = versionInfo.GitVersion;
            cluster.NodeCount = nodeList.Items.Count;

            if (string.IsNullOrEmpty(cluster.ApiServer))
                cluster.ApiServer = clientCache.ResolveApiServer(cluster);

            cluster.LastCheckedAt = DateTime.UtcNow;

            await TryAppendHealthSnapshotAsync(cluster, nodeList.Items);

            _logger.LogInformation("Probe succeeded id={ClusterId} status={Status} version={Version} nodes={NodeCount}",
                cluster.Id, cluster.Status, cluster.Version, cluster.NodeCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Probe canceled cluster {ClusterName} id={ClusterId}", cluster.Name, cluster.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Probe failed for cluster {ClusterName} (Id={ClusterId})", cluster.Name, cluster.Id);
            cluster.Status = ClusterStatus.Offline;
            cluster.Version = null;
            cluster.NodeCount = 0;
            cluster.LastCheckedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 探测成功后追加一条节点健康快照;写入失败仅记警告,不改变本轮探测结论——
    /// 本地落库异常不应把可达集群误判为离线,与审计写入的静默降级口径一致。
    /// </summary>
    /// <param name="cluster">本轮探测成功的集群(已回写状态)。</param>
    /// <param name="nodes">当轮节点列表调用返回的节点集合,复用其结果不额外调用 K8s。</param>
    private async Task TryAppendHealthSnapshotAsync(ClusterInfo cluster, IList<V1Node> nodes)
    {
        try
        {
            await healthRepo.AddAsync(BuildHealthSnapshot(cluster.Id, nodes));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Append cluster health snapshot failed id={ClusterId}", cluster.Id);
        }
    }

    /// <summary>
    /// 按当轮节点列表构建健康快照:就绪 = Ready 条件状态为 True,
    /// 未就绪 = Ready 条件为 False/Unknown 或缺失该条件,两者之和恒等于节点总数。
    /// </summary>
    /// <param name="clusterId">所属集群 Id。</param>
    /// <param name="nodes">当轮节点集合。</param>
    /// <returns>待追加的健康快照。</returns>
    private static ClusterHealthSnapshot BuildHealthSnapshot(int clusterId, IList<V1Node> nodes)
    {
        var readyNodes = nodes.Count(IsNodeReady);
        return new ClusterHealthSnapshot
        {
            ClusterId = clusterId,
            CapturedAt = DateTime.UtcNow,
            TotalNodes = nodes.Count,
            ReadyNodes = readyNodes,
            NotReadyNodes = nodes.Count - readyNodes
        };
    }

    /// <summary>判断节点是否就绪:存在 Ready 条件且状态为 True;与节点列表页的就绪判定口径一致。</summary>
    /// <param name="node">待判定的节点。</param>
    /// <returns>就绪为 true,否则 false。</returns>
    private static bool IsNodeReady(V1Node node)
    {
        var readyCondition = node.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready");
        return readyCondition is not null && readyCondition.Status == "True";
    }

    private static ClusterPageQuery ToPageQuery(ClusterQueryRequest r)
    {
        // GroupId sentinel: null = no filter; 0 = ungrouped (translated by IClusterRepository to WHERE GroupId IS NULL); >0 = equality.
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
