using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Domain.Exceptions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Application.ViewModels.Mappings;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// Namespace(命名空间)管理服务:基于所选集群的凭据实时读写 k8s Namespace。
/// 支持列表、详情、YAML 创建与删除,default 与 kube- 前缀系统命名空间受硬保护;变更成功后写审计。
/// </summary>
public class NamespaceService(IClusterRepository repo, AuditService auditService, ILogger<NamespaceService> logger, IClusterClientCache clientCache)
{
    private readonly IClusterRepository repo = repo;

    private readonly AuditService auditService = auditService;

    private readonly ILogger<NamespaceService> logger = logger;

    /// <summary>拉取集群命名空间列表;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<List<NamespaceListViewModel>> ListNamespacesAsync(int clusterId)
    {
        var entity = await repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            var list = await client.CoreV1.ListNamespaceAsync();
            return list.Items.Select(ns => ns.ToNamespaceListViewModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ListNamespaces failed clusterId={ClusterId}", clusterId);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间列表");
        }
    }

    /// <summary>读取单个命名空间详情(含标签/注解/YAML);集群不存在返回 null,K8s 失败经翻译后抛业务异常。</summary>
    public async Task<NamespaceDetailViewModel?> GetNamespaceAsync(NamespaceKeyRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId);
        if (entity is null) return null;
        var client = clientCache.GetOrCreate(entity);
        try
        {
            var ns = await client.CoreV1.ReadNamespaceAsync(request.Name);
            return ns.ToNamespaceDetailViewModel();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReadNamespace failed clusterId={ClusterId} name={Name}", request.ClusterId, request.Name);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间详情");
        }
    }

    /// <summary>以 YAML 创建命名空间,名称取自 YAML 的 metadata.name;YAML 非法或未指定名称抛 <see cref="ValidationException"/>,成功后写创建审计。</summary>
    public async Task CreateNamespaceFromYamlAsync(NamespaceCreateRequest request)
    {
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        V1Namespace body;
        try
        {
            body = KubernetesYaml.Deserialize<V1Namespace>(request.Yaml);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Deserialize YAML failed for create clusterId={ClusterId}", request.ClusterId);
            throw new ValidationException($"YAML 格式错误:{ex.Message}");
        }
        var name = body.Metadata?.Name;
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("YAML 未指定 metadata.name");
        try
        {
            await client.CoreV1.CreateNamespaceAsync(body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CreateNamespace failed clusterId={ClusterId} name={Name}", request.ClusterId, name);
            throw K8sExceptionMapper.Translate(ex, "创建命名空间");
        }
        await auditService.LogAsync(AuditCategory.Namespace, AuditAction.Create, $"命名空间: {name} @ 集群 {entity.Name}");
    }

    /// <summary>删除指定命名空间,成功后写删除审计;default 与 kube- 前缀系统命名空间受保护,直接抛 <see cref="ValidationException"/> 且不调用 K8s API。</summary>
    public async Task DeleteNamespaceAsync(NamespaceKeyRequest request)
    {
        if (IsProtected(request.Name))
            throw new ValidationException($"「{request.Name}」为系统命名空间,禁止删除");
        var entity = await repo.GetByIdAsync(request.ClusterId)
            ?? throw new NotFoundException($"集群 {request.ClusterId} 不存在");
        var client = clientCache.GetOrCreate(entity);
        try
        {
            await client.CoreV1.DeleteNamespaceAsync(request.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "DeleteNamespace failed clusterId={ClusterId} name={Name}", request.ClusterId, request.Name);
            throw K8sExceptionMapper.Translate(ex, "删除命名空间");
        }
        await auditService.LogAsync(AuditCategory.Namespace, AuditAction.Delete, $"命名空间: {request.Name} @ 集群 {entity.Name}");
    }

    /// <summary>判断是否为受保护的系统命名空间:default 或以 kube- 开头;UI 与删除校验共用同一规则。</summary>
    public static bool IsProtected(string name)
        => name == "default" || name.StartsWith("kube-", StringComparison.Ordinal);
}
