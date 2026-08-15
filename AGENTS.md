# AGENTS.md

Repo-specific guidance for OpenCode agents working in `MultiClusterMgmtSys`.

## Stack

- .NET 10 / ASP.NET Core, Blazor **interactive server** render mode, MudBlazor 9 (plus `Extensions.MudBlazor.StaticInput` — its `MudBlazor.StaticInput` namespace appears in `_Imports.razor`, that's the package source)
- EF Core 10 with **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`) + ASP.NET Identity (roles `Admin`/`Member`, keys `int`)
- Kubernetes cluster access via `KubernetesClient` 19
- UI strings are **Chinese** (e.g. `ChineseIdentityErrorDescriber`, service error messages). Keep new user-facing strings consistent.
- Single project solution: `MultiClusterMgmtSys.slnx` (new XML-based solution format; opens with VS 17.14+ / current `dotnet`). Projects: `MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` and `docker-compose.dcproj`.

## Commands

```pwsh
dotnet build MultiClusterMgmtSys.slnx
dotnet run  --project MultiClusterMgmtSys       # http://localhost:5021 (http profile), https://localhost:7081
dotnet run  --project MultiClusterMgmtSys --launch-profile https
```

Docker: `docker compose build` / `docker compose up` (uses `MultiClusterMgmtSys/Dockerfile`, ports 8080/8081).

There is **no test project** and no lint/format/typecheck config. Do not invent test commands.

## Database quirks (important)

- Schema is created with `db.Database.EnsureCreated()` in `Program.cs` at startup — there are **no EF migrations** in the repo. Changing the model means dropping/regenerating `MultiClusterMgmtSys.db`, not running `dotnet ef migrations add`.
- On every startup `AccountService.CreateAdminAsync()` seeds roles and an `admin` user with default password `Changeme_123` (see `Components/Account/Services/AccountService.cs`). Don't relocate that call without preserving the startup seed.
- `*.db` / `*.db-shm` / `*.db-wal` are **gitignored** (see `.gitignore` tail) and are NOT tracked — they are runtime artifacts, do not hand-edit; delete to reset local state. The active store is `MultiClusterMgmtSys.db` (per `appsettings.json`). A stray `clusters.db` may appear locally from older runs — it is unused; ignore it.
- Connection string lives in `appsettings.json` (`ConnectionStrings:DefaultConnection`); overriding it requires user secrets (`UserSecretsId` is set in the csproj) or env, not another appsettings file.

## Namespaces — gotcha

Folder-to-namespace mapping is **inconsistent** across feature folders, and even within a single feature folder. Do not assume path == namespace — open a sibling file in the same folder and copy its namespace. The current split:

- `Components/Account/ViewModels/**` (incl. `Mappings/`) → `MultiClusterMgmtSys.Features.Account.ViewModels[.Mappings]` (**`Features.*`**, not `Components.*`)
- `Components/Account/Services/**` → `MultiClusterMgmtSys.Components.Account.Services` (**`Components.*`**, not `Features.*`)
- `Components/Configmaps/**` (Services, ViewModels, Mappings) → `MultiClusterMgmtSys.Features.Configmaps.*` (**`Features.*`**)
- `Components/Clusters/**`, `Components/Nodes/**`, `Components/Auth/**` → `MultiClusterMgmtSys.Components.<Feature>.*` (matches physical path)
- `Data/**` → `MultiClusterMgmtSys.Data.*` (matches physical path)
- **Two different `Common` folders, different roots:** root `Common/**` (Enums/Queries/ViewModels like `PagedResult<>`) → `MultiClusterMgmtSys.Common.*` (matches path); `Components/Common/**` (`ThemeManager`, `RedirectManager`) → `MultiClusterMgmtSys.Components.Common` (NOT `MultiClusterMgmtSys.Common`).

`Program.cs` mixes both `using MultiClusterMgmtSys.Features.Configmaps.Services;` and the `MultiClusterMgmtSys.Components.*` ones — preserve whichever style the folder already uses when wiring up new services.

## Architecture notes

- Entrypoint: `Program.cs` registers MudBlazor, Razor components (interactive server), Identity (cookie `MultiClusterMgmtSys.Auth`, 8h sliding, login `/login`, default redirect `/clusters`), `ApplicationDbContext` (SQLite), and a set of scoped services/repositories (`ClusterRepository`, `GroupRepository`, `ClusterService`, `GroupService`, `ClusterNodeService`, `ConfigMapService`, `AuthService`, `AccountService`, `ThemeManager`, `RedirectManager`, `ClusterSelectionState`).
- Feature layout under `Components/<Feature>/{Pages,Shared,Services,Requests,ViewModels,ViewModels/Mappings}`; `Data` holds `ApplicationDbContext` + `Entities` + `Repositories`; `Common` holds shared `Enums`/`ViewModels` (e.g. `PagedResult<>`).
- `Components/Auth/Services/Identity/*` hosts the Identity scaffolding extensions (`IdentityRevalidatingAuthenticationStateProvider`, `IdentityComponentsEndpointRouteBuilderExtensions`, `ChineseIdentityErrorDescriber`) — `Program.cs` calls `MapAdditionalIdentityEndpoints()` from here.
- K8s credentials are stored per `ClusterInfo` (`KubeConfig` / `Token` columns, `SkipTlsVerify` defaults to `true`); see `Data/ApplicationDbContext.cs` for the model constraints.
- `ClusterInfo` also has a `ClusterEndpoint` one-to-many collection (`Endpoints`) — admin-managed VIP/Domain metadata that doesn't come from the K8s API. Kind enum lives in `Common/Enums/ClusterEndpointKind.cs`. Adding endpoints requires the `Endpoints` cascade to be honoured by `EnsureCreated()` (no migrations).
- `ClusterGroup` carries only `Name` (no `Description`); `ApplicationDbContext` enforces the schema, so any column add/remove requires dropping `MultiClusterMgmtSys.db`.

## OpenCode / OpenSpec

- OpenCode skills for OpenSpec live under `.opencode/skills/openspec-*` and commands under `.opencode/commands/`. The repository uses an `openspec/` workflow (`changes/`, `specs/`, `config.yaml`). Use the `openspec-*` skills for proposing/implementing/archiving changes instead of editing those folders by hand.
- `openspec/config.yaml` has only `schema: spec-driven` set; the tech-stack `context:` block is still commented-out placeholders.
- Main spec for the cluster query contract lives at `openspec/specs/cluster-query-layering/spec.md`; `ClusterPageQuery.GroupId` semantic there (null=no filter, `0`=ungrouped sentinel → repo translates to `WHERE GroupId IS NULL`, positive=equality) must not drift from the repository implementation.
- OpenSpec `tasks.md` files use `- [ ]`/`- [x]` checkbox format that the apply skill parses — preserve this exact format when editing task lists.

## Conventions to preserve

- New services go through the scoped-DI pattern already in `Program.cs`; repositories surface data, services compose logic + K8s calls, `.razor` pages bind to ViewModels via `*.ViewModels.Mappings` extension methods.
- Services log meaningful events with `logger.LogInformation` at enter/done and `logger.LogWarning` on failures — established pattern in `GroupService`, `ClusterService`. Match it for new service methods.
- Razor `_Imports.razor` is the source of implied `@using`s; check it before adding `@using` to individual pages.
- `BlazorDisableThrowNavigationException` is enabled in the csproj — leave it on.
- **Frontend: prefer MudBlazor components**; fall back to raw HTML/CSS only when MudBlazor genuinely can't fit the need. Documented workaround: inline action buttons inside a `@onclick` row must be wrapped in `<span @onclick:stopPropagation="true">` — `@onclick` + `@onclick:stopPropagation` on the same MudBlazor component is a Razor error (RZ10010).
- Admin-only actions (create / rename / delete / batch operations / cluster delete) are gated via `<AuthorizeView Roles="Admin"><Authorized>`; view and filter actions are role-agnostic.