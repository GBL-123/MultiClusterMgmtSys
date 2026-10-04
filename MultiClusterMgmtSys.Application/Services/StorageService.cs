using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 存储管理服务:PVC 可操作(列表/详情/挂载 Pod 查询/YAML 创建/删除,归属与审计与 ConfigMap 同口径),
/// PV 与 StorageClass 只读浏览(零审计;契约见 storage-management)。
/// 写操作经 <see cref="ResourceOwnershipGuard"/> 服务端强制归属(Admin 穿透、创建者本人、无主 fail-closed),
/// 读路径投影 CanOperate;创建时校验命名空间黑名单并盖章归属(契约见 k8s-resource-ownership)。
/// </summary>
public class StorageService(
    IClusterRepository repo,
    ResourceOwnershipGuard guard,
    AuditService auditService,
    IClusterClientCache clientCache,
    ILogger<StorageService> logger)
{
    private readonly IClusterRepository _repo = repo;

    private readonly ResourceOwnershipGuard _guard = guard;

    private readonly AuditService _auditService = auditService;

    private readonly IClusterClientCache _clientCache = clientCache;

    private readonly ILogger<StorageService> _logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供持久卷声明页命名空间下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var entity = await _repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var nsList = await client.CoreV1.ListNamespaceAsync();
            return nsList.Items.Select(n => n.Metadata?.Name ?? "").OrderBy(n => n).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListNamespaces failed clusterId={ClusterId}", clusterId);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间");
        }
    }

    /// <summary>查询持久卷声明列表;Namespace 为 null 时查全部命名空间。集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<StorageClaimListViewModel>> ListClaimsAsync(StorageClaimQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var list = request.Namespace is null
                ? await client.CoreV1.ListPersistentVolumeClaimForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedPersistentVolumeClaimAsync(request.Namespace);
            var isAdmin = _guard.IsAdmin();
            var userId = _guard.TryGetUserId();
            var helmIndex = await _guard.GetHelmOwnershipIndexAsync(entity.Id);
            return list.Items.Select(claim =>
            {
                var vm = claim.ToClaimListViewModel();
                vm.CanOperate = isAdmin || ResourceOwnershipPolicy.CanOperateForIndex(claim.Metadata, userId, helmIndex);
                return vm;
            }).ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ListClaims failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载持久卷声明列表");
        }
    }

    /// <summary>读取单个持久卷声明详情(含挂载此卷的 Pod 清单);详情内容对非归属者同样可见=PVC 不含机密值(契约见 storage-management)。集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<StorageClaimDetailViewModel?> GetClaimDetailAsync(StorageClaimKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = _clientCache.GetOrCreate(entity);
        V1PersistentVolumeClaim claim;
        try
        {
            claim = await client.CoreV1.ReadNamespacedPersistentVolumeClaimAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadClaim failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载持久卷声明详情");
        }

        var canOperate = await _guard.CanOperateAsync(entity.Id, claim.Metadata);
        var mountedPods = await ListMountedPodsAsync(client, request.Namespace, request.Name);
        var vm = claim.ToClaimDetailViewModel(mountedPods);
        vm.CanOperate = canOperate;
        return vm;
    }

    /// <summary>以 YAML 创建持久卷声明:强制登录身份、校验命名空间黑名单并盖章归属,命名空间取自 YAML 的 metadata.namespace;YAML 非法、kind 不符或未指定命名空间抛 <see cref="ValidationException"/>,成功后写创建审计。</summary>
    public async Task CreateClaimFromYamlAsync(StorageClaimCreateRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var identity = _guard.RequireCreator();
        var client = _clientCache.GetOrCreate(entity);
        V1PersistentVolumeClaim body;
        try
        {
            body = KubernetesYaml.Deserialize<V1PersistentVolumeClaim>(request.Yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deserialize YAML failed for create claim clusterId={ClusterId}", request.ClusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        if (!string.IsNullOrEmpty(body.Kind) && body.Kind != "PersistentVolumeClaim")
        {
            throw new ValidationException("YAML 的 kind 必须为 PersistentVolumeClaim");
        }

        var ns = body.Metadata?.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new ValidationException("YAML 未指定 metadata.namespace");
        }

        PrepareForCreation(ns, identity, body.Metadata);
        try
        {
            await client.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(body, ns);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "CreateClaim failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建持久卷声明");
        }
        await _auditService.LogAsync(AuditCategory.Storage, AuditAction.Create, $"持久卷声明: {ns}/{body.Metadata?.Name ?? "未知"} @ 集群 {entity.Name}");
    }

    /// <summary>删除指定持久卷声明:先读对象完成归属判定,成功后写删除审计;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task DeleteClaimAsync(StorageClaimKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1PersistentVolumeClaim existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedPersistentVolumeClaimAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadClaim failed for ownership check clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载持久卷声明详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);
        try
        {
            await client.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "DeleteClaim failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除持久卷声明");
        }
        await _auditService.LogAsync(AuditCategory.Storage, AuditAction.Delete, $"持久卷声明: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>查询集群全部持久卷(PV,只读浏览、零审计);集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<StorageVolumeListViewModel>> ListVolumesAsync(StorageVolumeQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var list = await client.CoreV1.ListPersistentVolumeAsync();
            return list.Items.Select(volume => volume.ToVolumeListViewModel()).ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ListVolumes failed clusterId={ClusterId}", request.ClusterId);
            throw K8sExceptionMapper.Translate(ex, "加载持久卷列表");
        }
    }

    /// <summary>读取单个持久卷详情(只读浏览、零审计);集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<StorageVolumeDetailViewModel?> GetVolumeDetailAsync(StorageVolumeKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var volume = await client.CoreV1.ReadPersistentVolumeAsync(request.Name);
            return volume.ToVolumeDetailViewModel();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadVolume failed clusterId={ClusterId} name={Name}",
                request.ClusterId, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载持久卷详情");
        }
    }

    /// <summary>查询集群全部存储类(StorageClass,只读浏览、零审计);集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<StorageClassListViewModel>> ListStorageClassesAsync(StorageClassQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var list = await client.StorageV1.ListStorageClassAsync();
            return list.Items.Select(storageClass => storageClass.ToClassListViewModel()).ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ListStorageClasses failed clusterId={ClusterId}", request.ClusterId);
            throw K8sExceptionMapper.Translate(ex, "加载存储类列表");
        }
    }

    /// <summary>列出命名空间下挂载指定持久卷声明的 Pod(spec.volumes[].persistentVolumeClaim.claimName 匹配);查询失败降级为空清单并记警告,不打断详情页。</summary>
    private async Task<List<StorageMountedPodViewModel>> ListMountedPodsAsync(IKubernetes client, string namespaceName, string claimName)
    {
        try
        {
            var pods = await client.CoreV1.ListNamespacedPodAsync(namespaceName);
            return pods.Items
                .Where(pod => pod.Spec?.Volumes?.Any(volume => volume.PersistentVolumeClaim?.ClaimName == claimName) == true)
                .Select(pod => pod.ToMountedPodViewModel())
                .ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "List mounted pods failed ns={Namespace} claim={Claim}", namespaceName, claimName);
            return [];
        }
    }

    /// <summary>创建前收拢:命名空间黑名单(Admin 不受限)+ 归属盖章(无条件覆盖用户 YAML 的归属元数据)。</summary>
    private void PrepareForCreation(string namespaceName, (int UserId, string UserName) identity, V1ObjectMeta? metadata)
    {
        if (metadata is null)
        {
            throw new ValidationException("YAML 未指定 metadata");
        }

        ResourceOwnershipPolicy.EnsureCreationTargetNamespaceAllowed(namespaceName, _guard.IsAdmin());
        ResourceOwnershipStamp.Stamp(metadata, identity.UserId, identity.UserName);
    }
}
