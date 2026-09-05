# AGENTS.md

Repo-specific guidance for OpenCode agents working in `MultiClusterMgmtSys`.

## Current state (2026-09-04)

Swiss Industrial Print redesign (88b0984)、业务异常体系 (`business-exception-handling`)、个人资料页重构 (`profile-page-redesign`)、统一代码风格 (`unify-code-style`)、单元测试 (`add-unit-tests`) 均已归档并同步 specs(`ui-theme`、`exception-handling`、`profile-page`、`code-style`、`unit-testing`)。`dotnet build MultiClusterMgmtSys.slnx` passes (0 errors),`dotnet test MultiClusterMgmtSys.Tests` 66/66 green. If you ever see CS0246/CS0234 errors mentioning `MultiClusterMgmtSys.Components.<Feature>.Services`, `MultiClusterMgmtSys.Features.*` or `MultiClusterMgmtSys.Common.Queries`, that file's `@using`s are stale — target namespaces are in the Namespaces section below.

## Stack

- .NET 10 / ASP.NET Core, Blazor **interactive server** render mode, MudBlazor 9.9.0 + `Extensions.MudBlazor.StaticInput` (`@using MudBlazor.StaticInput` lives in `_Imports.razor`)
- EF Core 10 with **SQLite** + ASP.NET Identity (roles `Admin`/`Member`, keys `int`)
- Kubernetes cluster access via `KubernetesClient` 19.0.2
- Serilog: console + daily rolling file `logs/app-.log` (30-day retention); path configurable via `Logging:File:Path`. EF SQL statement logs are Development-only (`Program.cs`).
- UI strings are **Chinese** (e.g. `ChineseIdentityErrorDescriber`, service messages, audit descriptions). Keep new user-facing strings consistent.
- Single project solution `MultiClusterMgmtSys.slnx` (new XML format; VS 17.14+ / current `dotnet`). Projects: `MultiClusterMgmtSys/MultiClusterMgmtSys.csproj`, `docker-compose.dcproj`.
- Design system is **Swiss Industrial Print, light-only**: static theme, no dark mode (see "UI / CSS conventions" below).

## Commands

```pwsh
dotnet build MultiClusterMgmtSys.slnx       # passes (0 errors)
dotnet test MultiClusterMgmtSys.Tests       # 66 tests, all green
dotnet run  --project MultiClusterMgmtSys                                    # http://localhost:5021
dotnet run  --project MultiClusterMgmtSys --launch-profile https             # https://localhost:7081
```

No test project? — there IS one: `MultiClusterMgmtSys.Tests/`(xUnit + Moq + bUnit + SQLite 内存库)。无 lint/format/typecheck config。Do not invent test commands.

## Testing conventions

- 测试项目 `MultiClusterMgmtSys.Tests/`,目录镜像主项目(`Services/`、`Common/`、`Components/`)+ `TestInfrastructure/`(SqliteDbFactory、TestData、SeedUser、TestServices、BunitHost)。
- **后端测试以服务为边界**:直接调 Service 公开方法,仓库经真实 SQLite 内存库(`SqliteDbFactory.CreateContext()`)覆盖;断言语义 = 业务异常类型 + 中文 UserMessage + 查询结果。
- **K8s 服务可测**:`Func<KubernetesClientConfiguration, IKubernetes>` 工厂注入(Program.cs 注册真实工厂);测试用 Moq mock 接口的 `*WithHttpMessagesAsync` 方法(扩展方法的底层),抛 `KubernetesException(new V1Status{Code=…})` 验证翻译链路。`TestServices.ThrowingFactory()` 返回惰性 mock(工厂调用不抛,真正走到 K8s 才失败)。
- **bUnit 只测"接线契约"**:`FindComponent<T>()` 取 MudBlazor 组件实例、触发公开事件/参数,断言自己组件的状态/渲染分支/自有 CSS 类(`.status-badge`/`.empty-state` 等);**禁止断言 `.mud-*` 内部 DOM**。bUnit 测试需要 `TimeProvider.System` + `JSRuntimeMode.Loose` + MudServices(BunitHost 已配)。
- 主项目 `WarningsAsErrors` 含 `MUD0002`(MudBlazor 分析器)——组件 API 误用(如给无 `Value` 参数的组件 `@bind-Value`)会直接编译失败,不要用 `NoWarn` 绕过。
- 验证:`dotnet build` 0 错误 + `dotnet test` 全绿。

