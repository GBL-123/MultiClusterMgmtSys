using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using k8s;
using k8s.Models;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Yaml;
using MultiClusterMgmtSys.Application.Enums;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// 舰队模板变量提取 / 渲染 / 残留扫描:模板占位符为双花括号 <c>{{&#64;var}}</c> 风格,
/// 变量名允许字母、数字、下划线、短横线与点;渲染是纯文本替换——变量值为空串时替换为空串,
/// 缺失变量保留占位符原文以便残留扫描捕获;<c>{{if ...}}</c> 等非法结构以残留形态被拒(不做模板引擎解析)。
/// </summary>
public static class FleetTemplateVariables
{
    /// <summary>占位符正则:双花括号包裹的合法变量名([A-Za-z0-9_.-]+)。</summary>
    private static readonly Regex _PlaceholderRegex = new(@"{{([A-Za-z0-9_.-]+)}}", RegexOptions.Compiled);

    /// <summary>残留扫描正则:渲染文本中仍存在的任意双花括号片段。</summary>
    private static readonly Regex _ResidualRegex = new(@"{{.*?}}", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>从模板文本提取占位符变量名,按首次出现顺序去重;空文本返回空列表。</summary>
    /// <param name="templateYaml">模板 YAML 原文。</param>
    /// <returns>变量名列表(去重保序)。</returns>
    public static IReadOnlyList<string> Extract(string templateYaml)
    {
        if (string.IsNullOrEmpty(templateYaml))
        {
            return [];
        }

        return [.. _PlaceholderRegex.Matches(templateYaml).Select(m => m.Groups[1].Value).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>按取值逐占位符纯文本替换;取值缺失时保留占位符原文(||null 值等价替换为空串)。</summary>
    /// <param name="templateYaml">模板 YAML 原文。</param>
    /// <param name="values">变量取值(键与占位符名一致)。</param>
    /// <returns>替换后的渲染文本。</returns>
    public static string Render(string templateYaml, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(templateYaml))
        {
            return "";
        }

        return _PlaceholderRegex.Replace(templateYaml, m =>
            values.TryGetValue(m.Groups[1].Value, out var value) ? value ?? "" : m.Value);
    }

    /// <summary>扫描文本中的残留双花括号片段(含 <c>{{if ...}}</c> 等非法结构),按首次出现顺序去重。</summary>
    /// <param name="text">待扫描文本。</param>
    /// <returns>残留占位符片段列表(去重保序)。</returns>
    public static IReadOnlyList<string> FindResiduals(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        return [.. _ResidualRegex.Matches(text).Select(m => m.Value).Distinct(StringComparer.Ordinal)];
    }
}

/// <summary>
/// 舰队模板下发服务(契约见 fleet-templates;仅 Admin):以一份含占位符的 YAML 模板为源,按「集群 × 变量」矩阵
/// 逐集群纯文本渲染,并逐集群与该集群现有同名对象(剥管后)对照,产出「新建 / 更新 / 一致 / 获取失败」判定;
/// 下发阶段重新校验并经既有 WorkloadService / ConfigMapService 创建与编辑路径写入,逐集群有界并发、失败隔离,
/// 每个执行成功的集群补写一条「舰队下发」审计;模板与矩阵零持久化。
/// </summary>
public class FleetTemplateService(
    IClusterRepository repo,
    IHttpContextAccessor httpContextAccessor,
    IServiceScopeFactory scopeFactory,
    ILogger<FleetTemplateService> logger)
{
    /// <summary>并行下发的最大并发度(与 Helm 批量下发一致)。</summary>
    private const int MaxFleetConcurrency = 4;

    private readonly IClusterRepository _repo = repo;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    private readonly ILogger<FleetTemplateService> _logger = logger;

    /// <summary>提取模板中的占位符变量名(单原语参数豁免),供页面在填值阶段提示待填列。</summary>
    /// <param name="templateYaml">模板 YAML 原文。</param>
    /// <returns>变量名列表(去重保序)。</returns>
    public IReadOnlyList<string> ExtractVariables(string templateYaml) => FleetTemplateVariables.Extract(templateYaml);

    /// <summary>
    /// 预览:服务端强制 Admin、校验目标集群与变量矩阵完整性并逐集群渲染,再并行读取各集群现有同名对象,
    /// 产出「新建 / 更新 / 一致 / 获取失败」判定行;单集群预取失败不中断其余集群。
    /// </summary>
    /// <param name="request">舰队预览请求(模板、目标集群与变量矩阵)。</param>
    /// <param name="progress">进度回调(已完成数,总数),可空。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>逐集群预览行(与目标集群顺序一致)。</returns>
    public async Task<FleetTemplatePreviewResultViewModel> PreviewAsync(
        FleetTemplatePreviewRequest request,
        IProgress<(int Current, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PreviewFleetTemplates start clusters={Clusters}", request.ClusterIds.Count);
        var units = await ValidateAndRenderAsync(request.TemplateYaml, request.ClusterIds, request.Variables, cancellationToken);

        var items = new ConcurrentDictionary<int, FleetTemplatePreviewItemViewModel>();
        var completed = 0;
        await Parallel.ForEachAsync(units, BuildParallelOptions(cancellationToken), async (unit, token) =>
        {
            var item = await PreviewClusterAsync(unit, token);
            items[unit.ClusterId] = item;
            progress?.Report((Interlocked.Increment(ref completed), units.Count));
        });

        var result = new FleetTemplatePreviewResultViewModel { Items = [.. units.Select(unit => items[unit.ClusterId])] };
        _logger.LogInformation(
            "PreviewFleetTemplates done create={Create} update={Update} identical={Identical} fetchFailed={FetchFailed}",
            result.Items.Count(item => item.Action == FleetTemplateAction.Create),
            result.Items.Count(item => item.Action == FleetTemplateAction.Update),
            result.Items.Count(item => item.Action == FleetTemplateAction.Identical),
            result.Items.Count(item => item.Action == FleetTemplateAction.FetchFailed));
        return result;
    }

    /// <summary>
    /// 下发:重新执行全部服务端校验(不信任页面预览),逐集群重新预取现值并判定后,
    /// 「新建 / 更新」走既有创建与编辑路径,「一致」「获取失败」跳过零写零审计;逐集群写「舰队下发」审计。
    /// </summary>
    /// <param name="request">舰队下发请求(与预览同形)。</param>
    /// <param name="progress">进度回调(已完成数,总数),可空。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>逐集群结果行与成功/失败计数。</returns>
    public async Task<FleetTemplateDeployResultViewModel> DeployToFleetAsync(
        FleetTemplateDeployRequest request,
        IProgress<(int Current, int Total)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("DeployFleetTemplates start clusters={Clusters}", request.ClusterIds.Count);
        var units = await ValidateAndRenderAsync(request.TemplateYaml, request.ClusterIds, request.Variables, cancellationToken);

        var items = new ConcurrentDictionary<int, FleetTemplateDeployItemViewModel>();
        var completed = 0;
        await Parallel.ForEachAsync(units, BuildParallelOptions(cancellationToken), async (unit, token) =>
        {
            var item = await DeployClusterAsync(unit, token);
            items[unit.ClusterId] = item;
            progress?.Report((Interlocked.Increment(ref completed), units.Count));
        });

        var result = new FleetTemplateDeployResultViewModel { Items = [.. units.Select(unit => items[unit.ClusterId])] };
        _logger.LogInformation(
            "DeployFleetTemplates done success={Success} failure={Failure}",
            result.SuccessCount,
            result.FailureCount);
        return result;
    }

    /// <summary>校验 Admin、目标集群与变量矩阵、渲染、逐集群解析并规范化 YAML(单文档、五族、metadata.name 必填)。</summary>
    /// <param name="templateYaml">模板 YAML 原文。</param>
    /// <param name="clusterIds">目标集群 Id 集合。</param>
    /// <param name="variables">逐集群变量矩阵。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>逐集群渲染单元(含剥管后的渲染 YAML),顺序与目标集群顺序一致。</returns>
    private async Task<List<FleetRenderedUnit>> ValidateAndRenderAsync(
        string templateYaml,
        IReadOnlyList<int> clusterIds,
        IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> variables,
        CancellationToken cancellationToken)
    {
        if (!IsAdmin())
        {
            _logger.LogWarning("Fleet template denied: non-admin invoked");
            throw new PermissionException("仅管理员可以使用舰队模板下发");
        }

        if (clusterIds is not { Count: > 0 })
        {
            throw new ValidationException("请选择至少一个目标集群");
        }

        var allClusters = await _repo.GetAllForDashboardAsync();
        var nameByCluster = allClusters.ToDictionary(cluster => cluster.Id, cluster => cluster.Name);
        var selectedIds = clusterIds.Distinct().ToList();
        var missingIds = selectedIds.Where(id => !nameByCluster.ContainsKey(id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new NotFoundException($"目标集群不存在：{string.Join("、", missingIds)}");
        }

        var extracted = FleetTemplateVariables.Extract(templateYaml);
        var errors = new List<string>();
        var rendered = new Dictionary<int, string>();
        foreach (var clusterId in selectedIds)
        {
            var clusterName = nameByCluster[clusterId];
            var clusterVars = variables.TryGetValue(clusterId, out var supplied)
                ? supplied
                : new Dictionary<string, string>();
            foreach (var name in extracted.Where(name => !clusterVars.ContainsKey(name)))
            {
                errors.Add($"集群 {clusterName} 缺变量 {name}");
            }

            var renderedYaml = FleetTemplateVariables.Render(templateYaml, clusterVars);
            foreach (var residual in FleetTemplateVariables.FindResiduals(renderedYaml))
            {
                errors.Add($"集群 {clusterName} 存在无法解析的占位符 {residual}");
            }

            rendered[clusterId] = renderedYaml;
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join(";", errors));
        }

        var units = new List<FleetRenderedUnit>();
        foreach (var clusterId in selectedIds)
        {
            var clusterName = nameByCluster[clusterId];
            var renderedYaml = rendered[clusterId];
            var document = ParseSingleDocument(renderedYaml, clusterName);
            var (family, metadata) = ClassifyDocument(document);
            if (family is null)
            {
                throw new ValidationException(
                    $"集群 {clusterName} 的模板 kind 不受支持，仅支持 Deployment、StatefulSet、DaemonSet、ReplicaSet、ConfigMap 五类资源");
            }

            if (string.IsNullOrEmpty(metadata?.Name))
            {
                throw new ValidationException($"集群 {clusterName} 的渲染 YAML 缺少 metadata.name");
            }

            var namespaceName = string.IsNullOrEmpty(metadata!.NamespaceProperty) ? "default" : metadata.NamespaceProperty;
            units.Add(new FleetRenderedUnit
            {
                ClusterId = clusterId,
                ClusterName = clusterName,
                Kind = family.Value,
                Name = metadata.Name,
                Namespace = namespaceName,
                RenderedYaml = ServerYamlSanitizer.Sanitize(renderedYaml, family.Value)
            });
        }

        return units;
    }

    /// <summary>解析渲染文本为单文档(node 数为 1);空文档 / 多文档 / 语法错误 / 未知 kind 各自给出中文校验错误。</summary>
    /// <param name="renderedYaml">该集群的渲染文本。</param>
    /// <param name="clusterName">集群名(错误信息上下文)。</param>
    /// <returns>已按 kind 映射的强类型文档对象。</returns>
    private static object ParseSingleDocument(string renderedYaml, string clusterName)
    {
        List<object> documents;
        try
        {
            documents = KubernetesYaml.LoadAllFromString(renderedYaml);
        }
        catch (KeyNotFoundException)
        {
            throw new ValidationException(
                $"集群 {clusterName} 的模板 kind 不受支持，仅支持 Deployment、StatefulSet、DaemonSet、ReplicaSet、ConfigMap 五类资源");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ValidationException($"YAML 格式错误：{ex.Message}");
        }

        if (documents.Count == 0)
        {
            throw new ValidationException($"集群 {clusterName} 的渲染 YAML 未包含任何文档");
        }

        if (documents.Count > 1)
        {
            throw new ValidationException($"集群 {clusterName} 的模板须为单文档 YAML，当前包含 {documents.Count} 个文档");
        }

        return documents[0];
    }

    /// <summary>识别文档对象所属资源族并提取 metadata;五族之外返回 null 元组。</summary>
    /// <param name="document">文档对象。</param>
    /// <returns>(资源族或 null,文档 metadata 或 null)。</returns>
    private static (CompareKind? Family, V1ObjectMeta? Metadata) ClassifyDocument(object document) => document switch
    {
        V1ConfigMap configMap => (CompareKind.ConfigMap, configMap.Metadata),
        V1Deployment deployment => (CompareKind.Deployment, deployment.Metadata),
        V1StatefulSet statefulSet => (CompareKind.StatefulSet, statefulSet.Metadata),
        V1DaemonSet daemonSet => (CompareKind.DaemonSet, daemonSet.Metadata),
        V1ReplicaSet replicaSet => (CompareKind.ReplicaSet, replicaSet.Metadata),
        _ => (null, null)
    };

    /// <summary>预览单集群:从独立作用域解析读路径服务,预取现值(剥管)并产出判定行;失败隔离为「获取失败」行。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>该集群的预览行。</returns>
    private async Task<FleetTemplatePreviewItemViewModel> PreviewClusterAsync(FleetRenderedUnit unit, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var current = await FetchCurrentYamlAsync(
                unit,
                scope.ServiceProvider.GetRequiredService<ConfigMapService>(),
                scope.ServiceProvider.GetRequiredService<WorkloadService>(),
                cancellationToken);
            if (!current.Fetched)
            {
                return PreviewFailure(unit, current.Message);
            }

            return JudgePreview(unit, current.Yaml);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fleet template preview failed clusterId={ClusterId} family={Family}", unit.ClusterId, unit.Kind);
            return PreviewFailure(unit, $"获取现有{unit.Kind.ToDisplayText()}资源失败，请检查集群可达性");
        }
    }

    /// <summary>按该集群现值 (剥管后) 与渲染值产出判定行(新建 / 更新 / 一致)。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="currentYaml">该集群现值 YAML(对象不存在为 null)。</param>
    /// <returns>该集群的预览行。</returns>
    private static FleetTemplatePreviewItemViewModel JudgePreview(FleetRenderedUnit unit, string? currentYaml)
    {
        var sanitizedCurrent = currentYaml is null ? null : ServerYamlSanitizer.Sanitize(currentYaml, unit.Kind);
        var identical = sanitizedCurrent is not null && IsLineSequenceEqual(sanitizedCurrent, unit.RenderedYaml);
        return new FleetTemplatePreviewItemViewModel
        {
            ClusterId = unit.ClusterId,
            ClusterName = unit.ClusterName,
            Action = sanitizedCurrent is null ? FleetTemplateAction.Create : identical ? FleetTemplateAction.Identical : FleetTemplateAction.Update,
            CurrentYaml = sanitizedCurrent,
            RenderedYaml = unit.RenderedYaml,
            Message = ""
        };
    }

    /// <summary>构建「获取失败」预览行(现值不可得时其余集群不受影响)。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="message">中文失败原因。</param>
    /// <returns>该集群的「获取失败」预览行。</returns>
    private static FleetTemplatePreviewItemViewModel PreviewFailure(FleetRenderedUnit unit, string message) => new()
    {
        ClusterId = unit.ClusterId,
        ClusterName = unit.ClusterName,
        Action = FleetTemplateAction.FetchFailed,
        CurrentYaml = null,
        RenderedYaml = unit.RenderedYaml,
        Message = message
    };

    /// <summary>下发单集群:重新预取现值判定 → 一致跳过 / 新建走创建路径 / 更新走编辑路径,成功后同作用域补写「舰队下发」审计;失败隔离为失败行。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>该集群的下发结果行。</returns>
    private async Task<FleetTemplateDeployItemViewModel> DeployClusterAsync(FleetRenderedUnit unit, CancellationToken cancellationToken)
    {
        var item = new FleetTemplateDeployItemViewModel { ClusterId = unit.ClusterId, ClusterName = unit.ClusterName };
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var configMapService = scope.ServiceProvider.GetRequiredService<ConfigMapService>();
            var workloadService = scope.ServiceProvider.GetRequiredService<WorkloadService>();
            var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

            var current = await FetchCurrentYamlAsync(unit, configMapService, workloadService, cancellationToken);
            if (!current.Fetched)
            {
                item.Action = FleetTemplateAction.FetchFailed;
                item.Message = current.Message;
                return item;
            }

            var sanitizedCurrent = current.Yaml is null ? null : ServerYamlSanitizer.Sanitize(current.Yaml, unit.Kind);
            if (sanitizedCurrent is not null && IsLineSequenceEqual(sanitizedCurrent, unit.RenderedYaml))
            {
                item.Action = FleetTemplateAction.Identical;
                item.Succeeded = true;
                return item;
            }

            var created = sanitizedCurrent is null;
            if (created)
            {
                await CreateAsync(unit, configMapService, workloadService);
            }
            else
            {
                await UpdateAsync(unit, configMapService, workloadService);
            }

            await auditService.LogAsync(
                unit.Kind == CompareKind.ConfigMap ? AuditCategory.Configmap : AuditCategory.Workload,
                created ? AuditAction.Create : AuditAction.Update,
                $"舰队下发：在集群 {unit.ClusterName} {(created ? "创建" : "更新")} {unit.Kind.ToDisplayText()} {unit.Name}");
            item.Action = created ? FleetTemplateAction.Create : FleetTemplateAction.Update;
            item.Succeeded = true;
            return item;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Fleet template deploy failed clusterId={ClusterId} family={Family} name={Name}",
                unit.ClusterId,
                unit.Kind,
                unit.Name);
            item.Succeeded = false;
            item.Message = ex is BusinessException business ? business.UserMessage : "下发失败，请稍后重试";
            return item;
        }
    }

