using System.Security.Claims;
using System.Text;
using k8s;
using MultiClusterMgmtSys.Application.Abstractions;
using MultiClusterMgmtSys.Application.Common.Exceptions;
using MultiClusterMgmtSys.Application.Common.Helm;
using MultiClusterMgmtSys.Application.Requests;
using MultiClusterMgmtSys.Application.ViewModels;
using MultiClusterMgmtSys.Domain.Entities;
using MultiClusterMgmtSys.Domain.Enums;
using MultiClusterMgmtSys.Domain.Exceptions;

namespace MultiClusterMgmtSys.Application.Services;

/// <summary>
/// Helm 应用管理服务:经 Helm CLI 子进程读取 release 列表/详情/历史/values/manifest,并执行安装/升级/回滚/卸载。
/// 归属权限在服务端强制(Admin 可操作任意 release,成员仅可操作自己安装的,无主仅 Admin);
/// Helm 调用不适用进程内 K8s 客户端缓存与 10 秒超时契约,进程级超时有界(契约见 helm-cli-runtime spec)。
/// </summary>
public class HelmService(
    IClusterRepository repo,
    IHelmReleaseOwnershipRepository ownershipRepo,
    IHelmCliRunner helmRunner,
    HelmOptions options,
    IClusterClientCache clientCache,
    IHttpContextAccessor httpContextAccessor,
    AuditService auditService,
    ILogger<HelmService> logger)
{
    private static readonly TimeSpan _ReadTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan _WriteTimeout = TimeSpan.FromSeconds(120);

    private static readonly TimeSpan _WaitTimeoutMargin = TimeSpan.FromSeconds(60);

    private const int WaitTimeoutSeconds = 300;

    private const int LogErrorMaxLength = 2000;

    private readonly IClusterRepository _repo = repo;

    private readonly IHelmReleaseOwnershipRepository _ownershipRepo = ownershipRepo;

    private readonly IHelmCliRunner _helmRunner = helmRunner;

    private readonly HelmOptions _options = options;

    private readonly IClusterClientCache _clientCache = clientCache;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private readonly AuditService _auditService = auditService;

    private readonly ILogger<HelmService> _logger = logger;

    /// <summary>列出集群全部命名空间的 release;集群不存在抛 <see cref="NotFoundException"/>,Helm 失败经翻译后抛业务异常。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <returns>release 列表,含当前用户的 <see cref="HelmReleaseListViewModel.CanOperate"/> 判定。</returns>
    public async Task<List<HelmReleaseListViewModel>> ListReleasesAsync(int clusterId)
    {
        _logger.LogInformation("ListHelmReleases clusterId={ClusterId}", clusterId);
        var cluster = await RequireClusterAsync(clusterId);
        var result = await RunAsync(HelmCommandBuilder.BuildList(), cluster, _ReadTimeout);
        var entries = HelmOutputParser.ParseReleaseList(result.StandardOutput);

        var ownershipByRelease = (await _ownershipRepo.GetByClusterAsync(clusterId))
            .ToDictionary(ownership => (ownership.Namespace, ownership.ReleaseName));
        var isAdmin = IsAdmin();
        var currentUserId = GetCurrentUserId();

        var releases = entries.Select(entry =>
        {
            var reference = HelmChartReference.Parse(entry.Chart);
            return new HelmReleaseListViewModel
            {
                Name = entry.Name,
                Namespace = entry.Namespace,
                Chart = entry.Chart,
                ChartName = reference.Name,
                ChartVersion = reference.Version,
                AppVersion = entry.AppVersion,
                Revision = entry.Revision,
                Status = entry.Status,
                UpdatedAt = entry.UpdatedAt,
                CanOperate = isAdmin || IsOwnedBy(ownershipByRelease.GetValueOrDefault((entry.Namespace, entry.Name)), entry.Revision, currentUserId)
            };
        }).ToList();

        _logger.LogInformation("ListHelmReleases returned {Count} for clusterId={ClusterId}", releases.Count, clusterId);
        return releases;
    }

    /// <summary>读取 release 详情(状态、NOTES、chart 元数据与 manifest);集群或 release 不存在抛 <see cref="NotFoundException"/>。</summary>
    /// <param name="request">release 资源键。</param>
    /// <returns>详情展示数据。</returns>
    public async Task<HelmReleaseDetailViewModel> GetReleaseDetailAsync(HelmReleaseKeyRequest request)
    {
        _logger.LogInformation("GetHelmReleaseDetail clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.Name);
        var cluster = await RequireClusterAsync(request.ClusterId);
        var result = await RunAsync(HelmCommandBuilder.BuildStatus(request.Name, request.Namespace), cluster, _ReadTimeout);
        var status = HelmOutputParser.ParseStatus(result.StandardOutput);
        var ownership = await _ownershipRepo.GetAsync(request.ClusterId, request.Namespace, request.Name);

        return new HelmReleaseDetailViewModel
        {
            Name = status.Name,
            Namespace = status.Namespace,
            Revision = status.Revision,
            Status = status.Status,
            Description = status.Description,
            Notes = status.Notes,
            ChartName = status.ChartName,
            ChartVersion = status.ChartVersion,
            AppVersion = status.AppVersion,
            Manifest = status.Manifest,
            LastDeployedAt = status.LastDeployedAt,
            CanOperate = IsAdmin() || IsOwnedBy(ownership, status.Revision, GetCurrentUserId())
        };
    }

    /// <summary>读取 release 历史(revision 列表);集群或 release 不存在抛 <see cref="NotFoundException"/>。</summary>
    /// <param name="request">release 资源键。</param>
    /// <returns>按 CLI 输出顺序的历史项(通常为 revision 升序)。</returns>
    public async Task<List<HelmReleaseHistoryItemViewModel>> GetReleaseHistoryAsync(HelmReleaseKeyRequest request)
    {
        _logger.LogInformation("GetHelmReleaseHistory clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.Name);
        var cluster = await RequireClusterAsync(request.ClusterId);
        var result = await RunAsync(HelmCommandBuilder.BuildHistory(request.Name, request.Namespace), cluster, _ReadTimeout);
        var history = HelmOutputParser.ParseHistory(result.StandardOutput);

        return [.. history.Select(item => new HelmReleaseHistoryItemViewModel
        {
            Revision = item.Revision,
            Status = item.Status,
            Chart = item.Chart,
            AppVersion = item.AppVersion,
            Description = item.Description,
            UpdatedAt = item.UpdatedAt
        })];
    }

    /// <summary>读取 release 的用户 values(YAML 文本);集群或 release 不存在抛 <see cref="NotFoundException"/>。</summary>
    /// <param name="request">release 资源键。</param>
    /// <returns>values 的 YAML 文本;无用户 values 时为空。</returns>
    public async Task<string> GetReleaseValuesAsync(HelmReleaseKeyRequest request)
    {
        _logger.LogInformation("GetHelmReleaseValues clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.Name);
        var cluster = await RequireClusterAsync(request.ClusterId);
        var result = await RunAsync(HelmCommandBuilder.BuildGetValues(request.Name, request.Namespace), cluster, _ReadTimeout);
        return result.StandardOutput;
    }

    /// <summary>读取 release 渲染后的 manifest 文本;集群或 release 不存在抛 <see cref="NotFoundException"/>。</summary>
    /// <param name="request">release 资源键。</param>
    /// <returns>manifest 文本。</returns>
    public async Task<string> GetReleaseManifestAsync(HelmReleaseKeyRequest request)
    {
        _logger.LogInformation("GetHelmReleaseManifest clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.Name);
        var cluster = await RequireClusterAsync(request.ClusterId);
        var result = await RunAsync(HelmCommandBuilder.BuildGetManifest(request.Name, request.Namespace), cluster, _ReadTimeout);
        return result.StandardOutput;
    }

    /// <summary>拉取集群命名空间列表(升序),供安装对话框命名空间下拉使用;集群不存在抛 <see cref="NotFoundException"/>,K8s 失败经翻译后抛业务异常。</summary>
    /// <param name="clusterId">集群 Id。</param>
    /// <returns>命名空间名称列表。</returns>
    public async Task<List<string>> GetNamespacesAsync(int clusterId)
    {
        var cluster = await RequireClusterAsync(clusterId);
        var client = _clientCache.GetOrCreate(cluster);
        try
        {
            var namespaces = await client.CoreV1.ListNamespaceAsync();
            return [.. namespaces.Items.Select(item => item.Metadata?.Name ?? "").OrderBy(name => name)];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ListNamespaces failed clusterId={ClusterId}", clusterId);
            throw K8sExceptionMapper.Translate(ex, "加载命名空间");
        }
    }

    /// <summary>解析上传的 chart 包:校验大小上限并提取元数据与 values 初值;包不合法抛 <see cref="ValidationException"/>。</summary>
    /// <param name="content">.tgz 字节内容。</param>
    /// <returns>对话框展示与预填数据。</returns>
    public HelmChartPackageViewModel ParseChartPackage(byte[] content)
    {
        ChartPackageLimits.EnsureWithinLimit(content.LongLength, _options.MaxPackageBytes);
        var info = ChartPackageReader.Read(content);
        return new HelmChartPackageViewModel
        {
            Name = info.Name,
            Version = info.Version,
            AppVersion = info.AppVersion,
            Description = info.Description,
            Dependencies = info.Dependencies,
            ValuesYaml = info.ValuesYaml,
            Warnings = info.Warnings,
            SuggestedReleaseName = info.Name
        };
    }

    /// <summary>安装 release:上传包暂存为临时文件执行 helm install,成功后写归属与审计;失败不写归属。</summary>
    /// <param name="request">安装请求(包内容、values、目标位置与选项)。</param>
    public async Task InstallAsync(HelmInstallRequest request)
    {
        _logger.LogInformation("InstallHelmRelease clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.ReleaseName);
        var cluster = await RequireClusterAsync(request.ClusterId);
        var userId = RequireCurrentUserId();
        var userName = GetCurrentUserName();
        ValidateNameAndNamespace(request.ReleaseName, request.Namespace);
        ChartPackageLimits.EnsureWithinLimit(request.ChartPackage.LongLength, _options.MaxPackageBytes);

        var arguments = HelmCommandBuilder.BuildInstall(
            request.ReleaseName,
            request.Namespace,
            includeValues: HasValues(request.ValuesYaml),
            createNamespace: request.CreateNamespace,
            wait: request.Wait,
            timeoutSeconds: WaitTimeoutSeconds);
        await RunAsync(arguments, cluster, BuildWriteTimeout(request.Wait), BuildWriteFiles(request.ChartPackage, request.ValuesYaml));

        await TryUpsertOwnershipAsync(new HelmReleaseOwnership
        {
            ClusterId = cluster.Id,
            Namespace = request.Namespace,
            ReleaseName = request.ReleaseName,
            OwnerUserId = userId,
            OwnerUserName = userName,
            InstalledAt = DateTime.UtcNow,
            InstalledRevision = 1
        });
        await _auditService.LogAsync(
            AuditCategory.Helm,
            AuditAction.Install,
            $"Helm: 安装 {request.ReleaseName}(集群 {cluster.Name} / 命名空间 {request.Namespace})");
    }

    /// <summary>升级 release:上传新包执行 helm upgrade(沿用现存 values 或提交重新编辑的 values),归属不变;权限不足抛 <see cref="PermissionException"/>。</summary>
    /// <param name="request">升级请求(包内容、values 模式与选项)。</param>
    public async Task UpgradeAsync(HelmUpgradeRequest request)
    {
        _logger.LogInformation("UpgradeHelmRelease clusterId={ClusterId} ns={Namespace} name={Name}", request.ClusterId, request.Namespace, request.ReleaseName);
        var cluster = await RequireClusterAsync(request.ClusterId);
        ValidateNameAndNamespace(request.ReleaseName, request.Namespace);
        ChartPackageLimits.EnsureWithinLimit(request.ChartPackage.LongLength, _options.MaxPackageBytes);
        var currentRevision = await GetCurrentRevisionAsync(cluster, request.Namespace, request.ReleaseName);
        await RequireOperatePermissionAsync(cluster.Id, request.Namespace, request.ReleaseName, currentRevision);

        var arguments = HelmCommandBuilder.BuildUpgrade(
            request.ReleaseName,
            request.Namespace,
            includeValues: !request.ReuseValues && HasValues(request.ValuesYaml),
            reuseValues: request.ReuseValues,
            wait: request.Wait,
            timeoutSeconds: WaitTimeoutSeconds);
        await RunAsync(arguments, cluster, BuildWriteTimeout(request.Wait), BuildWriteFiles(request.ChartPackage, request.ValuesYaml));

        await _auditService.LogAsync(
            AuditCategory.Helm,
            AuditAction.Upgrade,
            $"Helm: 升级 {request.ReleaseName}(集群 {cluster.Name} / 命名空间 {request.Namespace})");
    }

    /// <summary>回滚 release 到指定 revision,归属不变;权限不足抛 <see cref="PermissionException"/>。</summary>
    /// <param name="request">回滚请求。</param>
    public async Task RollbackAsync(HelmRollbackRequest request)
    {
        _logger.LogInformation("RollbackHelmRelease clusterId={ClusterId} ns={Namespace} name={Name} revision={Revision}", request.ClusterId, request.Namespace, request.ReleaseName, request.Revision);
        var cluster = await RequireClusterAsync(request.ClusterId);
        ValidateNameAndNamespace(request.ReleaseName, request.Namespace);
        var currentRevision = await GetCurrentRevisionAsync(cluster, request.Namespace, request.ReleaseName);
        await RequireOperatePermissionAsync(cluster.Id, request.Namespace, request.ReleaseName, currentRevision);

        await RunAsync(
            HelmCommandBuilder.BuildRollback(request.ReleaseName, request.Namespace, request.Revision),
            cluster,
            _WriteTimeout);

        await _auditService.LogAsync(
            AuditCategory.Helm,
            AuditAction.Rollback,
            $"Helm: 回滚 {request.ReleaseName}(集群 {cluster.Name} / 命名空间 {request.Namespace}, revision {request.Revision})");
    }

    /// <summary>卸载 release:成功后删除归属记录并写审计;权限不足抛 <see cref="PermissionException"/>。</summary>
    /// <param name="request">卸载请求(可选保留历史)。</param>
    public async Task UninstallAsync(HelmUninstallRequest request)
    {
        _logger.LogInformation("UninstallHelmRelease clusterId={ClusterId} ns={Namespace} name={Name} keepHistory={KeepHistory}", request.ClusterId, request.Namespace, request.ReleaseName, request.KeepHistory);
        var cluster = await RequireClusterAsync(request.ClusterId);
        ValidateNameAndNamespace(request.ReleaseName, request.Namespace);
        var currentRevision = await GetCurrentRevisionAsync(cluster, request.Namespace, request.ReleaseName);
        await RequireOperatePermissionAsync(cluster.Id, request.Namespace, request.ReleaseName, currentRevision);

        await RunAsync(
            HelmCommandBuilder.BuildUninstall(request.ReleaseName, request.Namespace, request.KeepHistory),
            cluster,
            _WriteTimeout);

        await TryDeleteOwnershipAsync(cluster.Id, request.Namespace, request.ReleaseName);
        await _auditService.LogAsync(
            AuditCategory.Helm,
            AuditAction.Uninstall,
            $"Helm: 卸载 {request.ReleaseName}(集群 {cluster.Name} / 命名空间 {request.Namespace})");
    }

    private async Task<ClusterInfo> RequireClusterAsync(int clusterId)
        => await _repo.GetByIdAsync(clusterId)
            ?? throw new NotFoundException($"集群 {clusterId} 不存在");

    private async Task<HelmCliResult> RunAsync(
        IReadOnlyList<string> arguments,
        ClusterInfo cluster,
        TimeSpan timeout,
        IReadOnlyList<HelmCliFile>? files = null)
    {
        var result = await _helmRunner.RunAsync(new HelmCliInvocation
        {
            Arguments = arguments,
            Files = files ?? [],
            Cluster = cluster,
            Timeout = timeout
        });
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Helm command failed command={Command} clusterId={ClusterId} exitCode={ExitCode} timedOut={TimedOut} stderr={StandardError}",
                arguments.Count > 0 ? arguments[0] : "",
                cluster.Id,
                result.ExitCode,
                result.TimedOut,
                Truncate(result.StandardError));
            throw HelmErrorTranslator.Translate(result);
        }
        return result;
    }

    private bool IsAdmin() => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;

    private int? GetCurrentUserId()
    {
        var idText = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idText, out var id) ? id : null;
    }

    private string GetCurrentUserName()
        => _httpContextAccessor.HttpContext?.User.Identity?.Name
            ?? throw new PermissionException("无法获取当前登录账号信息");

    private int RequireCurrentUserId()
        => GetCurrentUserId() ?? throw new PermissionException("无法获取当前登录账号信息");

    private static void ValidateNameAndNamespace(string releaseName, string namespaceName)
    {
        if (!HelmCommandBuilder.IsValidReleaseName(releaseName))
        {
            throw new ValidationException("Release 名称不合法:仅允许小写字母、数字与连字符,以字母或数字开头结尾且不超过 53 字符");
        }
        if (!HelmCommandBuilder.IsValidNamespace(namespaceName))
        {
            throw new ValidationException("命名空间名称不合法");
        }
    }

    private static bool HasValues(string? valuesYaml) => !string.IsNullOrWhiteSpace(valuesYaml);

    private TimeSpan BuildWriteTimeout(bool wait)
        => wait ? TimeSpan.FromSeconds(WaitTimeoutSeconds) + _WaitTimeoutMargin : _WriteTimeout;

    private static IReadOnlyList<HelmCliFile> BuildWriteFiles(byte[] chartPackage, string valuesYaml)
    {
        var files = new List<HelmCliFile> { new(HelmCliFileNames.ChartPackage, chartPackage) };
        if (HasValues(valuesYaml))
        {
            files.Add(new HelmCliFile(HelmCliFileNames.Values, Encoding.UTF8.GetBytes(valuesYaml)));
        }
        return files;
    }

    private async Task<int> GetCurrentRevisionAsync(ClusterInfo cluster, string namespaceName, string releaseName)
    {
        var result = await RunAsync(HelmCommandBuilder.BuildStatus(releaseName, namespaceName), cluster, _ReadTimeout);
        return HelmOutputParser.ParseStatus(result.StandardOutput).Revision;
    }

    private async Task RequireOperatePermissionAsync(int clusterId, string namespaceName, string releaseName, int currentRevision)
    {
        if (IsAdmin())
        {
            return;
        }

        var userId = RequireCurrentUserId();
        var ownership = await _ownershipRepo.GetAsync(clusterId, namespaceName, releaseName);
        if (!IsOwnedBy(ownership, currentRevision, userId))
        {
            _logger.LogWarning("Helm operation denied clusterId={ClusterId} ns={Namespace} name={Name} userId={UserId}", clusterId, namespaceName, releaseName, userId);
            throw new PermissionException("仅可操作自己安装的 Chart 包");
        }
    }

    private async Task TryUpsertOwnershipAsync(HelmReleaseOwnership ownership)
    {
        try
        {
            await _ownershipRepo.UpsertAsync(ownership);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Persist helm ownership failed clusterId={ClusterId} ns={Namespace} name={Name}",
                ownership.ClusterId, ownership.Namespace, ownership.ReleaseName);
        }
    }

    private async Task TryDeleteOwnershipAsync(int clusterId, string namespaceName, string releaseName)
    {
        try
        {
            await _ownershipRepo.DeleteAsync(clusterId, namespaceName, releaseName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Delete helm ownership failed clusterId={ClusterId} ns={Namespace} name={Name}",
                clusterId, namespaceName, releaseName);
        }
    }

    private static bool IsOwnedBy(HelmReleaseOwnership? ownership, int currentRevision, int? currentUserId)
        => ownership is not null
            && currentUserId.HasValue
            && ownership.OwnerUserId == currentUserId.Value
            && currentRevision >= ownership.InstalledRevision;

    private static string Truncate(string value)
        => value.Length <= LogErrorMaxLength ? value : value[..LogErrorMaxLength];
}
