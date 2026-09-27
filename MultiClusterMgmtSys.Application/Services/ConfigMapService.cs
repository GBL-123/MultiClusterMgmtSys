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
/// ConfigMap(配置)管理服务:基于所选集群的凭据实时读写 k8s ConfigMap。
/// 支持按命名空间查询、YAML 创建/更新(仅覆盖 data/binaryData)、删除,成功后写审计。
/// 写操作经 <see cref="ResourceOwnershipGuard"/> 服务端强制归属(Admin 穿透、创建者本人、无主 fail-closed),
/// 读路径投影 CanOperate;创建时校验命名空间黑名单并盖章归属(契约见 k8s-resource-ownership)。
/// </summary>
public class ConfigMapService(
    IClusterRepository repo,
    ResourceOwnershipGuard guard,
    AuditService auditService,
    IClusterClientCache clientCache,
    ILogger<ConfigMapService> logger)
{
    private readonly IClusterRepository _repo = repo;

    private readonly ResourceOwnershipGuard _guard = guard;

    private readonly AuditService _auditService = auditService;

    private readonly IClusterClientCache _clientCache = clientCache;

    private readonly ILogger<ConfigMapService> _logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供配置页命名空间下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
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

    /// <summary>查询配置列表;Namespace 为 null 时查全部命名空间。集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(ConfigMapQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var list = request.Namespace is null
                ? await client.CoreV1.ListConfigMapForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedConfigMapAsync(request.Namespace);
            var isAdmin = _guard.IsAdmin();
            var userId = _guard.TryGetUserId();
            var helmIndex = await _guard.GetHelmOwnershipIndexAsync(entity.Id);
            return list.Items.Select(cm =>
            {
                var vm = cm.ToConfigMapListViewModel();
                vm.CanOperate = isAdmin || ResourceOwnershipPolicy.CanOperateForIndex(cm.Metadata, userId, helmIndex);
                return vm;
            }).ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ListConfigMaps failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载配置列表");
        }
    }

    /// <summary>读取单个配置详情(含 data/binaryData);集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<ConfigMapDetailViewModel?> GetConfigMapAsync(ConfigMapKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = _clientCache.GetOrCreate(entity);
        V1ConfigMap cm;
        try
        {
            cm = await client.CoreV1.ReadNamespacedConfigMapAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载配置详情");
        }

        var vm = cm.ToConfigMapDetailViewModel();
        vm.CanOperate = await _guard.CanOperateAsync(entity.Id, cm.Metadata);
        return vm;
    }

    /// <summary>删除指定配置:先读对象完成归属判定,成功后写删除审计;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task DeleteConfigMapAsync(ConfigMapKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1ConfigMap existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedConfigMapAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadConfigMap failed for ownership check clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载配置详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);
        try
        {
            await client.CoreV1.DeleteNamespacedConfigMapAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "DeleteConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除配置");
        }
        await _auditService.LogAsync(AuditCategory.Configmap, AuditAction.Delete, $"配置: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>以 YAML 更新既有配置:先读对象完成归属判定,反序列化后仅覆盖 data/binaryData 字段(归属元数据保持服务器侧),携带服务器最新对象替换提交;YAML 非法抛 <see cref="ValidationException"/>,成功后写更新审计。</summary>
    public async Task UpdateConfigMapFromYamlAsync(ConfigMapUpdateRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1ConfigMap deserialized;
        try
        {
            deserialized = KubernetesYaml.Deserialize<V1ConfigMap>(request.Yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deserialize YAML failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        V1ConfigMap existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedConfigMapAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadConfigMap failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载配置详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);

        try
        {
            existing.Data = deserialized.Data;
            existing.BinaryData = deserialized.BinaryData;
            await client.CoreV1.ReplaceNamespacedConfigMapAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReplaceConfigMap failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存配置");
        }
        await _auditService.LogAsync(AuditCategory.Configmap, AuditAction.Update, $"配置: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>以 YAML 创建配置:强制登录身份、校验命名空间黑名单并盖章归属,命名空间取自 YAML 的 metadata.namespace;YAML 非法或未指定命名空间抛 <see cref="ValidationException"/>,成功后写创建审计。</summary>
    public async Task CreateConfigMapFromYamlAsync(ConfigMapCreateRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var identity = _guard.RequireCreator();
        var client = _clientCache.GetOrCreate(entity);
        V1ConfigMap body;
        try
        {
            body = KubernetesYaml.Deserialize<V1ConfigMap>(request.Yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deserialize YAML failed for create clusterId={ClusterId}", request.ClusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        var ns = body.Metadata?.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(ns))
            throw new ValidationException("YAML 未指定 metadata.namespace");
        PrepareForCreation(ns, identity, body.Metadata);
        try
        {
            await client.CoreV1.CreateNamespacedConfigMapAsync(body, ns);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "CreateConfigMap failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建配置");
        }
        await _auditService.LogAsync(AuditCategory.Configmap, AuditAction.Create, $"配置: {ns}/{body.Metadata?.Name ?? "未知"} @ 集群 {entity.Name}");
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