Build gotcha: if `dotnet build` fails with MSB3021 (exe locked), a running app instance is holding `bin/.../MultiClusterMgmtSys.exe` — stop that process (or `Stop-Process` it) and rebuild.

## Docker / prod deploy

- Deploy: `docker compose -f docker-compose.prod.yml up -d --build`. Do **not** use bare `docker compose up` — it merges the VS-debug `docker-compose.override.yml` (Development env + user-secrets mounts) and fails off-Windows.
- Prod stack is app + **nginx**: the app port is **not** exposed on the host; nginx terminates TLS on 80/443 (WebSocket upgrade for Blazor Server) and proxies to app :8080. Certs: `./nginx/certs/fullchain.pem` + `privkey.pem` (gitignored; self-signed generation commands are in the compose file comments).
- First run on a Linux server: `mkdir -p db logs nginx/certs && sudo chown 1654:1654 db logs` (container runs as UID 1654, set via `USER $APP_UID` in `MultiClusterMgmtSys/Dockerfile`).
- Env overrides in compose: `ConnectionStrings__DefaultConnection` → `/app/db/MultiClusterMgmtSys.db`, `Logging__File__Path` → `/app/logs/app-.log`; host `./db` and `./logs` are bind mounts. Backup = stop service, copy `db/MultiClusterMgmtSys.db`.

## Database quirks (important)

- Schema is created with `db.Database.EnsureCreated()` in `Program.cs` at startup — **no EF migrations** in the repo. Model changes = drop/regenerate `MultiClusterMgmtSys/db/MultiClusterMgmtSys.db`, not `dotnet ef migrations add`.
- Every startup `AccountService.CreateAdminAsync()` seeds roles and `admin` / `Changeme_123` (`Services/AccountService.cs`). Don't relocate that call without preserving the seed. Identity password policy is min-length 8 + at least one digit, nothing else (`Program.cs`) — generated/seed passwords must satisfy it.
- `*.db` / `*.db-shm` / `*.db-wal` are **gitignored** runtime artifacts — never hand-edit; delete to reset local state. Active store: `MultiClusterMgmtSys/db/MultiClusterMgmtSys.db` (per `appsettings.json`, relative to project dir). Docker redirects to `/app/db`. A stray `clusters.db` may exist from old runs — unused, ignore it.
- Connection string lives in `appsettings.json`; overriding requires user secrets (`UserSecretsId` set in csproj) or env vars, not another appsettings file.
- Schema facts (`Data/ApplicationDbContext.cs`): `ClusterInfo.GroupId` FK is `SetNull` (deleting a group ungroups its clusters); `ClusterEndpoint` and `NodeIpRemark` cascade from `ClusterInfo`; `NodeIpRemark` has a unique index `(ClusterId, NodeName, Address)`; `AuditLog.CreatedAt` is indexed; `ApplicationUser.CreatedAt` defaults to `CURRENT_TIMESTAMP`.

## Namespaces — gotcha

Folder-to-namespace mapping is **inconsistent** (post-restructure). Never assume path == namespace — open a sibling file and copy its namespace. Current split:

- `Services/**` (incl. `Identity/`) → `MultiClusterMgmtSys.Services[.Identity]` — `AccountService`, `ClusterService`, `GroupService`, `ClusterNodeService`, `ConfigMapService`, `AuditService`, `AuthService`, and the Identity scaffolding (`ChineseIdentityErrorDescriber`, `IdentityComponentsEndpointRouteBuilderExtensions` → `MapAdditionalIdentityEndpoints()`, `IdentityRevalidatingAuthenticationStateProvider`)
- `ViewModels/**` (incl. `Mappings/`) → `MultiClusterMgmtSys.ViewModels[.Mappings]` — **EXCEPT** the 10 `Cluster*` ViewModels (ClusterViewModel, ClusterCreateViewModel, ClusterUpdateViewModel, ClusterEditViewModel, ClusterDetailViewModel, ClusterNodeViewModel, ClusterNodeDetailViewModel, ClusterGroupViewModel, ClusterEndpointViewModel, ClusterEndpointEditItem) which still declare `MultiClusterMgmtSys.Components.Clusters.ViewModels`
- `Requests/**` → `MultiClusterMgmtSys.Requests`
- `Models/**` → `MultiClusterMgmtSys.Models` (`ClusterPageQuery`, `VersionFilterSentinel`)
- `Common/Enums`, `Data/**`, `Components/Common` → match physical path (`MultiClusterMgmtSys.Common.Enums`, `.Data[.Entities|.Repositories]`, `.Components.Common`)
- Razor under `Components/<Feature>/{Pages,Shared}` → `MultiClusterMgmtSys.Components.<Feature>`(无 `@namespace` 覆盖,全部跟随物理路径);顶层 `Components/Pages`(Error/Home/NotFound)、`Components/Shared`(RedirectToLogin)、`Components/Layout`(AppBar/Drawer/MainLayout/ReconnectModal)同理 → `.Components.Pages` / `.Components.Shared` / `.Components.Layout`

