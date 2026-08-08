# AGENTS.md

Repo-specific guidance for OpenCode agents working in `MultiClusterMgmtSys`.

## Stack

- .NET 10 / ASP.NET Core, Blazor **interactive server** render mode, MudBlazor 9
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
- `MultiClusterMgmtSys.db` / `.db-shm` / `.db-wal` are committed under the project dir. They are runtime artifacts — do not hand-edit; delete to reset local state.
- Connection string lives in `appsettings.json` (`ConnectionStrings:DefaultConnection`); overriding it requires user secrets (`UserSecretsId` is set in the csproj) or env, not another appsettings file.

## Namespaces — gotcha

Folder-to-namespace mapping is **inconsistent** across feature folders. Match the existing namespace of neighbouring files when adding a file; do not assume path == namespace:

- `Components/Account/ViewModels/**`          → `MultiClusterMgmtSys.Features.Account.ViewModels[.Mappings]`
- `Components/Configmaps/Services/**`          → `MultiClusterMgmtSys.Features.Configmaps.Services`
- `Components/Clusters/**`, `Components/Nodes/**`, `Components/Auth/**`, `Data/**`, `Common/**` → `MultiClusterMgmtSys.[<Folder>...]*` matching the physical path.

`Program.cs` imports both `using MultiClusterMgmtSys.Features.Configmaps.Services;` and the `Components.*` ones — preserve that split when wiring up new services.

## Architecture notes

- Entrypoint: `Program.cs` registers MudBlazor, Razor components (interactive server), Identity (cookie `MultiClusterMgmtSys.Auth`, 8h sliding, login `/login`, default redirect `/clusters`), `ApplicationDbContext` (SQLite), and a set of scoped services/repositories (`ClusterRepository`, `GroupRepository`, `ClusterService`, `GroupService`, `ClusterNodeService`, `ConfigMapService`, `AuthService`, `AccountService`, `ThemeManager`, `RedirectManager`).
- Feature layout under `Components/<Feature>/{Pages,Shared,Services,Requests,ViewModels,ViewModels/Mappings}`; `Data` holds `ApplicationDbContext` + `Entities` + `Repositories`; `Common` holds shared `Enums`/`ViewModels` (e.g. `PagedResult<>`).
- `Components/Auth/Services/Identity/*` hosts the Identity scaffolding extensions (`IdentityRevalidatingAuthenticationStateProvider`, `IdentityComponentsEndpointRouteBuilderExtensions`, `ChineseIdentityErrorDescriber`) — `Program.cs` calls `MapAdditionalIdentityEndpoints()` from here.
- K8s credentials are stored per `ClusterInfo` (`KubeConfig` / `Token` columns, `SkipTlsVerify` defaults to `true`); see `Data/ApplicationDbContext.cs` for the model constraints.

## OpenCode / OpenSpec

- OpenCode skills for OpenSpec live under `.opencode/skills/openspec-*` and commands under `.opencode/commands/`. The repository uses an `openspec/` workflow (`changes/`, `specs/`, `config.yaml`). Use the `openspec-*` skills for proposing/implementing/archiving changes instead of editing those folders by hand.
- `openspec/config.yaml` is currently all commented-out placeholders — tech-stack context is not yet filled in.

## Conventions to preserve

- New services go through the scoped-DI pattern already in `Program.cs`; repositories surface data, services compose logic + K8s calls, `.razor` pages bind to ViewModels via `*.ViewModels.Mappings` extension methods.
- Razor `_Imports.razor` is the source of implied `@using`s; check it before adding `@using` to individual pages.
- `BlazorDisableThrowNavigationException` is enabled in the csproj — leave it on.