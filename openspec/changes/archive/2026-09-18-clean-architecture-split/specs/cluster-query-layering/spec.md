# cluster-query-layering Delta

## MODIFIED Requirements

### Requirement: Repository consumes a sentinel-free query specification

The clusters repository's paged query method SHALL accept a pure query-specification record whose fields are typed primitives or enums — not UI-layer request types and not string sentinels. The repository SHALL live in the `MultiClusterMgmtSys.Infrastructure` project (`Persistence` folder), which does not reference `MultiClusterMgmtSys.Web`; the reverse dependency from data layer to UI layer is therefore prevented by project references, not only by convention.

The previous requirement is preserved except for the `GroupId` field: it gains a third state (`0` = ungrouped sentinel) that the repository MUST translate to a `WHERE GroupId IS NULL` predicate. All other clauses (NameContains, Status, Version, DateRange, SortBy, Page, PageSize) remain unchanged.

#### Scenario: No reverse dependency from data layer to UI layer
- **WHEN** the solution is compiled
- **THEN** `Infrastructure/Persistence/ClusterRepository.cs` contains no `using MultiClusterMgmtSys.Web.*;` and no reference to any `ClusterQueryRequest` type, and `MultiClusterMgmtSys.Infrastructure` has no project reference to `MultiClusterMgmtSys.Web`

#### Scenario: GroupId null means no filter
- **WHEN** `ClusterPageQuery.GroupId` is `null`
- **THEN** the repository applies no group predicate, returning clusters regardless of `GroupId` value (including null and non-null)
- **AND** this behavior is unchanged from the pre-change repository

#### Scenario: GroupId zero means ungrouped sentinel
- **WHEN** `ClusterPageQuery.GroupId` is `0`
- **THEN** the repository applies `WHERE ClusterInfo.GroupId IS NULL` (EF Core null comparison)
- **AND** only clusters without an assigned group are returned
- **AND** the repository MUST NOT translate `0` to a literal `WHERE GroupId == 0` clause (no real group has id 0)

#### Scenario: GroupId positive value means equality
- **WHEN** `ClusterPageQuery.GroupId` is a positive `int`
- **THEN** the repository applies `WHERE ClusterInfo.GroupId == <value>` exactly as the pre-change repository did for any non-null value

#### Scenario: Version filtering uses semantic fields
- **WHEN** `ClusterPageQuery.HasVersion` is `null`
- **THEN** the repository applies no version filter
- **WHEN** `ClusterPageQuery.HasVersion` is `false`
- **THEN** the repository filters to clusters whose `Version` is null or empty
- **WHEN** `ClusterPageQuery.HasVersion` is `true` and `Version` is a non-empty string
- **THEN** the repository filters to clusters whose `Version` equals that string

#### Scenario: Other filters unchanged
- **WHEN** `ClusterPageQuery.NameContains`, `Status`, `CreatedAfter`, or `CreatedBefore` is set
- **THEN** the repository applies the corresponding `Where` clause exactly as the pre-refactor repository did (name `Contains`, status equality, `CreatedAt >=` start, `CreatedAt <` end-of-day)

#### Scenario: Sort and paging assembly stays in repository
- **WHEN** `ClusterPageQuery.SortBy` and `SortDescending` are provided
- **THEN** the repository maps them to an `IOrderedQueryable` via the same switch over `ClusterSortField`, applies `ThenByDescending(c => c.Id)` as a stable tiebreaker, and clamps `Page`/`PageSize` to `Math.Max(value, 1)` before `Skip`/`Take`
