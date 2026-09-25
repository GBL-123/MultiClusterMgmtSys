# AGENTS.md

Repo-specific guidance for OpenCode agents working in `MultiClusterMgmtSys`.

## Current state (2026-09-23)

`dotnet build MultiClusterMgmtSys.slnx` 0 错误;`dotnet test MultiClusterMgmtSys.Tests` 770/770 green(xunit.v3 + MTP,see Testing conventions);`./coverage.ps1` 四程序集合并行覆盖率 **78.2%**(门禁 75%)。最近归档的 change:`restyle-reconnect-modal`(重连弹窗与 404/错误兜底页工业风统一,新增 `fallback-pages` 契约)、`clean-architecture-split`(四项目分层重构:Domain/Application/Infrastructure/Web)、`pod-logs`、`pod-management`、`event-management`、`k8s-client-cache`、`display-humanization` 等;`openspec/changes/add-cluster-dashboard/` 为当前待实现 change(全局集群看板,见 Architecture notes 的「全局集群看板」条)。

## Coverage 工具链 (覆盖率口径见 `openspec/specs/unit-testing`)

- **一键入口(首选)**:仓库根 `./coverage.ps1` = build → 直接执行测试 exe(`--coverage --coverage-output-format cobertura --coverage-output`,cobertura 固定落 `coverage/coverage.cobertura.xml`,无 GUID 名)→ ReportGenerator 5.5.11(硬编码 NuGet 缓存路径,csproj 升级版本时须同步脚本)生成 `coverage/report/`(Html;TextSummary)→ **直接解析 cobertura XML**:取 `MultiClusterMgmtSys.Domain` / `.Application` / `.Infrastructure` / `.Web` 四个 `<package>` 的 `<class>/<lines>` 命中行数求和,合并行覆盖率做 ≥75% 门禁(`-Threshold` 可调;四个程序集缺任一/无覆盖行即 throw)。`Summary.txt` 的程序集行只有百分比、没有 Covered/Total 列,**不可**作为数据源。build/测试/报告任一失败即中止、不出报告;每轮整删重建 `coverage/`(已 gitignore)。**直接跑 exe 必须显式传 `--coverage-output-format cobertura`**——MTP 覆盖扩展不按文件名推断格式,缺省落二进制。
- 手动调试路径(脚本不适用时才用):收集 `dotnet test MultiClusterMgmtSys.Tests --coverage --coverage-output-format cobertura` → `TestResults/<guid>.cobertura.xml`(落**解决方案根**的 TestResults/);报告 `dotnet "<NuGet缓存>\reportgenerator\5.5.11\tools\net10.0\ReportGenerator.dll" -reports:"TestResults/<最新单份>.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`。
- **口径以最新单份 cobertura 为准**(仅手动路径适用):用 `TestResults/*.cobertura.xml` 通配符会把历史构建的报告合并成 MultiReport,重复计覆盖导致虚高——查数字时永远只喂最新一份;`coverage.ps1` 每轮单份固定文件名,天然无此问题。
- 排除口径:`[ExcludeFromCodeCoverage]` 已打在 Web 的 `Program.cs`(partial class,顶层语句同文件)、`Endpoints/IdentityComponentsEndpointRouteBuilderExtensions.cs`、`Components/App.razor`、`Components/Routes.razor`;测试程序集不计入分母。`ChineseIdentityErrorDescriber` 不排除(有测试)。
- **已知 dev-run 怪癖(非本仓问题)**:`dotnet run` 下 scoped CSS 包 URL(`MultiClusterMgmtSys.Web.*.styles.css`)会被 `StaticAssetDevelopmentRuntimeHandler` 以 `wwwroot\<bundle>` 路径尝试读文件而 500,基线同样复现;`css/app.css`、`js/show-password.js`、`_framework/*` 正常。

## Stack

- .NET 10 / ASP.NET Core, Blazor **interactive server** render mode, MudBlazor 9.10.0 + `Extensions.MudBlazor.StaticInput` (`@using MudBlazor.StaticInput` lives in `_Imports.razor`)
- EF Core 10 with **SQLite** + ASP.NET Identity (roles `Admin`/`Member`, keys `int`)
- Kubernetes cluster access via `KubernetesClient` 19.0.2
- Unit tests: **xunit.v3 + MTP** (Microsoft.Testing.Platform v2, enabled by root `global.json` `test.runner`; no `sdk` pin — Docker uses the floating `sdk:10.0` image). No VSTest bridge — never re-add `Microsoft.NET.Test.Sdk` / `xunit.runner.visualstudio` / coverlet packages. Test project has `OutputType Exe`.
- Serilog: console + daily rolling file `logs/app-.log` (30-day retention); path configurable via `Logging:File:Path`. EF SQL statement logs are Development-only (`InfrastructureServiceCollectionExtensions` 注册链路不变)。
- UI strings are **Chinese** (e.g. `ChineseIdentityErrorDescriber`, service messages, audit descriptions). Keep new user-facing strings consistent.
- Solution `MultiClusterMgmtSys.slnx` (new XML format)。项目:`MultiClusterMgmtSys.Domain`、`MultiClusterMgmtSys.Application`、`MultiClusterMgmtSys.Infrastructure`、`MultiClusterMgmtSys.Web`(宿主 + Blazor UI)、`MultiClusterMgmtSys.Tests`、`docker-compose.dcproj`。
- Design system is **Swiss Industrial Print, light-only**: static theme, no dark mode (see "UI / CSS conventions" below).

