## Context

The clusters feature currently has a leaky boundary between `Data/Repositories/ClusterRepository.cs` and `Components/Clusters/Services/ClusterService.cs`. The repository's `GetPagedAsync` accepts `ClusterQueryRequest` — a UI-layer DTO living under `Components/Clusters/Requests/` — which forces the data layer to `using MultiClusterMgmtSys.Components.Clusters.Requests`. The request DTO also encodes the version dropdown's state with two magic strings (`"__ALL__"` = no filter, `"__NULL__"` = clusters with null version), and the repository branches on these literals. This is the only repo/service pair in the codebase with this problem; `GroupRepository`/`GroupService` and `ConfigMapService` already follow the intended split.

Constraints (from `AGENTS.md`):
- Single-project .NET 10 Blazor server app, SQLite via EF Core with `EnsureCreated()` (no migrations).
- Scoped DI; repositories surface data, services compose logic + K8s calls; `.razor` pages bind to ViewModels via `*.ViewModels.Mappings`.
- `Common/Enums/` already holds shared enums (`ClusterStatus`, `ConnectionType`, `ClusterSortField`).
- No test project; verification is `dotnet build` + manual smoke.

## Goals / Non-Goals

**Goals:**
- Eliminate the `Data.Repositories` → `Components.Clusters.Requests` reverse dependency.
- Eliminate UI-layer sentinel strings (`"__ALL__"` / `"__NULL__"`) from the data-access layer.
- Centralize "translate UI request + MudBlazor `TableState` into a repository query" in `ClusterService`, where business-logic translation belongs.
- Keep the repository's existing EF query assembly (filter `Where`, sort `switch`, stable `ThenByDescending(Id)`, `Math.Max` page clamping) in the repository — that is database-access work, not business logic.
- Preserve all current user-visible behavior on the clusters list page (filtering, sorting, paging, sentinel semantics translated 1:1).

**Non-Goals:**
- Refactoring `GroupRepository` / `GroupService` / `ConfigMapService` — already compliant.
- De-duplicating `BuildConfig` / `ProbeAsync` between `ClusterService` and `ConfigMapService`. (Noted for a future change; not needed to fix the layering.)
- Changing the DB schema, connection string, or Identity config.
- Introducing a test project, UnitOfWork, repository interfaces, or `IQueryable` exposure.
- Touching `ClusterDetail.razor` (currently fully commented out with `@* ... *@`).

## Decisions

### Decision 1: Translate via a query-specification DTO, not base-type params or `IQueryable`

`ClusterService` translates `ClusterQueryRequest` (+ `TableState`) into a new `ClusterPageQuery` record, which the repository consumes.

**Alternatives considered:**
- **P1 — Repository accepts `Expression<Func<ClusterInfo,bool>>` + enum + page/size**: Eliminates the DTO and the reverse dependency, but EF Core has no built-in `PredicateBuilder`; combining 5 optional filters requires either LinqKit (`AsExpandable`) or hand-rolled `Expression.And` parameter rewriting. High friction for a CRUD screen, and makes the repository body harder to read than the current `if (q.X) query = query.Where(...)`.
- **P3 — Repository exposes `IQueryable<ClusterInfo>` and Service composes**: Most flexible, but pushes `AsNoTracking`/`Include`/`Where`/`OrderBy` assembly into the service, which the project convention reserves for the repository. Also reopens the question of whether composing `IQueryable` counts as "database operations".

**Why P2 (chosen):** The repository body is *almost* correct today — it's only the **input type** that's wrong. Swapping `ClusterQueryRequest` for a sentinel-free `ClusterPageQuery` keeps the existing filter/sort/paging assembly logic in place verbatim, just reading typed semantic fields. Smallest diff, best readability, matches `GroupRepository`'s shape, and the translation cost lands entirely in the service (where it belongs).

### Decision 2: Replace string sentinels with a `VersionFilter` enum

New enum in `Common/Enums/VersionFilter.cs` (namespace `MultiClusterMgmtSys.Common.Enums`, matching the folder-to-namespace rule for `Common/`):

```
All         // was "__ALL__" — no version filtering
OnlyNull    // was "__NULL__" — clusters whose Version is null/empty
Specific    // a concrete version string
```

