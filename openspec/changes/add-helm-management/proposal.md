# Helm 应用管理

## Why

系统已经覆盖了集群资源视角(工作负载、服务、配置、事件),但**应用级交付完全缺失**:chart 包只能登录集群节点用 `helm` 命令手工安装与维护,环境上装了哪些 release、什么版本、什么状态,系统里一概看不见;成员无法自助安装,操作既无权限管控也无审计记录。

这件事现在可行的前提刚刚确认:helm CLI 可以随本系统镜像一起分发(用户确认),因此执行引擎直接用官方 helm 二进制、兼容性零风险,不必赌托管 SDK 的模板兼容性。

## What Changes

- **已安装 Chart 包视图**:新增 `/helm` 集群上下文页——release 列表、详情(状态/NOTES/用户 values/manifest)、历史(revision)。数据全部经 helm CLI 获取(`list`/`status`/`history`/`get`),沿用集群选择栏与既有详情页/表格设计词汇。
- **完整生命周期操作**:安装、升级、回滚、卸载四个动作,全部经 helm CLI 子进程执行。安装/升级的 chart 来源为**上传 `.tgz` 包**(即传即用:临时文件、操作结束即删,系统不持久化包),上传时本地解析 Chart.yaml 展示名称/版本并对包做合法性校验。
- **归属权限模型**:release 列表全员可见;操作按归属强制——**Admin 可操作任意 release;Member 只能操作自己安装的**;系统外安装/无法确认归属的 release 视为无主,**仅 Admin 可操作**(fail-closed)。强制在服务端(不信 UI)+ 页面条件渲染两层;归属记账落库,不展示"安装者"列,追溯走审计日志。
- **Helm CLI 运行时**:镜像内钉死 Helm 4.x 二进制(开发机经 PATH 或 `Helm:CliPath`);子进程经 `ArgumentList` 启动(无 shell 注入面),进程级有界超时 + 取消时进程树终止;凭据物化为临时 kubeconfig(KubeConfig 文本直写 / Token 型合成);`HELM_*` 目录与 KUBECONFIG 钉到可写临时路径(容器内非 root、`/app` 不可写)。
- **审计与契约修订**:审计新增 Helm 类别与安装/升级/回滚/卸载动作;`kubernetes-call-timeout` 与 `k8s-client-cache` 显式声明"Helm CLI 子进程不适用"的边界;`display-conventions` 增加 Helm release 状态中英映射;`ui-theme` 徽章适用范围扩展到 Helm release。
- **BREAKING(数据)**:归属记账新增一张表,项目无 EF migrations(`EnsureCreated` 不补建表),升级后必须删 `db/` 重建,已登记集群与凭据需重新录入(与 `add-cluster-dashboard` 同款影响,仅开发态与自建部署)。

## Capabilities

### New Capabilities

- `helm-release-management`: Helm release 的读取与生命周期契约——列表/详情/历史/values/manifest 的口径;安装/升级/回滚/卸载的行为与参数;.tgz 上传校验、元数据解析与临时文件语义;归属记账、无主判定与双层权限强制;页面路由、导航与状态展示要求。
- `helm-cli-runtime`: Helm CLI 运行时契约——二进制随镜像分发与版本钉定/可配置路径;子进程执行安全(参数表、无 shell、进程树终止、有界超时与取消);凭据物化与临时文件生命周期;`HELM_*` 环境隔离;可测试的进程执行端口分层。

### Modified Capabilities

- `audit-log`: 审计事件枚举增加 Helm 安装/升级/回滚/卸载;类别枚举增加 Helm;动作枚举扩充(安装/升级/回滚/卸载),目标描述格式含 release、命名空间、集群。
- `kubernetes-call-timeout`: 明确契约适用边界——Helm CLI 子进程发起的集群访问不适用 10 秒 REST 超时,采用 helm 自身的 `--timeout` 与进程级上限,但仍 SHALL 有界、不得无限等待。
- `k8s-client-cache`: 明确契约适用边界——Helm CLI 子进程不经客户端缓存与工厂;契约中的"所有 K8s 调用"限定为进程内经 KubernetesClient 的调用。
- `display-conventions`: 增加 Helm release 状态的双语映射(deployed/failed/pending-*/superseded/uninstalling/uninstalled)与徽章语义归属。
- `ui-theme`: 状态徽章的应用范围扩展至 Helm release 列表与详情,沿用既有在线/离线/未知配色,不新增颜色。

## Impact

- **代码**:
  - 新增:`Application/Abstractions/IHelmCliRunner.cs`(端口)、`Application/Abstractions/IHelmReleaseOwnershipRepository.cs`、`Application/Services/HelmService.cs`、`Application/Requests/Helm*.cs`、`Application/ViewModels/Helm*.cs` + Mappings、`Domain/Entities/HelmReleaseOwnership.cs`、`Infrastructure/Helm/**`(CLI runner、命令构建、凭据物化、进程执行封装)、`Infrastructure/Persistence/HelmReleaseOwnershipRepository.cs`、`Web/Components/Helm/**`(页面、详情、安装/升级/回滚/卸载对话框)。
  - 修改:`Domain/Enums/AuditCategory.cs`、`AuditAction.cs`、`Infrastructure/Persistence/ApplicationDbContext.cs`(DbSet + 唯一索引 + 级联)、`Infrastructure/InfrastructureServiceCollectionExtensions.cs`、`Application/ApplicationServiceCollectionExtensions.cs`、`Web/Components/Layout/Drawer.razor`、`Web/wwwroot/css/app.css`、`MultiClusterMgmtSys.Web/Dockerfile`(Helm 二进制)、`Web/appsettings.json`(`Helm:CliPath` 等)。
- **测试**:新增 HelmService 测试(fake runner + JSON fixture:命令参数、解析、异常翻译、归属权限矩阵)、ChartPackageReader 测试(内存构造 tgz)、KubeConfig 物化测试、HelmCliRunner 测试(假进程执行器:临时文件生命周期、环境变量、取消终止)、bUnit 页面/对话框接线与权限可见性测试;`dotnet build` 0 错误、`dotnet test` 全绿、`./coverage.ps1` 四程序集合并行覆盖率门禁 75% 不降。
- **文档/部署**:`Dockerfile` 增加钉版 Helm 4.x 与升级指引;AGENTS.md 补记新页面、归属表、删库重建步骤、开发机 helm 前置条件;新增 specs 与 5 个 delta 随归档合并进主 spec。
- **范围外**:不做 repo 目录管理/仓库直连/OCI(来源限上传 .tgz);不做 chart 包持久化库(即传即用);不做后台作业队列(同步执行,默认不 `--wait`,wait 为高级选项);不把系统暴露为 helm repo;不引入托管渲染 SDK;不做 release 资源清单的 Kubernetes 对象级下钻。
