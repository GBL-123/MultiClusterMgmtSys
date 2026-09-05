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

public class ClusterService(ClusterRepository repo, ClusterNodeService nodeService, AuditService auditService, ILogger<ClusterService> logger, Func<KubernetesClientConfiguration, IKubernetes> clientFactory)
{
    private readonly ClusterRepository repo = repo;

    private readonly ClusterNodeService nodeService = nodeService;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<ClusterService> logger = logger;

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

    public async Task<List<string>> GetAvailableVersionsAsync()
    {
        logger.LogInformation("GetAvailableVersions");
        var versions = await repo.GetDistinctVersionsAsync();
        logger.LogInformation("GetAvailableVersions returned {Count}", versions.Count);
        return versions;
    }

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

    public async Task<ClusterViewModel> RefreshClusterStatusAsync(int id)
    {
        logger.LogInformation("RefreshClusterStatus id={ClusterId}", id);
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", id);
            throw new NotFoundException($"集群 {id} 不存在");
        }
        await ProbeAsync(entity);
        logger.LogInformation("RefreshClusterStatus id={ClusterId} status={Status}", id, entity.Status);
        await repo.UpdateAsync(entity);
        return entity.ToViewModel();
    }

    public async Task<int> RefreshAllClustersStatusAsync(IProgress<(int current, int total)>? progress = null)
    {
        logger.LogInformation("RefreshAllClustersStatus start");
        var ids = await repo.GetAllIdsAsync();
        var total = ids.Count;
        var succeeded = 0;
        var current = 0;
        progress?.Report((0, total));
        foreach (var id in ids)
        {
            try
            {
                await RefreshClusterStatusAsync(id);
                succeeded++;
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