`ClusterQueryRequest.Version` changes from `string` (default `"__ALL__"`) to `VersionFilter` (default `All`); the actual version string moves to a new `Version`-adjacent field on the request only when `Specific` is selected — modeled as `ClusterQueryRequest.VersionFilter` + `ClusterQueryRequest.Version` (the specific string, ignored unless `VersionFilter == Specific`). This is **BREAKING** at the type level but the sole consumer is `ClusterFilterBar.razor` (a single `MudSelect` binding plus `ResetFilter`), so blast radius is one razor file.

`ClusterPageQuery` carries the translated form: `HasVersion` (`null` = no filter, `false` = only-null, `true` = specific) plus the optional `Version` string. The repository branches on `HasVersion`, never on string literals.

**Why not keep the strings and only isolate them:** Eliminating them removes a whole class of typo/contract bugs (compiler enforces the variants) and keeps the UI honest. Since the only consumer is one component, the cost is bounded.

### Decision 3: `ClusterPageQuery` lives under `Common/Queries/`

```
Common/Queries/ClusterPageQuery.cs   → namespace MultiClusterMgmtSys.Common.Queries
```

Rationale: `Common/` already hosts `PagedResult<>` (a shared shape crossing service↔UI) and `Common/Enums/` holds `ClusterSortField`/`ClusterStatus` that `ClusterPageQuery` references. Putting the query spec in `Common` (not `Data/`) keeps it free of EF references and lets both layers import it without a new reverse dependency. `Common/` follows the physical-path namespace rule per `AGENTS.md`, so `Common.Queries` is the correct namespace. The repository adds `using MultiClusterMgmtSys.Common.Queries;` — the same direction as its existing `using MultiClusterMgmtSys.Common.Enums;`.

### Decision 4: Repository still owns paging/sort assembly; service owns TableState translation

The current `ClusterService.GetPagedAsync(TableState, ClusterQueryRequest)` overload — which translates MudBlazor's `SortLabel` (string) → `ClusterSortField` and `SortDirection` → `bool`, and remaps `state.Page` (0-based) → 1-based — stays in the service. That is UI-adapter logic. The repository keeps its `switch (q.SortBy)` → `IOrderedQueryable`, the `ThenByDescending(c => c.Id)` stable tiebreaker, and `Math.Max(q.Page, 1)` clamping — those are DB-side query mechanics. `Math.Max` clamping is deliberately duplicated lightly rather than coupled across the boundary; the service clamps before sending so the spec is always well-formed, and the repository stays defensive.

### Decision 5: No interface abstractions introduced

`ClusterRepository` stays a concrete scoped class (matches `GroupRepository`, `ConfigMapService`, and the DI registration in `Program.cs`). No `IClusterRepository`. This is a layering refactor, not a DI decoupling exercise; adding interfaces now would be scope creep and inconsistent with the rest of the codebase.

## Risks / Trade-offs

- **[Risk] Sentinel semantics drift during translation** — `OnlyNull` must map to exactly `IsNullOrEmpty(Version)` and `Specific` to `Version == value`, matching today's `"__NULL__"`/`"__ALL__"` behavior. → Mitigation: the translation lives in one `ClusterService` method; the repository branch becomes `q.HasVersion switch { null => skip, false => Where(IsNullOrEmpty), true => Where(== q.Version) }` mirroring the original clauses. Manual smoke: filter "全部/未知/具体版本" and compare row counts before/after.
- **[Risk] `ClusterFilterBar.razor` binding breaks if enum names don't match MudSelect expectations** — `MudSelect<T>` needs the bind type to be the enum. → Mitigation: change `@bind-Value="Query.VersionFilter"` to `VersionFilter`; `MudSelectItem Value="@VersionFilter.All"` etc. MudBlazor renders enum names; if Chinese labels are wanted, use `MudSelectItem` child content text (current UI uses "全部"/"未知" labels already, so the items keep their text).
- **[Risk] No automated tests to catch regressions** — only `dotnet build` + manual smoke. → Mitigation: tasks.md calls out the exact manual smoke steps (clusters page load, each filter, sort, paginate, reset).
- **[Trade-off] Two DTOs for one concept** — `ClusterQueryRequest` (UI) + `ClusterPageQuery` (repo). Accepted: the extra type is the cost of a clean boundary; the translation is ~15 lines.
- **[Trade-off] `VersionFilter.Specific` carries the actual version string in a sibling field on the request** — slightly less elegant than a discriminated union, but C# records don't have sum types; this matches how `ClusterPageQuery.HasVersion` + `Version` already pairs up.