## 分层架构 (契约 `openspec/specs/architecture-layering`)

```
Domain <-- Application <-- Infrastructure <-- Web
              ^                                |
              +--------------------------------+
        (Web 组件只消费 Application 契约;Infrastructure 不引用 Web)
```

- `Domain`:实体/枚举/业务异常,零项目与包依赖。
- `Application`:用例服务 + `Requests`/`ViewModels`/`Models`/`Abstractions`(端口);允许引用 k8s/Identity/Http 等框架包,**禁止**引用 EF Core/DbContext/Infrastructure/Web。
- `Infrastructure`:EF/SQLite、Identity 持久化、k8s 客户端(配置/缓存)、YAML 模板读取、后台同步;实现 Application 端口。
- `Web`:宿主组合根(`Program.cs`、DI 注册、appsettings、wwwroot、Dockerfile)+ 全部 Razor UI(`Components/`、`Endpoints/`)。
- **组件命名空间隔离由架构测试保证**(编译器无法阻断,Web 与组合根同程序集):`MultiClusterMgmtSys.Web.Components.*` 不得使用 Infrastructure/EF/`Domain.Entities`/仓库端口/k8s 类型;规则在 `MultiClusterMgmtSys.Tests/Architecture/ArchitectureTests.cs`,随 `dotnet test` 执行。
- **DI 注册规则**:新增服务注册进所属层扩展——`AddApplicationServices()`(`Application/ApplicationServiceCollectionExtensions.cs`)或 `AddInfrastructure(config, contentRootPath)`(`Infrastructure/InfrastructureServiceCollectionExtensions.cs`);`Program.cs` 只做组合与 Web 专属注册(MudBlazor/认证/UI 辅助服务)。

## Commands

```pwsh
dotnet build MultiClusterMgmtSys.slnx       # 0 errors
dotnet test MultiClusterMgmtSys.Tests       # 727 tests, all green (MTP)
./coverage.ps1                              # 一键 UT + 覆盖率报告:727 tests 全绿 + 四程序集合并行覆盖门禁 75% → coverage/coverage.cobertura.xml + coverage/report/index.html(-Threshold 可调阈值,低于阈值 exit 1)
dotnet run  --project MultiClusterMgmtSys.Web                                    # http://localhost:5021
dotnet run  --project MultiClusterMgmtSys.Web --launch-profile https             # https://localhost:7081
```

测试项目是 `MultiClusterMgmtSys.Tests/`(xunit.v3 + Moq + bUnit + SQLite 内存库);无 lint/format/typecheck config,不要虚构 lint 命令。

Build gotcha: if `dotnet build` fails with MSB3021/MSB3026/MSB3027 (exe locked), a running app instance is holding `bin/.../MultiClusterMgmtSys.Web.exe` (often a leftover `dotnet run`) — the error prints the locking PID; `Stop-Process -Id <pid> -Force`, then rebuild. 项目/目录改名后如出现静态资产 404/500,删 `bin/`+`obj/` 全量重建。

## Testing conventions

