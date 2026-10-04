using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.Common.Ownership;
using MultiClusterMgmtSys.Application.Common.Secrets;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// Secret(密钥)管理服务:基于所选集群的凭据实时读写 k8s Secret。
/// 支持按命名空间查询、YAML 创建/更新(占位符 <c>&lt;REDACTED:key&gt;</c> 保留现值)、删除,成功后写审计;
/// 值默认掩码,「查看明文」逐键揭示并写审计(只记录键名,不记录值;契约见 secrets-page)。
/// 写操作经 <see cref="ResourceOwnershipGuard"/> 服务端强制归属(Admin 穿透、创建者本人、无主 fail-closed),
/// 读路径投影 CanOperate;创建时校验命名空间黑名单并盖章归属(契约见 k8s-resource-ownership)。
/// </summary>
public class SecretService(
    IClusterRepository repo,
    ResourceOwnershipGuard guard,
    AuditService auditService,
    IClusterClientCache clientCache,
    ILogger<SecretService> logger)
{
    private readonly IClusterRepository _repo = repo;

    private readonly ResourceOwnershipGuard _guard = guard;

    private readonly AuditService _auditService = auditService;

    private readonly IClusterClientCache _clientCache = clientCache;

    private readonly ILogger<SecretService> _logger = logger;

    /// <summary>拉取集群命名空间列表(升序),供密钥页命名空间下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
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

    /// <summary>查询密钥列表;Namespace 为 null 时查全部命名空间。集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<SecretListViewModel>> ListSecretsAsync(SecretQueryRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        try
        {
            var list = request.Namespace is null
                ? await client.CoreV1.ListSecretForAllNamespacesAsync()
                : await client.CoreV1.ListNamespacedSecretAsync(request.Namespace);
            var isAdmin = _guard.IsAdmin();
            var userId = _guard.TryGetUserId();
            var helmIndex = await _guard.GetHelmOwnershipIndexAsync(entity.Id);
            return list.Items.Select(secret =>
            {
                var vm = secret.ToSecretListViewModel();
                vm.CanOperate = isAdmin || ResourceOwnershipPolicy.CanOperateForIndex(secret.Metadata, userId, helmIndex);
                return vm;
            }).ToList();
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ListSecrets failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, request.Namespace);
            throw K8sExceptionMapper.Translate(ex, "加载密钥列表");
        }
    }

    /// <summary>读取单个密钥详情(值掩码 + base64 原文 YAML);不可操作者(受限态)不投影键与 YAML。集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<SecretDetailViewModel?> GetSecretDetailAsync(SecretKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = _clientCache.GetOrCreate(entity);
        V1Secret secret;
        try
        {
            secret = await client.CoreV1.ReadNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadSecret failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载密钥详情");
        }

        var canOperate = await _guard.CanOperateAsync(entity.Id, secret.Metadata);
        var vm = secret.ToSecretDetailViewModel(canOperate);
        vm.CanOperate = canOperate;
        return vm;
    }

    /// <summary>查看指定键的明文:先读对象完成归属判定,仅文本键可揭示;成功后写查看审计(只记录键名,不记录值)。</summary>
    public async Task<string> RevealSecretKeyAsync(SecretRevealRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1Secret existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadSecret failed for reveal clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载密钥详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);

        var data = existing.Data ?? new Dictionary<string, byte[]>();
        if (!data.TryGetValue(request.Key, out var bytes))
        {
            throw new NotFoundException($"密钥「{request.Name}」中不存在键「{request.Key}」");
        }

        if (!SecretRedaction.IsUtf8Text(bytes))
        {
            throw new ValidationException($"「{request.Key}」不是文本内容，无法查看明文");
        }

        var plaintext = Encoding.UTF8.GetString(bytes);
        await _auditService.LogAsync(AuditCategory.Secret, AuditAction.View, $"密钥: {request.Namespace}/{request.Name}「{request.Key}」 @ 集群 {entity.Name}");
        return plaintext;
    }

    /// <summary>以 YAML 创建密钥:强制登录身份、校验命名空间黑名单并盖章归属,命名空间取自 YAML 的 metadata.namespace;YAML 非法、kind 不符或未指定命名空间抛 <see cref="ValidationException"/>,成功后写创建审计。</summary>
    public async Task CreateSecretFromYamlAsync(SecretCreateRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var identity = _guard.RequireCreator();
        var client = _clientCache.GetOrCreate(entity);
        SecretYamlBody shim;
        try
        {
            shim = KubernetesYaml.Deserialize<SecretYamlBody>(request.Yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deserialize YAML failed for create clusterId={ClusterId}", request.ClusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        if (!string.IsNullOrEmpty(shim.Kind) && shim.Kind != "Secret")
        {
            throw new ValidationException("YAML 的 kind 必须为 Secret");
        }

        var ns = shim.Metadata?.NamespaceProperty;
        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new ValidationException("YAML 未指定 metadata.namespace");
        }

        PrepareForCreation(ns, identity, shim.Metadata);
        var body = BuildCreateBody(shim);
        try
        {
            await client.CoreV1.CreateNamespacedSecretAsync(body, ns);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "CreateSecret failed clusterId={ClusterId} ns={Namespace}", request.ClusterId, ns);
            throw K8sExceptionMapper.Translate(ex, "创建密钥");
        }
        await _auditService.LogAsync(AuditCategory.Secret, AuditAction.Create, $"密钥: {ns}/{shim.Metadata?.Name ?? "未知"} @ 集群 {entity.Name}");
    }

    /// <summary>读取编辑页数据:先读对象完成归属判定,YAML 以占位符回显(不回显明文/base64)。</summary>
    public async Task<SecretEditViewModel> GetSecretForEditAsync(SecretKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1Secret existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadSecret failed for edit clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载密钥详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);
        var edit = existing.ToSecretEditViewModel();
        edit.CanOperate = true;
        return edit;
    }

    /// <summary>以 YAML 更新既有密钥:先读对象完成归属判定,占位符保留服务器现值、新值覆盖、未提交键删除,替换体保持服务器侧元数据(归属标签不变);YAML 非法抛 <see cref="ValidationException"/>,成功后写更新审计。</summary>
    public async Task UpdateSecretFromYamlAsync(SecretUpdateRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        SecretYamlBody shim;
        try
        {
            shim = KubernetesYaml.Deserialize<SecretYamlBody>(request.Yaml);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Deserialize YAML failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        if (!string.IsNullOrEmpty(shim.Kind) && shim.Kind != "Secret")
        {
            throw new ValidationException("YAML 的 kind 必须为 Secret");
        }

        V1Secret existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadSecret failed for update clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载密钥详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);

        var merged = SecretRedaction.MergeSubmitted(shim, existing);
        try
        {
            existing.Data = merged;
            existing.StringData = null;
            existing.Type = string.IsNullOrWhiteSpace(shim.Type) ? existing.Type : shim.Type;
            await client.CoreV1.ReplaceNamespacedSecretAsync(existing, request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReplaceSecret failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "保存密钥");
        }
        await _auditService.LogAsync(AuditCategory.Secret, AuditAction.Update, $"密钥: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>删除指定密钥:先读对象完成归属判定,成功后写删除审计;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task DeleteSecretAsync(SecretKeyRequest request)
    {
        var entity = await _repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = _clientCache.GetOrCreate(entity);
        V1Secret existing;
        try
        {
            existing = await client.CoreV1.ReadNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "ReadSecret failed for ownership check clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载密钥详情");
        }
        await _guard.RequireOperateAsync(request.ClusterId, request.Namespace, request.Name, existing.Metadata);
        try
        {
            await client.CoreV1.DeleteNamespacedSecretAsync(request.Name, request.Namespace);
        }
        catch (Exception ex) when (ex is KubernetesException or HttpOperationException or TaskCanceledException or OperationCanceledException or HttpRequestException)
        {
            _logger.LogWarning(ex, "DeleteSecret failed clusterId={ClusterId} ns={Namespace} name={Name}",
                request.ClusterId, request.Namespace, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除密钥");
        }
        await _auditService.LogAsync(AuditCategory.Secret, AuditAction.Delete, $"密钥: {request.Namespace}/{request.Name} @ 集群 {entity.Name}");
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

    /// <summary>把提交的 YAML 载体转成 V1Secret:创建时禁用占位符,data 按 base64 校验,stringData 按明文预编码进 data。</summary>
    private static V1Secret BuildCreateBody(SecretYamlBody shim)
    {
        foreach (var (key, value) in shim.Data ?? new Dictionary<string, string>())
        {
            if (SecretRedaction.IsPlaceholder(value))
            {
                throw new ValidationException($"「{key}」不能在创建时使用占位符");
            }
        }

        foreach (var (key, value) in shim.StringData ?? new Dictionary<string, string>())
        {
            if (SecretRedaction.IsPlaceholder(value))
            {
                throw new ValidationException($"「{key}」不能在创建时使用占位符");
            }
        }

        var emptyServer = new V1Secret { Data = new Dictionary<string, byte[]>() };
        var merged = SecretRedaction.MergeSubmitted(shim, emptyServer);
        return new V1Secret
        {
            ApiVersion = "v1",
            Kind = "Secret",
            Metadata = shim.Metadata,
            Type = string.IsNullOrWhiteSpace(shim.Type) ? "Opaque" : shim.Type,
            Data = merged
        };
    }
}
