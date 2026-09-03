using k8s;
using MudBlazor;
using System.Text;
using MultiClusterMgmtSys.Data.Repositories;
using MultiClusterMgmtSys.Data.Entities;
using MultiClusterMgmtSys.Common.Enums;
using MultiClusterMgmtSys.Common.Exceptions;
using MultiClusterMgmtSys.ViewModels.Mappings;
using MultiClusterMgmtSys.Components.Clusters.ViewModels;
using MultiClusterMgmtSys.ViewModels;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Models;

namespace MultiClusterMgmtSys.Services;

public class ClusterService(ClusterRepository repo, ClusterNodeService nodeService, AuditService auditService, ILogger<ClusterService> logger)
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

    public async Task<ClusterViewModel> AddClusterAsync(ClusterCreateViewModel vm)
    {
        logger.LogInformation("AddCluster name={Name} groupId={GroupId}", vm.Name, vm.GroupId);
        var entity = new ClusterInfo
        {
            Name = vm.Name,
            GroupId = vm.GroupId,
            ApiServer = vm.ApiServer,
            ConnectionType = vm.ConnectionType,
            KubeConfig = vm.ConnectionType == ConnectionType.KubeConfig ? vm.KubeConfig : null,
            Token = vm.ConnectionType == ConnectionType.Token ? vm.Token : null,
            SkipTlsVerify = vm.SkipTlsVerify,
            Status = ClusterStatus.Unknown,
            CreatedAt = DateTime.UtcNow
        };

        await repo.AddAsync(entity);
        logger.LogInformation("AddCluster created id={ClusterId}", entity.Id);
        entity.ApplyEndpoints(vm.Endpoints);
        await ProbeAsync(entity);
        logger.LogInformation("AddCluster probed id={ClusterId} status={Status}", entity.Id, entity.Status);
        await repo.UpdateAsync(entity);
        await auditService.LogAsync(AuditCategory.Cluster, AuditAction.Create, $"集群: {entity.Name}");
        return entity.ToViewModel();
    }

    public async Task<ClusterViewModel> UpdateClusterAsync(ClusterUpdateViewModel vm)
    {
        logger.LogInformation("UpdateCluster id={ClusterId}", vm.Id);
        var entity = await repo.GetByIdAsync(vm.Id);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", vm.Id);
            throw new NotFoundException($"集群 {vm.Id} 不存在");
        }

        var configChanged = entity.ConnectionType != vm.ConnectionType
            || entity.ApiServer != vm.ApiServer
            || entity.KubeConfig != vm.KubeConfig
            || entity.Token != vm.Token
            || entity.SkipTlsVerify != vm.SkipTlsVerify;

        entity.Name = vm.Name;
        entity.GroupId = vm.GroupId;
        entity.ApiServer = vm.ApiServer;
        entity.ConnectionType = vm.ConnectionType;
        entity.SkipTlsVerify = vm.SkipTlsVerify;
        entity.KubeConfig = vm.ConnectionType == ConnectionType.KubeConfig ? vm.KubeConfig : null;
        entity.Token = vm.ConnectionType == ConnectionType.Token ? vm.Token : null;

        if (configChanged)
        {
            logger.LogInformation("UpdateCluster id={ClusterId} config changed, probing", vm.Id);
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

    public async Task UpdateClusterEndpointsAsync(int clusterId, List<ClusterEndpointEditItem> items)
    {
        logger.LogInformation("UpdateClusterEndpoints id={ClusterId} count={Count}", clusterId, items.Count);
        var entity = await repo.GetByIdAsync(clusterId);
        if (entity is null)
        {
            logger.LogWarning("Cluster {ClusterId} not found", clusterId);
            throw new NotFoundException($"集群 {clusterId} 不存在");
        }

        entity.ApplyEndpoints(items);
        await repo.UpdateAsync(entity);
        logger.LogInformation("UpdateClusterEndpoints persisted id={ClusterId}", clusterId);
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
            using var client = new Kubernetes(config);
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

        DateTime? createdAfter = r.DateRange?.Start is not null
            ? DateTime.SpecifyKind(r.DateRange.Start.Value, DateTimeKind.Utc)
            : null;
        DateTime? createdBefore = r.DateRange?.End is not null
            ? DateTime.SpecifyKind(r.DateRange.End.Value, DateTimeKind.Utc)
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
