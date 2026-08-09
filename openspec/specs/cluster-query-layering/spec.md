# cluster-query-layering

## Purpose

Define the contract between the cluster UI/service layer and the clusters repository for paged cluster queries. The repository consumes a pure, UI-agnostic query specification; the service owns translation from UI request DTOs and MudBlazor table state; string version sentinels in the UI request DTO are replaced by a typed enum.

## Requirements

### Requirement: Repository consumes a sentinel-free query specification

The clusters repository's paged query method SHALL accept a pure query-specification record whose fields are typed primitives or enums — not UI-layer request types and not string sentinels. The repository MUST NOT import any namespace under `MultiClusterMgmtSys.Components.*`.

#### Scenario: No reverse dependency from data layer to UI layer
- **WHEN** the repository assembly is compiled
- **THEN** `Data/Repositories/ClusterRepository.cs` has no `using MultiClusterMgmtSys.Components.Clusters.Requests;` and no reference to any `ClusterQueryRequest` type

#### Scenario: Version filtering uses semantic fields
- **WHEN** `ClusterPageQuery.HasVersion` is `null`
- **THEN** the repository applies no version filter
- **WHEN** `ClusterPageQuery.HasVersion` is `false`
- **THEN** the repository filters to clusters whose `Version` is null or empty
- **WHEN** `ClusterPageQuery.HasVersion` is `true` and `Version` is a non-empty string
- **THEN** the repository filters to clusters whose `Version` equals that string

#### Scenario: Other filters unchanged
- **WHEN** `ClusterPageQuery.GroupId`, `NameContains`, `Status`, `CreatedAfter`, or `CreatedBefore` is set
- **THEN** the repository applies the corresponding `Where` clause exactly as the pre-refactor repository did (group equality, name `Contains`, status equality, `CreatedAt >=` start, `CreatedAt <` end-of-day)

#### Scenario: Sort and paging assembly stays in repository
- **WHEN** `ClusterPageQuery.SortBy` and `SortDescending` are provided
- **THEN** the repository maps them to an `IOrderedQueryable` via the same switch over `ClusterSortField`, applies `ThenByDescending(c => c.Id)` as a stable tiebreaker, and clamps `Page`/`PageSize` to `Math.Max(value, 1)` before `Skip`/`Take`

### Requirement: Service translates UI request and table state into the repository query specification

`ClusterService` SHALL own the translation from `ClusterQueryRequest` (UI DTO) and MudBlazor `TableState` into `ClusterPageQuery` (repository DTO). The translation MUST be the only site that knows about string sentinels, MudBlazor sort labels, and 0-based page indexing from the table component.

#### Scenario: Version sentinel becomes a typed filter
- **WHEN** `ClusterQueryRequest.VersionFilter` is `All`
- **THEN** the service sets `ClusterPageQuery.HasVersion = null`
- **WHEN** `ClusterQueryRequest.VersionFilter` is `OnlyNull`
- **THEN** the service sets `ClusterPageQuery.HasVersion = false`
- **WHEN** `ClusterQueryRequest.VersionFilter` is `Specific` and `ClusterQueryRequest.Version` is a non-empty string
- **THEN** the service sets `ClusterPageQuery.HasVersion = true` and `ClusterPageQuery.Version` to that string

#### Scenario: TableState is translated in the service
- **WHEN** the `GetPagedAsync(TableState, ClusterQueryRequest)` overload is called
- **THEN** the service maps `TableState.SortLabel` ("Name"/"Status"/"Version"/"NodeCount"/"CreatedAt") to `ClusterSortField`, `TableState.SortDirection` to `SortDescending` (`!= Ascending`), and `TableState.Page` (0-based) to `ClusterPageQuery.Page` (1-based), then delegates to the single-request overload

#### Scenario: Existing public surface preserved
- **WHEN** a Razor page calls `ClusterService.GetPagedAsync(state, query)` or `ClusterService.GetPagedAsync(query)`
- **THEN** the method signatures are unchanged and return `PagedResult<ClusterViewModel>` as before

### Requirement: Typed version filter replaces string sentinels

`ClusterQueryRequest.Version` (a `string` defaulting to `"__ALL__"`) SHALL be replaced by a `VersionFilter` enum plus a companion `Version` string field that is only evaluated when `VersionFilter == Specific`. The `Common/Enums/VersionFilter` enum MUST define `All`, `OnlyNull`, and `Specific` variants and default to `All`.

#### Scenario: Default value matches old "no filter" behavior
- **WHEN** a new `ClusterQueryRequest` is constructed with no explicit version filter
- **THEN** `VersionFilter` is `All`, equivalent to the pre-refactor default `"__ALL__"`

#### Scenario: UI binds the enum directly
- **WHEN** `ClusterFilterBar.razor` renders the version `MudSelect`
- **THEN** its items bind to `VersionFilter` values (`All` shown as "全部", `OnlyNull` shown as "未知", and each available cluster version bound as `Specific` with its version string), and no literal `"__ALL__"` / `"__NULL__"` strings remain in the file

#### Scenario: Reset clears the filter
- **WHEN** `ClusterFilterBar.razor`'s reset is invoked
- **THEN** `Query.VersionFilter` is set to `All` (not to any string sentinel)