- 测试项目 `MultiClusterMgmtSys.Tests/`,目录镜像四层(`Domain/`、`Application/`、`Infrastructure/`、`Components/`)+ `TestInfrastructure/`(SqliteDbFactory、TestData、SeedUser、TestIdentity、TestHttpContext、K8sMocks、ServiceHarness、BunitHost、BunitServiceExtensions)。测试项目引用四个生产项目,csproj 全局 `Using` 含 `MultiClusterMgmtSys.Application.Abstractions`。
- **后端测试以服务为边界**:直接调 Service 公开方法,仓库经真实 SQLite 内存库(`SqliteDbFactory.CreateContext()`)覆盖;断言语义 = 业务异常类型 + 中文 UserMessage + 查询结果。
- **K8s 服务可测**:`Func<KubernetesClientConfiguration, IKubernetes>` 工厂注入(`AddInfrastructure` 注册真实工厂);测试用 Moq mock 接口的 `*WithHttpMessagesAsync` 方法(扩展方法的底层,签名用 `sigtool` 反查——k8s 19 的参数顺序与直觉不同),抛 `KubernetesException(new V1Status{Code=…})` 验证翻译链路。`K8sMocks.LazyFailing()` 返回惰性失败 mock(工厂调用不抛,真正走到 K8s 才失败);常用 setup 见 K8sMocks 的 `SetupListNodes/SetupGetVersion/SetupListDeployments/SetupListNamespaceObjects/…` 扩展。
- **统一 10s 超时**:`ClusterServiceTests` 与 `ClusterClientCacheTests` 用捕获 `KubernetesClientConfiguration` 的工厂断言两种连接方式的 `HttpClientTimeout` 均为 10s——改超时值必须同步这两个测试(见 Architecture notes)。
- **bUnit 只测"接线契约"**:`FindComponent<T>()` 取 MudBlazor 组件实例、触发公开事件/参数,断言自己组件的状态/渲染分支/自有 CSS 类(`.status-badge`/`.empty-state` 等);**禁止断言 `.mud-*` 内部 DOM**。bUnit 测试需要 `TimeProvider.System` + `JSRuntimeMode.Loose` + MudServices(BunitHost 已配);事件回调必须经 `cut.InvokeAsync(...)` 调度到 Dispatcher;异步数据(MudTable ServerData)用 `cut.WaitForState(...)` 等待;并行执行下依赖后台刷新完成的测试需有界等待(轮询 + `Task.Delay`,勿用固定次数紧循环)。
- **bUnit 2.x API**:`BunitContext`(不是 `TestContext`,也与 xunit.v3 的 `Xunit.TestContext` 撞名)、`Render<T>()`(不是 `RenderComponent`)、`AddAuthorization()` + `SetAuthorized(name)`/`SetRoles("Admin")`;创建过 MudBlazor 组件的 ctx 用 `await using var ctx = ...` 释放 —— MudBlazor 的 KeyInterceptor/PointerEventsNone 服务仅实现 `IAsyncDisposable`,同步 `Dispose()` 会在测试逻辑已通过后抛异常;此类测试方法签名用 `async Task`。`BunitContext` 的 Services 在**首次 GetService 后冻结**,所有服务必须在首渲染前注册;对话框测试用 `ctx.Render<MudDialogProvider>()` + `ShowAsync<T>()` 流程;登录页级联 `HttpContext` 用 `AddCascadingValue(new DefaultHttpContext())`(按类型);需要 RendererInfo 的组件在**注册完服务后**调 `ctx.Renderer.SetRendererInfo(new RendererInfo("bunit", true))`。
- **bUnit 交互硬坑(实测)**:① MudMenu 弹层由 JS 渲染,bUnit 中点击后菜单项不出现——菜单驱动的删除/批量改角色无法端到端驱动,改测直接按钮或退到服务层;② DOM `.Click()` 触发含 `await DialogService.ShowAsync(...).Result` 的处理器后,页面续体在调度队列里,需 `await provider.InvokeAsync(() => { })` 冲刷再轮询服务/审计落库,直接 `await dialogReference.Result` 可能死锁;③ 依赖 `MudForm.ValidateAsync()` 的对话框提交流(EditGroupDialog/AccountEditDialog 等)在 bUnit 下不稳定,项目实践改为渲染断言 + 服务层覆盖提交逻辑;④ 全量刷新(「刷新全部」)的互斥锁 `ClusterService.syncGate` 是**静态**信号量,并行执行时本轮可能仍在等锁——bUnit 只能断言进度行以「已完成 / 总数」形态渲染与按钮禁用态,具体进度数值由服务层测试覆盖。
- **页面测试依赖栈**:`BunitServiceExtensions` 提供 `AddClusterStack`、`AddGroupAndSyncStack`、`AddWorkloadServices`、`AddNamespaceStack`、`AddEventStack`、`AddDashboardStack`(K8s 服务统一经 `IClusterClientCache`,各栈内部已配 `AddClientCache`;YAML 模板用 `AddYamlTemplates`),一次性注册页面所需服务(仍受"首次 GetService 后冻结"约束,必须在首渲染前调用);仓库以端口类型注册(`IClusterRepository` 等)。
- **MTP runner gotchas**(xunit.v3 + MTP v2,由根 `global.json` `test.runner` 启用):不要传 VSTest 时代参数 —— `--nologo` 会被测试应用拒绝(exit 5,且误导性报告为 "Zero tests ran",见 dotnet/sdk#55309);零执行测试 = exit 8;过滤用 MTP/xunit 语法(`--filter-class`/`--filter-trait`/`--filter-method`),不是 VSTest 的 `--filter` 表达式;排查并行干扰可用 `--parallel none`。测试数量基线:770。
- `MultiClusterMgmtSys.Web` csproj `WarningsAsErrors` 含 `MUD0002`(MudBlazor 分析器)——组件 API 误用(如给无 `Value` 参数的组件 `@bind-Value`)会直接编译失败,不要用 `NoWarn` 绕过。
- 验证:`dotnet build` 0 错误 + `dotnet test` 全绿。

## Docker / prod deploy

- Deploy: `docker compose -f docker-compose.prod.yml up -d --build`. Do **not** use bare `docker compose up` — it merges the VS-debug `docker-compose.override.yml` (Development env + user-secrets mounts) and fails off-Windows.
- Prod stack is app + **nginx**: the app port is **not** exposed on the host; nginx terminates TLS on 80/443 (WebSocket upgrade for Blazor Server) and proxies to app :8080. Certs: `./nginx/certs/fullchain.pem` + `privkey.pem` (gitignored; self-signed generation commands are in the compose file comments).
- First run is fully automated: a one-shot `fs-init` container (same app image, root, `/bin/sh`) creates `db/ logs/ nginx/certs/` and chowns `db/`+`logs/` to the app UID (default 1654, from `USER $APP_UID` in `MultiClusterMgmtSys.Web/Dockerfile`; override with `MCMS_APP_UID` env var). It runs on every `up -d` (idempotent), and `multiclustermgmtsys` starts only after it (`service_completed_successfully`). Only the TLS certs remain manual: place `./nginx/certs/fullchain.pem` + `privkey.pem`.
- Env overrides in compose: `ConnectionStrings__DefaultConnection` → `/app/db/MultiClusterMgmtSys.db`, `Logging__File__Path` → `/app/logs/app-.log`; host `./db` and `./logs` are bind mounts. Backup = stop service, copy `db/MultiClusterMgmtSys.db`.
- Entry dll 为 `MultiClusterMgmtSys.Web.dll`(Dockerfile ENTRYPOINT),镜像/容器名仍为 `multiclustermgmtsys`。

## Database quirks (important)

- Schema is created with `db.Database.EnsureCreated()` in `Program.cs` at startup — **no EF migrations** in the repo. Model changes = drop/regenerate `MultiClusterMgmtSys.Web/db/MultiClusterMgmtSys.db`, not `dotnet ef migrations add`.
- Every startup `AccountService.CreateAdminAsync()` seeds roles and `admin` / `Changeme_123` (`Application/Services/AccountService.cs`)。Don't relocate that call without preserving the seed. Identity password policy is min-length 8 + at least one digit, nothing else (`InfrastructureServiceCollectionExtensions`)。
- `*.db` / `*.db-shm` / `*.db-wal` are **gitignored** runtime artifacts — never hand-edit; delete to reset local state. Active store: `MultiClusterMgmtSys.Web/db/MultiClusterMgmtSys.db`. Docker redirects to `/app/db`.
- Relative SQLite paths in the connection string are resolved against `ContentRootPath` at startup and the parent directory is auto-created in `InfrastructureServiceCollectionExtensions.AddInfrastructure` — SQLite creates files but not directories; deleting the whole `db/` folder is safe, next start recreates it.
- Connection string lives in `appsettings.json`; overriding requires user secrets (`UserSecretsId` set in csproj) or env vars, not another appsettings file.
- Schema facts (`Infrastructure/Persistence/ApplicationDbContext.cs`): `ClusterInfo.GroupId` FK is `SetNull`;`ClusterEndpoint`、`NodeIpRemark` 与 `ClusterHealthSnapshot` cascade from `ClusterInfo`;`NodeIpRemark` has a unique index `(ClusterId, NodeName, Address)`;`ClusterHealthSnapshot` has an index `(ClusterId, CapturedAt)`;`AuditLog.CreatedAt` is indexed;`ApplicationUser.CreatedAt` defaults to `CURRENT_TIMESTAMP`。
- **`ClusterHealthSnapshot` 是只追加表**(契约 `cluster-scheduled-sync`):后台同步每次**成功**探测一个集群即追加一行节点就绪统计(总数/就绪/未就绪),不覆盖、不修改;探测失败与停机取消均不写。因此「每集群最新一行」即该集群最近一次成功探测的结果——看板据此计算舰队规模,集群离线时主档 `NodeCount` 虽被置 0,规模仍取快照的最后已知值而不缩水。目前**无保留策略**(7 集群 / 5 分钟间隔约 2000 行/天,SQLite 无感)。
- **`add-cluster-dashboard` 引入建表变更**:升级到该版本必须删除 `MultiClusterMgmtSys.Web/db/` 下的库文件后重启重建(`EnsureCreated` 不会为既有库补建新表),已登记的集群与凭据需重新录入。

## Folder / namespace gotchas

- 目录/命名空间/项目已对齐:`Domain/Entities|Enums|Exceptions` → `MultiClusterMgmtSys.Domain.*`;`Application/Services|Abstractions|Requests|ViewModels|Models|Enums|Common|Identity` → `MultiClusterMgmtSys.Application.*`;`Infrastructure/Persistence|Identity|Kubernetes|Templates|Sync` → `MultiClusterMgmtSys.Infrastructure.*`;Razor 跟随物理路径(`Web/Components/<Feature>/**` → `MultiClusterMgmtSys.Web.Components.<Feature>`)。**不要**凭旧记忆寻找 `MultiClusterMgmtSys.Services` / `.Data` / `.Components` / `.Common.Enums` / `.Models`(已不存在);测试命名空间为 `MultiClusterMgmtSys.Tests.<Layer>.*`。
- 易错点:**Service 管理目录是 `Web/Components/Svcs/`(命名空间 `.Web.Components.Svcs`,路由却是 `/services`)**,别去 `Components/Services` 找;命名空间管理在 `Web/Components/Namespaces/`(路由 `/namespaces`)。
- 宿主项目/程序集是 `MultiClusterMgmtSys.Web`(不是 `MultiClusterMgmtSys`);`MultiClusterMgmtSys.Domain|Application|Infrastructure` 是类库。

## Architecture notes

- `Program.cs`(Web):MudBlazor、Razor components(interactive server)、认证(cookie `MultiClusterMgmtSys.Auth`,8h sliding,login `/login`,access-denied `/access-denied`,default redirect `/dashboard`)、`AddApplicationServices()`、`AddInfrastructure(configuration, ContentRootPath)`、`AddDatabaseDeveloperPageExceptionFilter()`、Web 专属 UI 辅助服务(`ClusterSelectionState`、`RedirectManager`、`ExceptionPresenter`)。`ThemeManager` is **static** — not registered, never injected。
- **K8s 调用统一超时**:所有服务经 `Infrastructure/Kubernetes/KubernetesClientConfig.cs`(`internal static`,`InternalsVisibleTo` 对测试可见)构建 `KubernetesClientConfiguration` 并设 `HttpClientTimeout = 10s`,覆盖 kubeconfig 与 Token 两种连接方式(契约 `kubernetes-call-timeout`)。新增 K8s 服务复用 `KubernetesClientConfig.Build(cluster)`,**不要**再复制私有 `BuildConfig`;API Server 地址解析走端口 `IClusterClientCache.ResolveApiServer(cluster)`(探测后回填用)。
- **K8s 客户端缓存**:所有 K8s 服务经 singleton `IClusterClientCache.GetOrCreate(cluster)` 取客户端(契约 `k8s-client-cache`)——按集群 Id 缓存,凭据五项(连接方式/kubeconfig/ApiServer/Token/SkipTlsVerify)哈希指纹变化即重建,空闲 10 分钟驱逐并释放客户端。**不要**在服务里直接调工厂新建客户端或 `using var client` 释放;缓存是唯一创建路径,`KubernetesClientConfig.Build` 只被缓存调用。测试 mock 注入仍走工厂(bUnit 栈的 `AddClientCache` 已配好)。
- **端口/实现归位**:仓库端口 `IClusterRepository`/`IGroupRepository`/`IAuditLogRepository`/`IAppSettingRepository`/`IAccountQueryRepository` + `IClusterClientCache`/`IYamlTemplateService` 在 `Application/Abstractions`,实现在 `Infrastructure/*`;业务异常在 `Domain/Exceptions`,`K8sExceptionMapper` 在 `Application/Common/Exceptions`(与 k8s 调用同层)。
- **事件管理**:`Web/Components/Events/`(命名空间 `.Web.Components.Events`,路由 `/events`、`/events/{ClusterId:int}`)+ `EventService`(只读、无审计);相对时间展示统一 `Application/Common/Time/RelativeTimeFormatter.Format`(null→「—」,刚刚/N 分钟前/N 小时前/N 天前)。
- **全局集群看板**(契约 `cluster-dashboard`):`Web/Components/Dashboard/`(命名空间 `.Web.Components.Dashboard`,路由 `/dashboard`,**登录后默认落地页**)+ `DashboardService`。数据**全部读自 SQLite**(集群主档 + 节点健康快照 + 审计 + 同步设置),**不发起任何 K8s 调用**——集群全部离线时看板仍可用,`[刷新全部]` 复用 `ClusterService.RefreshAllClustersStatusAsync` 并带进度。新鲜度以**生效同步间隔 × 2** 为过期阈值(停用/从未同步各自表达,不误报为过期);「最近操作」复用审计可见范围(Admin 全站 / 非 Admin 仅本人,标题随之切换)。看板是聚合视图,**不**用 `MudTable`、不接 `ClusterSelectSidebar`、不提供管理操作——与集群列表页的分工是「健康视角」对「管理视角」。
- **全量刷新有界并发 + 停机取消**:`ClusterService.RefreshAllClustersStatusAsync` 三段式(取数 → `Parallel.ForEachAsync` 最多 4 个并发探测 → 串行 `UpdateAsync` + 状态翻转审计),进度用 `Interlocked`,`CancellationToken` 由 `ClusterSyncBackgroundService`(`Infrastructure/Sync`)透传;`ProbeAsync` 用 `catch (OperationCanceledException) when (token.IsCancellationRequested)` 区分停机取消与探测失败——取消不置 `Offline`、不写审计(契约 `cluster-scheduled-sync`)。探测**成功**时额外解析当轮 `ListNodeAsync` 结果的节点就绪统计并追加一条 `ClusterHealthSnapshot`(复用既有调用,**不新增 K8s 请求**);探测失败与停机取消均不写快照;快照落库失败只记警告,不把可达集群误判为离线。
- **YAML 模板**:`IYamlTemplateService`(接口在 `Application/Abstractions`,实现 `Infrastructure/Templates/YamlTemplateService.cs`,singleton)从 Web `wwwroot/templates/{category}/{name}.yaml` 读取新建对话框的初始 YAML(workload/configmap/service/namespace 四类);文件缺失或读取失败时回退内置最小骨架且只记日志、不抛异常。
- **Exception handling**: services throw `BusinessException` subclasses (中文 `UserMessage`,位于 `Domain/Exceptions`);K8s 调用点 catch → `K8sExceptionMapper.Translate(ex, "操作")` 再抛;UI catch → `await ExHandler.HandleAsync(ex, "操作")`,不直出 `ex.Message`。详见下方 "Exception handling" 节。
- Pipeline extras: `UseForwardedHeaders` trusting **all** proxies (required by prod nginx TLS termination — keep), `UseStatusCodePagesWithReExecute("/not-found")`, `MapStaticAssets()`, dev-only `UseMigrationsEndPoint` + `AddDatabaseDeveloperPageExceptionFilter`.
- Repositories surface data; services compose logic + K8s calls; `.razor` pages bind ViewModels via `Application/ViewModels/Mappings` extension methods.
- K8s credentials per `ClusterInfo`: `KubeConfig` / `Token` (TEXT), `ConnectionType` enum, `SkipTlsVerify` default `true`.
- `ClusterEndpoint` = admin-managed VIP/domain metadata, not from the K8s API (Kind enum `Domain/Enums/ClusterEndpointKind.cs`)。`NodeIpRemark` stores admin node-IP remarks merged into node reads by `ClusterNodeService`; only `InternalIP`/`ExternalIP` rows are eligible。
- `ApplicationUser` 在 `Application/Identity`(Identity 归属 Application,契约 D3/architecture-layering);`IdentityRevalidatingAuthenticationStateProvider` 在 `Infrastructure/Identity`。

## UI / CSS conventions (Swiss Industrial Print)

- **`ThemeManager` is a static class** (`MultiClusterMgmtSys.Web.Components.Common`): use `ThemeManager.Theme`, never `@inject` it or register it in DI. Light-only — **no dark mode**: no `PaletteDark`, no `IsDarkMode`, no toggle button, no `mcm-theme-dark-mode` localStorage. Do not reintroduce.
- Tokens: paper `#F4F4F0`, surface `#FCFBF7`, ink `#111111`, hairline `#E2DED5`, secondary `#6E675C`, amber `#D97706` (brand/focus/progress only), radius 3px. Elevation = MudBlazor's default shadows + hairlines: content cards/panels keep the default `Elevation`(`MudPaper`/`MudCard` 默认 1;列表页空态卡 `Elevation="2"`;卡片内表格 `Elevation="0"`)——**不要**给内容卡片/面板写 `Elevation="0"`(看板页曾因此与全站不一致)。无阴影例外(仅发丝线):登录/注册 `.auth-panel`、兜底页 `.fallback-page`、AppBar/Drawer、tooltip、重连弹窗。
- Fonts self-hosted in `MultiClusterMgmtSys.Web/wwwroot/fonts/` (Space Grotesk variable + IBM Plex Mono 400/500/600, OFL): `.font-mono` for data columns (version/API/address/count/timestamp), `.font-grotesk` for display; body = system CJK stack. `tabular-nums` is global on `html`.
- Status display = `Components/Common/StatusBadge.razor`(`Text` 中文 / `CssClass` online|offline|unknown / 可选 `Raw` 英文次行),内部渲染 `.status-badge` + `.status-dot`。Do **not** use filled `MudChip` for status. Mapping: Ready/Active→online, Offline/NotReady→offline, else unknown.
- **双语展示约定**(契约 `display-conventions`):枚举值用 `Components/Common/StackedText.razor`(中文主行 + 等宽英文次行),映射统一走 `Application/ViewModels/Mappings/K8sDisplayText.cs`(纯静态,未登记值回退原文;raw 字段保留供排序/过滤/提交),字段标签用「中文 (English)」;筛选下拉改双语标签时提交值必须保持 raw。Tooltip 统一 `TextTooltip.razor`(支持 `Mono`)或图标用 `TooltipIconButton.razor`,**禁止原生 `title=`**。
- 行内图标操作统一用 `Components/Common/TooltipIconButton.razor`(`Icon`/`Text`/`Color`/`Class`/`Disabled`/`OnClick`;`Text` 同时作为 tooltip 与 `aria-label`,点击自动收起 tooltip)。按钮忙碌态加 `Class="icon-busy"`(原地旋转图标,不撑动行高),别在按钮里塞 `MudProgressCircular`。
- Name links use `.link-primary` (amber, hover underline) — not `Color="Color.Primary"` + permanent underline.
- Full-height pages (YAML view/edit): page `MudStack` needs `Class="flex-auto d-flex" Style="align-self: stretch; min-height: 0;"`, then `flex: 1 1 auto; min-height: 0` down the chain. Do **not** use `calc(100vh)` offsets.
- **页面根节点必须带 `flex-auto`**:`MainLayout` 的 `MudContainer` 是 `d-flex`(**row**),`@Body` 的根元素是行内 flex item——漏掉 `flex-auto` 会收缩到内容宽度(看板页实测踩过:卡片只有约 175px 宽)。普通内容页写 `Class="flex-auto"`(`Profile.razor` 是 `flex-auto pb-4`、`Events.razor` 是 `flex-auto`);表格页由 `{feature}-table` 规则组负责。内容可能超出视口高度时,该页样式可加 `min-height: 0`,但**不要**在页面根节点加 `overflow-y: auto`——滚动容器会在其边界裁掉卡片的投影阴影(看板页实测踩过),让文档滚动即可(与其他内容页一致)。
- 列表页表格类命名 `{feature}-table`(`.clusters-table`/`.namespaces-table`/…)且**必须登记进 app.css 的 flex-fill 规则组**(本体、`.mud-table-container`、`.mud-table-pagination` 三处选择器)——漏登记的表格不会拉伸到与浏览器适配的高度。
- 看板页(`/dashboard`)是聚合快照而非表格页:样式在 `app.css` **第 15 节**,**不**登记进上面的表格 flex-fill 规则组。结构为报头(`.dashboard-masthead`,标题 + 新鲜度 + 刷新入口)+ KPI 带(`.dashboard-stat-bar`,1px 发丝线分隔的 5 格)+ 双栏主体(`.dashboard-columns`:左栏「需要关注 + 最近操作」、右栏「分组健康 + 版本分布」);区块统一用 `.dashboard-card` 卡壳(纸色卡头 `.dashboard-card-head` + 发丝线行),行类为 `.dashboard-attention-row`/`.dashboard-breakdown-row`/`.dashboard-activity-row`,计量条为 `.dashboard-meter`/`.dashboard-meter-fill`。大号数字用 `.dashboard-stat-value` + `.font-grotesk`(40–48px 紧凑字距,`html ` 前缀压过 Mud 的 body1 字号),数据行用 `.font-mono`,状态计数仅在非零时着色(在线绿/离线红/未知琥珀)。**注意**:组件属性(如 `MudText` 的 `Class`)不接受字面量与 C# 表达式混排(编译错误 RZ9986),条件类名必须整体在 `@code` 中算好。
- **YAML view/edit cards use a plain `<textarea class="yaml-textarea">` inside `MudCard Class="pa-4 yaml-card"`**, NOT `MudTextField` with `Lines`。Bind via `value` + `@oninput`。
- Empty states: `.empty-state` (mono dashed box `[ 暂无… ]`); table loading text `// 正在加载...` in `.font-mono`. Auth cards: `Elevation="0"` + `.auth-panel` hairline. Brand: `.brand-mark` amber square (28px; `.large` 40px on login/register) + `.appbar-subtitle` `MCM // CONTROL`.
- **`MudDateRangePicker` 不能用 `@bind-Value`**——MudBlazor 该组件继承链上无 `Value` 参数,属性会被静默吞进 `UserAttributes`,选完日期不回写。必须 `DateRange="..."` + `DateRangeChanged="OnDateRangeChanged"` 显式绑定(ClusterFilterBar 是范本)。
- 紧凑对话框(改密/重置密码)用 `Class="pwd-dialog"` + app.css 的 `[class~="pwd-dialog"]` 规则去除内部滚动条。
- **断线重连弹窗**:`ReconnectModal.razor` 的样式位于全局 `app.css`(第 13 节,不依赖组件 scoped CSS,顺带规避 dev-run 样式包 500 怪癖);模块脚本位于 `wwwroot/js/reconnect.js`(从 collocated 位置迁出,规避 dev-run 静态资产 500 怪癖),引用 `@Assets["js/reconnect.js"]`;其原生 `<dialog>` 与按钮 id(`components-reconnect-*`、`components-reload-button`)是框架/组件 JS 契约,保持原生(ui-theme 例外);重连状态类名与倒计时 span 不得更名。**MudBlazor 自带 `#components-reconnect-modal button` 样式**(主题变量颜色 + `margin:40px auto !important`)且弹窗在 `MudThemeProvider` 之外,覆盖必须用 `dialog#components-reconnect-modal ...` 级选择器;进度线由 `MudProgressLinear` + `app.css` 自绘轨道/滑段(不依赖 Mud 内部 bar 的颜色与动画)。
- **兜底页面**:`Pages/NotFound.razor`(`/not-found`,Router `NotFoundPage`)与 `Pages/Error.razor`(`/Error`,`UseExceptionHandler`)使用 `.fallback-page` 发丝线卡片 + mono 代码行 + 中文文案 + 「返回集群列表」入口,不保留 Blazor 模板英文与开发环境说明;两页均用 `EmptyLayout`。

## Exception handling

- 业务异常层次 `Domain/Exceptions/`:`BusinessException`(抽象,中文 `UserMessage`)+ `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException` / `ClusterUnreachableException`。
- K8s 异常翻译:`K8sExceptionMapper.Translate(ex, "操作")`(`Application/Common/Exceptions`)。**KubernetesClient 19 的 `KubernetesClientException` 已无状态码属性**——状态码在 `k8s.KubernetesException.Status.Code`(V1Status)与 `k8s.Autorest.HttpOperationException.Response.StatusCode`;映射 404→NotFound、409→Conflict、403/401→Permission、400→Validation(取 `Status.Message`)、超时→ClusterUnreachable,5xx/未知原样返回当系统异常。
- 服务层:每个调 K8s 的方法包 try/catch → LogWarning(含操作与 id 上下文)→ Translate 再抛;业务规则失败直接抛业务异常(中文)。优雅降级分支保留(如 `ProbeAsync` 失败置 `Offline`、详情页节点加载失败置 `IsReachable=false`),不向用户弹错。
- UI 层:`ExceptionPresenter`(`Components/Common`,scoped)统一提示——`catch (Exception ex) { await ExHandler.HandleAsync(ex, "操作"); }`;业务异常显示 `UserMessage`(Conflict→Warning,其余 Error),非业务异常显示「{操作}失败,请稍后重试」并 LogError。**不要直出 `ex.Message`**(唯一例外:YAML 本地解析错误包成 `ValidationException($"YAML 格式错误:{ex.Message}")`)。
- `AuditService.LogAsync` 写失败保持静默(catch + LogWarning),不打扰用户。

## OpenCode / OpenSpec

- OpenSpec skills under `.opencode/skills/openspec-*`, commands under `.opencode/commands/opsx-*`; the repo uses an `openspec/` workflow (`changes/`, `changes/archive/`, `specs/`, `config.yaml`). Use the skills instead of hand-editing those folders.
- `openspec/config.yaml` has only `schema: spec-driven`; the tech-stack `context:` block is commented-out placeholder.
- `openspec/specs/architecture-layering/spec.md` 是分层契约(四项目依赖方向、端口归位、Web 组件命名空间隔离、架构测试规则)。
- `openspec/specs/cluster-query-layering/spec.md` is the contract for `ClusterPageQuery.GroupId` (null=no filter, `0`=ungrouped sentinel → repo translates to `WHERE GroupId IS NULL`, positive=equality) and the version filter sentinels (`VersionFilterSentinel.All` = `""`, `OnlyNull` = `"__null__"`). Must not drift from `Application/Models/ClusterPageQuery.cs` + `Infrastructure/Persistence/ClusterRepository.cs`.
- `openspec/specs/ui-theme/spec.md` is the design-system contract — keep code consistent with it when touching UI.
- `openspec/specs/display-conventions/spec.md` 是全站展示契约——改展示前先读。
- `openspec/specs/exception-handling/spec.md` 是异常契约——改异常逻辑前先看它。
- `openspec/specs/unit-testing/spec.md` 是测试与覆盖率口径契约;其余 feature 契约在 `openspec/specs/` 下同名目录(`workload-management`、`service-management`、`accounts-page`、`audit-log`、`cluster-scheduled-sync`、`detail-page-tabs`、`node-detail-layout`、`nodes-page`、`node-ip-notes`、`configmaps-page`、`cluster-endpoints`、`cluster-detail`、`cluster-edit`、`clusters-group-navigation`、`service-contracts`、`code-style`、`k8s-client-cache`、`kubernetes-call-timeout` 等);动对应 feature 前先读其 spec,行为以 spec 为准。
- OpenSpec `tasks.md` files use `- [ ]`/`- [x]` checkboxes that the apply skill parses — preserve this exact format.

## Conventions to preserve

- Services log `logger.LogInformation` at enter/done and `logger.LogWarning` on failures; mutating methods (create/update/delete/move/rename/login/logout/register/batch ops) write audit entries via `auditService.LogAsync(AuditCategory.X, AuditAction.Y, "中文描述")` after success (enums in `Domain/Enums/AuditCategory.cs` / `AuditAction.cs`, entity `Domain/Entities/AuditLog.cs`)。
- `_Imports.razor` is the source of implied `@using`s; check it before adding `@using` to pages.
- `BlazorDisableThrowNavigationException` is enabled in the Web csproj — leave it on.
- Prefer MudBlazor components over raw HTML/CSS. Documented exceptions only: (1) full-height YAML textareas (`yaml-textarea`), (2) `ReconnectModal.razor` raw buttons whose ids are Blazor framework JS contract, (3) Blazor built-in `InputFile`, (4) design-system display spans (`.status-badge`/`.brand-mark`/`.role-badge`/`.empty-state`/mono hint rows) — see ui-theme spec. Inline action controls inside a clickable row (`OnRowClick`/`@onclick`) must be wrapped in a `<span @onclick:stopPropagation="true">` (or `<div>`) — `@onclick` + `@onclick:stopPropagation` on the same MudBlazor component is a Razor error (RZ10010).
- Admin-only actions (create/rename/delete/batch/cluster delete/endpoints/IP notes/命名空间新建与删除) are gated via `<AuthorizeView Roles="Admin"><Authorized>`; view and filter actions are role-agnostic.
- Commit messages are short Chinese one-liners (e.g. `修改项目结构`, `重设计前端UI为工业印刷风格`) — match that style when asked to commit.

## Service contracts

- **输入**:服务方法接收前端传入的参数,一律收拢为 `Application/Requests/` 下的 `*Request` 对象;**单原语参数豁免**(如 `GetClusterDetailAsync(int id)`)。ViewModel 不得充当服务入参。
- **输出**:返回给前端展示的数据一律用 `Application/ViewModels/` 下的 ViewModel(含 `PagedResult<T>` 包装、`AccountBatchResult` 等结果类);不得返回 `Domain/Entities/` 实体。
- **豁免**:框架类型返回值(`IdentityResult`/`SignInResult`)、裸 `List<string>` 下拉数据、`AuditService.LogAsync`(内部审计)、`IProgress` 进度回调。
- **操作者上下文**:服务需要当前用户时,注入 `IHttpContextAccessor` 从 `HttpContext.User` 获取(如 `AccountService`、`AuthService`),**前端不得传 `currentUserId`/`currentUserName` 参数**;取不到身份且为安全判断前提时抛 `PermissionException`。
- **服务层禁前端框架类型**:`Application`(服务)与 `Infrastructure`(仓库)签名不得出现 MudBlazor 类型(`TableState`/`DateRange` 等);分页/排序/过滤并入 `*QueryRequest`。`MudTable` 的 `TableState` 在组件边界翻译成 Request 字段(见 `AccountTable`/`AuditLogs` 的 `LoadData`)。
- **目录归属**:`Application/Requests/` = 输入类(含输入编辑行 `NodeIpNoteEditItem`/`ClusterEndpointEditItem`);`Application/ViewModels/` = 展示输出类;`Application/Models/` = 其余(纯前端状态 `NodeListFilter`/`SvcListFilter`/`EventListFilter`、下拉选项 `EventKindOption`、查询对象/哨兵 `ClusterPageQuery` 含 `VersionFilterSentinel`)。

## Code style

- **Razor 注入**:一律用页面顶部的 `@inject`(`@using`/`@namespace` 块与 `@inject` 块之间空一行,紧随其后为标记区),`@code` 中不得出现 `[Inject]`。
- **Razor 参数注解**:`[Parameter]` / `[CascadingParameter]` 注解单独一行,属性声明独立一行;属性之间、属性与方法之间、方法之间空一行。
- **C# 成员间隔**:字段/属性/方法之间一律空一行(类声明后的首个成员不强制前置空行)。四个生产项目与宿主内的所有 .cs 适用。
- **XML 注释**:四个生产项目(Domain/Application/Infrastructure/Web)csproj 开启 `GenerateDocumentationFile`(CS1591 清零,**勿**加入 NoWarn);Domain(Entities/Enums/Exceptions)、Application(Services/Requests/ViewModels 含 Mappings 类型级/Models/Abstractions/Common)、Infrastructure(Persistence/Identity/Kubernetes/Templates/Sync)、Web(Components 下非 Razor 类型)的 public 类型/成员一律中文 `/// <summary>`(`/// ` 后带空格;枚举每成员一条;record 位置参数用 `<param>` 标注;.razor 与测试项目豁免)。契约注释须与对应 OpenSpec spec 语义一致。
- 验证方式:`dotnet build` 0 错误 + "连续成员行"静态审计零命中 + CS1591 零命中(`rg "^\s*///(?=\S)"` 审计缺空格)。