## Architecture notes

- `Program.cs`: MudBlazor, Razor components (interactive server), Identity (cookie `MultiClusterMgmtSys.Auth`, 8h sliding, login `/login`, access-denied `/access-denied`, default redirect `/clusters`), `ApplicationDbContext` (SQLite), scoped services/repos (`ClusterRepository`, `GroupRepository`, `AuditLogRepository`, `ClusterNodeService`, `ConfigMapService`, `ClusterService`, `GroupService`, `AuditService`, `ClusterSelectionState`, `AuthService`, `AccountService`, `RedirectManager`, `ExceptionPresenter`). `ThemeManager` is **static** (see UI section) — not registered, never injected.
- **Exception handling**: services throw `BusinessException` subclasses (中文 `UserMessage`); K8s 调用点 catch → `K8sExceptionMapper.Translate(ex, "操作")` 再抛;UI catch → `await ExHandler.HandleAsync(ex, "操作")`,不直出 `ex.Message`。详见下方 "Exception handling" 节。
- Pipeline extras: `UseForwardedHeaders` trusting **all** proxies (required by prod nginx TLS termination — keep), `UseStatusCodePagesWithReExecute("/not-found")`, dev-only `UseMigrationsEndPoint` + `AddDatabaseDeveloperPageExceptionFilter`.
- Repositories surface data; services compose logic + K8s calls; `.razor` pages bind ViewModels via `*.ViewModels.Mappings` extension methods.
- K8s credentials per `ClusterInfo`: `KubeConfig` / `Token` (TEXT), `ConnectionType` enum, `SkipTlsVerify` default `true`.
- `ClusterEndpoint` = admin-managed VIP/domain metadata, not from the K8s API (Kind enum `Common/Enums/ClusterEndpointKind.cs`). `NodeIpRemark` stores admin node-IP remarks merged into node reads by `ClusterNodeService`; only `InternalIP`/`ExternalIP` rows are eligible.

## UI / CSS conventions (Swiss Industrial Print)

- **`ThemeManager` is a static class** (`MultiClusterMgmtSys.Components.Common`): use `ThemeManager.Theme`, never `@inject` it or register it in DI. Light-only — **no dark mode**: no `PaletteDark`, no `IsDarkMode`, no toggle button, no `mcm-theme-dark-mode` localStorage. Do not reintroduce.
- Tokens: paper `#F4F4F0`, surface `#FCFBF7`, ink `#111111`, hairline `#E2DED5`, secondary `#6E675C`, amber `#D97706` (brand/focus/progress only), radius 3px, elevation via hairlines + inset, not shadows.
- Fonts self-hosted in `wwwroot/fonts/` (Space Grotesk variable + IBM Plex Mono 400/500/600, OFL): `.font-mono` for data columns (version/API/address/count/timestamp), `.font-grotesk` for display; body = system CJK stack. `tabular-nums` is global on `html`.
- Status display = `<span class="status-badge online|offline|unknown"><span class="status-dot"></span>…</span>` (pale fill + deep text). Do **not** use filled `MudChip` for status. Mapping: Ready→online, Offline/NotReady→offline, else unknown.
- Name links use `.link-primary` (amber, hover underline) — not `Color="Color.Primary"` + permanent underline.
- Full-height pages (YAML view/edit): page `MudStack` needs `Class="flex-auto d-flex" Style="align-self: stretch; min-height: 0;"`, then `flex: 1 1 auto; min-height: 0` down the chain (same pattern as `.clusters-table` / `.mud-table-container`). Do **not** use `calc(100vh)` offsets.
- **YAML view/edit cards use a plain `<textarea class="yaml-textarea">` inside `MudCard Class="pa-4 yaml-card"`**, NOT `MudTextField` with `Lines` — MudTextField multiline renders `.mud-input-control > .mud-input > textarea.mud-input-root` (specificity fights CSS, and removing `Lines` silently downgrades it to a single-line input). Bind via `value` + `@oninput`.
- Empty states: `.empty-state` (mono dashed box `[ 暂无… ]`); table loading text `// 正在加载...` in `.font-mono`. Auth cards: `Elevation="0"` + `.auth-panel` hairline. Brand: `.brand-mark` amber square (28px; `.large` 40px on login/register) + `.appbar-subtitle` `MCM // CONTROL`.
- **`MudDateRangePicker` 不能用 `@bind-Value`**——MudBlazor 9.9 该组件继承链上无 `Value` 参数,属性会被静默吞进 `UserAttributes`,选完日期不回写。必须 `DateRange="..."` + `DateRangeChanged="OnDateRangeChanged"` 显式绑定(ClusterFilterBar 是范本)。
- 紧凑对话框(改密/重置密码)用 `Class="pwd-dialog"` + app.css 的 `[class~="pwd-dialog"]` 规则去除内部滚动条。