    /// <summary>按资源族经既有读路径预取对象现值 YAML;对象不存在返回不存在标记,读取失败返回隔离原因。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="configMapService">该作用域内的 ConfigMap 服务。</param>
    /// <param name="workloadService">该作用域内的工作负载服务。</param>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>(读取是否完成,现值 YAML 或 null 为不存在,失败原因)。</returns>
    private async Task<(bool Fetched, string? Yaml, string Message)> FetchCurrentYamlAsync(
        FleetRenderedUnit unit,
        ConfigMapService configMapService,
        WorkloadService workloadService,
        CancellationToken cancellationToken)
    {
        try
        {
            string? yaml = unit.Kind switch
            {
                CompareKind.ConfigMap => (await configMapService.GetConfigMapAsync(new ConfigMapKeyRequest(unit.ClusterId, unit.Name, unit.Namespace)))?.Yaml,
                CompareKind.Deployment => (await workloadService.GetDeploymentAsync(new WorkloadKeyRequest(unit.ClusterId, unit.Name, unit.Namespace)))?.Yaml,
                CompareKind.StatefulSet => (await workloadService.GetStatefulSetAsync(new WorkloadKeyRequest(unit.ClusterId, unit.Name, unit.Namespace)))?.Yaml,
                CompareKind.DaemonSet => (await workloadService.GetDaemonSetAsync(new WorkloadKeyRequest(unit.ClusterId, unit.Name, unit.Namespace)))?.Yaml,
                CompareKind.ReplicaSet => (await workloadService.GetReplicaSetAsync(new WorkloadKeyRequest(unit.ClusterId, unit.Name, unit.Namespace)))?.Yaml,
                _ => null
            };
            return (true, yaml, "");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (NotFoundException)
        {
            return (true, null, "");
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning(
                ex,
                "Fleet template fetch current failed clusterId={ClusterId} family={Family}",
                unit.ClusterId,
                unit.Kind);
            return (false, null, ex.UserMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Fleet template fetch current failed clusterId={ClusterId} family={Family}",
                unit.ClusterId,
                unit.Kind);
            return (false, null, $"获取现有{unit.Kind.ToDisplayText()}资源失败，请检查集群可达性[{ex.GetType().Name}:{ex.Message}]");
        }
    }

    /// <summary>按资源族走既有创建路径(归属盖章、命名空间规则与审计语义自动继承)。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="configMapService">该作用域内的 ConfigMap 服务。</param>
    /// <param name="workloadService">该作用域内的工作负载服务。</param>
    /// <returns>创建完成任务。</returns>
    private static Task CreateAsync(FleetRenderedUnit unit, ConfigMapService configMapService, WorkloadService workloadService) =>
        unit.Kind switch
        {
            CompareKind.ConfigMap => configMapService.CreateConfigMapFromYamlAsync(new ConfigMapCreateRequest(unit.ClusterId, unit.RenderedYaml)),
            CompareKind.Deployment => workloadService.CreateDeploymentFromYamlAsync(new WorkloadCreateRequest(unit.ClusterId, unit.RenderedYaml)),
            CompareKind.StatefulSet => workloadService.CreateStatefulSetFromYamlAsync(new WorkloadCreateRequest(unit.ClusterId, unit.RenderedYaml)),
            CompareKind.DaemonSet => workloadService.CreateDaemonSetFromYamlAsync(new WorkloadCreateRequest(unit.ClusterId, unit.RenderedYaml)),
            CompareKind.ReplicaSet => workloadService.CreateReplicaSetFromYamlAsync(new WorkloadCreateRequest(unit.ClusterId, unit.RenderedYaml)),
            _ => Task.CompletedTask
        };

    /// <summary>按资源族走既有编辑路径(全量替换)。</summary>
    /// <param name="unit">该集群的渲染单元。</param>
    /// <param name="configMapService">该作用域内的 ConfigMap 服务。</param>
    /// <param name="workloadService">该作用域内的工作负载服务。</param>
    /// <returns>更新完成任务。</returns>
    private static Task UpdateAsync(FleetRenderedUnit unit, ConfigMapService configMapService, WorkloadService workloadService) =>
        unit.Kind switch
        {
            CompareKind.ConfigMap => configMapService.UpdateConfigMapFromYamlAsync(new ConfigMapUpdateRequest(unit.ClusterId, unit.Name, unit.Namespace, unit.RenderedYaml)),
            CompareKind.Deployment => workloadService.UpdateDeploymentFromYamlAsync(new WorkloadUpdateRequest(unit.ClusterId, unit.Name, unit.Namespace, unit.RenderedYaml)),
            CompareKind.StatefulSet => workloadService.UpdateStatefulSetFromYamlAsync(new WorkloadUpdateRequest(unit.ClusterId, unit.Name, unit.Namespace, unit.RenderedYaml)),
            CompareKind.DaemonSet => workloadService.UpdateDaemonSetFromYamlAsync(new WorkloadUpdateRequest(unit.ClusterId, unit.Name, unit.Namespace, unit.RenderedYaml)),
            CompareKind.ReplicaSet => workloadService.UpdateReplicaSetFromYamlAsync(new WorkloadUpdateRequest(unit.ClusterId, unit.Name, unit.Namespace, unit.RenderedYaml)),
            _ => Task.CompletedTask
        };

    /// <summary>有界并发选项(最多 4 集群同时)。</summary>
    /// <param name="cancellationToken">停机取消令牌。</param>
    /// <returns>并行选项。</returns>
    private static ParallelOptions BuildParallelOptions(CancellationToken cancellationToken) => new()
    {
        MaxDegreeOfParallelism = MaxFleetConcurrency,
        CancellationToken = cancellationToken
    };

    /// <summary>判断两侧规范化文本按行序列是否一致(容忽略换行符差异)。</summary>
    /// <param name="left">左侧文本(现值剥管后)。</param>
    /// <param name="right">右侧文本(渲染值剥管后)。</param>
    /// <returns>行序列一致为 true。</returns>
    private static bool IsLineSequenceEqual(string left, string right)
    {
        var leftLines = left.Replace("\r\n", "\n").Split('\n');
        var rightLines = right.Replace("\r\n", "\n").Split('\n');
        return leftLines.Length == rightLines.Length
            && leftLines.SequenceEqual(rightLines, StringComparer.Ordinal);
    }

    /// <summary>当前用户是否为 Admin(服务端强制,不依赖 UI)。</summary>
    /// <returns>Admin 角色为 true。</returns>
    private bool IsAdmin() => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;

    /// <summary>单个目标集群的渲染单元:包含该集群身份、按渲染 YAML 判定的资源族与对象键、剥管后的渲染文本。</summary>
    private sealed class FleetRenderedUnit
    {
        /// <summary>目标集群 Id。</summary>
        public required int ClusterId { get; init; }

        /// <summary>目标集群展示名。</summary>
        public required string ClusterName { get; init; }

        /// <summary>渲染 YAML 的资源族(五族之一)。</summary>
        public required CompareKind Kind { get; init; }

        /// <summary>渲染 YAML 的对象名。</summary>
        public required string Name { get; init; }

        /// <summary>渲染 YAML 的目标命名空间(缺省为 default)。</summary>
        public required string Namespace { get; init; }

        /// <summary>剥管后的渲染 YAML(下发与比较都以该文本为准)。</summary>
        public required string RenderedYaml { get; init; }
    }
}
