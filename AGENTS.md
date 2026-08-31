# AGENTS.md

Repo-specific guidance for OpenCode agents working in `MultiClusterMgmtSys`.

## Current state (2026-08-31)

Commit `88b0984` ("重设计前端UI为工业印刷风格") applied the Swiss Industrial Print redesign; the `redesign-ui-industrial-print` change is archived (2026-08-31) and synced to `openspec/specs/ui-theme/spec.md`. `dotnet build MultiClusterMgmtSys.slnx` passes (0 errors). If you ever see CS0246/CS0234 errors mentioning `MultiClusterMgmtSys.Components.<Feature>.Services`, `MultiClusterMgmtSys.Features.*` or `MultiClusterMgmtSys.Common.Queries`, that file's `@using`s are stale — target namespaces are in the Namespaces section below.

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
dotnet run  --project MultiClusterMgmtSys                                    # http://localhost:5021
dotnet run  --project MultiClusterMgmtSys --launch-profile https             # https://localhost:7081
```

No test project, no lint/format/typecheck config. Do not invent test commands.

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
- Razor under `Components/<Feature>/{Pages,Shared}` → `MultiClusterMgmtSys.Components.<Feature>`; some Configmaps shared components declare `@namespace MultiClusterMgmtSys.Features.Configmaps.Shared` explicitly

## Architecture notes

- `Program.cs`: MudBlazor, Razor components (interactive server), Identity (cookie `MultiClusterMgmtSys.Auth`, 8h sliding, login `/login`, access-denied `/access-denied`, default redirect `/clusters`), `ApplicationDbContext` (SQLite), scoped services/repos (`ClusterRepository`, `GroupRepository`, `AuditLogRepository`, `ClusterNodeService`, `ConfigMapService`, `ClusterService`, `GroupService`, `AuditService`, `ClusterSelectionState`, `AuthService`, `AccountService`, `RedirectManager`). `ThemeManager` is **static** (see UI section) — not registered, never injected.
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

## OpenCode / OpenSpec

- OpenSpec skills under `.opencode/skills/openspec-*`, commands under `.opencode/commands/opsx-*`; the repo uses an `openspec/` workflow (`changes/`, `changes/archive/`, `specs/`, `config.yaml`). Use the skills instead of hand-editing those folders.
- `openspec/config.yaml` has only `schema: spec-driven`; the tech-stack `context:` block is commented-out placeholder.
- `openspec/specs/cluster-query-layering/spec.md` is the contract for `ClusterPageQuery.GroupId` (null=no filter, `0`=ungrouped sentinel → repo translates to `WHERE GroupId IS NULL`, positive=equality) and the version filter sentinels (`VersionFilterSentinel.All` = `""`, `OnlyNull` = `"__null__"`). Must not drift from `Models/ClusterPageQuery.cs` + `Data/Repositories/ClusterRepository.cs`.
- `openspec/specs/ui-theme/spec.md` is the design-system contract (tokens, fonts, status badges, AppBar/nav/table rules, dark-mode removal) — keep code consistent with it when touching UI.
- OpenSpec `tasks.md` files use `- [ ]`/`- [x]` checkboxes that the apply skill parses — preserve this exact format.

## Conventions to preserve

- Services log `logger.LogInformation` at enter/done and `logger.LogWarning` on failures; mutating methods (create/update/delete/move/rename/login/logout/register/batch ops) write audit entries via `auditService.LogAsync(AuditCategory.X, AuditAction.Y, "中文描述")` after success (enums in `Common/Enums/AuditCategory.cs` / `AuditAction.cs`, entity `Data/Entities/AuditLog.cs`).
- `_Imports.razor` is the source of implied `@using`s; check it before adding `@using` to pages.
- `BlazorDisableThrowNavigationException` is enabled in the csproj — leave it on.
- Prefer MudBlazor components over raw HTML/CSS (only exception: full-height YAML textareas, see UI section). Inline action buttons inside a `@onclick` row must be wrapped in `<span @onclick:stopPropagation="true">` — `@onclick` + `@onclick:stopPropagation` on the same MudBlazor component is a Razor error (RZ10010).
- Admin-only actions (create/rename/delete/batch/cluster delete/endpoints/IP notes) are gated via `<AuthorizeView Roles="Admin"><Authorized>`; view and filter actions are role-agnostic.
- Commit messages are short Chinese one-liners (e.g. `修改项目结构`, `重设计前端UI为工业印刷风格`) — match that style when asked to commit.