## Exception handling

- 业务异常层次 `Common/Exceptions/`:`BusinessException`(抽象,中文 `UserMessage`)+ `NotFoundException` / `ConflictException` / `ValidationException` / `PermissionException` / `ClusterUnreachableException`。
- K8s 异常翻译:`K8sExceptionMapper.Translate(ex, "操作")`。**KubernetesClient 19 的 `KubernetesClientException` 已无状态码属性**——状态码在 `k8s.KubernetesException.Status.Code`(V1Status)与 `k8s.Autorest.HttpOperationException.Response.StatusCode`;映射 404→NotFound、409→Conflict、403/401→Permission、400→Validation(取 `Status.Message`)、超时→ClusterUnreachable,5xx/未知原样返回当系统异常。
- 服务层:每个调 K8s 的方法包 try/catch → LogWarning(含操作与 id 上下文)→ Translate 再抛;业务规则失败直接抛业务异常(中文)。优雅降级分支保留(如 `ProbeAsync` 失败置 `Offline`、详情页节点加载失败置 `IsReachable=false`),不向用户弹错。
- UI 层:`ExceptionPresenter`(`Components/Common`,scoped)统一提示——`catch (Exception ex) { await ExHandler.HandleAsync(ex, "操作"); }`;业务异常显示 `UserMessage`(Conflict→Warning,其余 Error),非业务异常显示「{操作}失败,请稍后重试」并 LogError。**不要直出 `ex.Message`**(唯一例外:YAML 本地解析错误包成 `ValidationException($"YAML 格式错误:{ex.Message}")`)。
- `AuditService.LogAsync` 写失败保持静默(catch + LogWarning),不打扰用户。

## OpenCode / OpenSpec

- OpenSpec skills under `.opencode/skills/openspec-*`, commands under `.opencode/commands/opsx-*`; the repo uses an `openspec/` workflow (`changes/`, `changes/archive/`, `specs/`, `config.yaml`). Use the skills instead of hand-editing those folders.
- `openspec/config.yaml` has only `schema: spec-driven`; the tech-stack `context:` block is commented-out placeholder.
- `openspec/specs/cluster-query-layering/spec.md` is the contract for `ClusterPageQuery.GroupId` (null=no filter, `0`=ungrouped sentinel → repo translates to `WHERE GroupId IS NULL`, positive=equality) and the version filter sentinels (`VersionFilterSentinel.All` = `""`, `OnlyNull` = `"__null__"`). Must not drift from `Models/ClusterPageQuery.cs` + `Data/Repositories/ClusterRepository.cs`.
- `openspec/specs/ui-theme/spec.md` is the design-system contract (tokens, fonts, status badges, AppBar/nav/table rules, dark-mode removal) — keep code consistent with it when touching UI.
- `openspec/specs/exception-handling/spec.md` 是异常契约(业务异常类型、K8s 翻译、服务日志、UI 提示规则、审计静默)——改异常逻辑前先看它。
- OpenSpec `tasks.md` files use `- [ ]`/`- [x]` checkboxes that the apply skill parses — preserve this exact format.

## Conventions to preserve

- Services log `logger.LogInformation` at enter/done and `logger.LogWarning` on failures; mutating methods (create/update/delete/move/rename/login/logout/register/batch ops) write audit entries via `auditService.LogAsync(AuditCategory.X, AuditAction.Y, "中文描述")` after success (enums in `Common/Enums/AuditCategory.cs` / `AuditAction.cs`, entity `Data/Entities/AuditLog.cs`).
- `_Imports.razor` is the source of implied `@using`s; check it before adding `@using` to pages.
- `BlazorDisableThrowNavigationException` is enabled in the csproj — leave it on.
- Prefer MudBlazor components over raw HTML/CSS (buttons/inputs/selects/switches/tables/dialogs/cards/progress → MudBlazor). Documented exceptions only: (1) full-height YAML textareas (`yaml-textarea`), (2) `ReconnectModal.razor` raw buttons whose ids are Blazor framework JS contract, (3) Blazor built-in `InputFile` (no core MudBlazor equivalent), (4) design-system display spans (`.status-badge`/`.brand-mark`/`.role-badge`/`.empty-state`/mono hint rows) — see ui-theme spec. Inline action buttons inside a `@onclick` row must be wrapped in `<span @onclick:stopPropagation="true">` — `@onclick` + `@onclick:stopPropagation` on the same MudBlazor component is a Razor error (RZ10010).
- Admin-only actions (create/rename/delete/batch/cluster delete/endpoints/IP notes) are gated via `<AuthorizeView Roles="Admin"><Authorized>`; view and filter actions are role-agnostic.
- Commit messages are short Chinese one-liners (e.g. `修改项目结构`, `重设计前端UI为工业印刷风格`) — match that style when asked to commit.

## Service contracts

- **输入**:服务方法接收前端传入的参数,一律收拢为 `Requests/` 下的 `*Request` 对象;**单原语参数豁免**(如 `GetClusterDetailAsync(int id)`、`GetNamespacesAsync(int)`)。ViewModel 不得充当服务入参。
- **输出**:返回给前端展示的数据一律用 `ViewModels/` 下的 ViewModel(含 `PagedResult<T>` 包装、`AccountBatchResult` 等结果类);不得返回 `Data/Entities/` 实体。
- **豁免**:框架类型返回值(`IdentityResult`/`SignInResult`)、裸 `List<string>` 下拉数据(`GetAvailableVersionsAsync`/`GetNamespacesAsync`)、`AuditService.LogAsync`(内部审计)、`IProgress` 进度回调。
- **操作者上下文**:服务需要当前用户(用户名/ID/角色)时,注入 `IHttpContextAccessor` 从 `HttpContext.User` 获取(如 `AccountService`、`AuthService`),**前端不得传 `currentUserId`/`currentUserName` 参数**;取不到身份且为安全判断前提时抛 `PermissionException`。
- **服务层禁前端框架类型**:`Services/` 与 `Data/Repositories/` 签名不得出现 MudBlazor 类型(`TableState`/`DateRange` 等);分页/排序/过滤并入 `*QueryRequest`(对齐 `ClusterQueryRequest` 范本)。`MudTable` 的 `TableState` 在组件边界翻译成 Request 字段(见 `AccountTable`/`AuditLogs` 的 `LoadData`)。
- **目录归属**:`Requests/` = 输入类(含输入编辑行 `NodeIpNoteEditItem`/`ClusterEndpointEditItem`);`ViewModels/` = 展示输出类;`Models/` = 其余(纯前端状态 `NodeListFilter`、哨兵 `VersionFilterSentinel`)。

## Code style

- **Razor 注入**:一律用页面顶部的 `@inject`(`@using`/`@namespace` 块与 `@inject` 块之间空一行,紧随其后为标记区),`@code` 中不得出现 `[Inject]`。
- **Razor 参数注解**:`[Parameter]` / `[CascadingParameter]` 注解单独一行,属性声明独立一行;属性之间、属性与方法之间、方法之间空一行。
- **C# 成员间隔**:字段/属性/方法之间一律空一行(类声明后的首个成员不强制前置空行)。全项目(Components/Services/Data/ViewModels/Requests/Models/Common)适用。
- 验证方式:`dotnet build` 0 错误 + "连续成员行"静态审计(相邻两行均为成员声明即命中)零命中。