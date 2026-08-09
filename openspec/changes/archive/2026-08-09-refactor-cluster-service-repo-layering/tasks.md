## 1. New shared types

- [x] 1.1 Create `Common/Enums/VersionFilter.cs` with `enum VersionFilter { All, OnlyNull, Specific }`, namespace `MultiClusterMgmtSys.Common.Enums`
- [x] 1.2 Create `Common/Queries/ClusterPageQuery.cs` as a `record` with fields: `int? GroupId`, `string? NameContains`, `ClusterStatus? Status`, `bool? HasVersion` (null=no filter, false=only-null, true=specific), `string? Version`, `DateTime? CreatedAfter`, `DateTime? CreatedBefore`, `ClusterSortField SortBy`, `bool SortDescending`, `int Page`, `int PageSize`. Namespace `MultiClusterMgmtSys.Common.Queries`

## 2. Update request DTO

- [x] 2.1 In `Components/Clusters/Requests/ClusterQueryRequest.cs`, change `public string Version { get; set; } = "__ALL__";` to `public VersionFilter VersionFilter { get; set; } = VersionFilter.All;` and add `public string? Version { get; set; }` (the specific version string, used only when `VersionFilter == Specific`)
- [x] 2.2 Add `using MultiClusterMgmtSys.Common.Enums;` to the request file (for `VersionFilter`)

## 3. Refactor repository

- [x] 3.1 In `Data/Repositories/ClusterRepository.cs`, change `GetPagedAsync(ClusterQueryRequest q)` to `GetPagedAsync(ClusterPageQuery q)` and remove `using MultiClusterMgmtSys.Components.Clusters.Requests;`; add `using MultiClusterMgmtSys.Common.Queries;`
- [x] 3.2 Replace the version-filter block: drop `q.Version == "__NULL__"` / `"__ALL__"` branches and use `q.HasVersion` — `null` → skip, `false` → `Where(c => string.IsNullOrEmpty(c.Version))`, `true` → `Where(c => c.Version == q.Version)`
- [x] 3.3 Rename references from `q.Name` to `q.NameContains`, `q.DateRange?.Start`/`End` to `q.CreatedAfter`/`q.CreatedBefore` (the service now passes resolved values). Keep all other `Where`/sort/`ThenByDescending(Id)`/`Math.Max` clauses unchanged
- [x] 3.4 Confirm no remaining `Components.*` import in the file; the only new import direction is `Common.*` (Enums, Queries), same as existing `Common.Enums`

## 4. Refactor service

- [x] 4.1 In `Components/Clusters/Services/ClusterService.cs`, add a private `ClusterPageQuery ToPageQuery(ClusterQueryRequest r)` that translates the request: `NameContains = r.Name`, `GroupId = r.GroupId`, `Status = r.Status`, version per `VersionFilter` switch (`All`→`HasVersion=null`/`Version=null`; `OnlyNull`→`HasVersion=false`/`Version=null`; `Specific`→`HasVersion=true`/`Version=r.Version`), date range from `r.DateRange?.Start`/`End` (apply same `SpecifyKind(Utc)` as the repo did), `SortBy`/`SortDescending` passed through, `Page`/`PageSize` clamped via `Math.Max(value, 1)`
- [x] 4.2 Update `GetPagedAsync(ClusterQueryRequest)` to call `ToPageQuery` then `repo.GetPagedAsync(query)`; keep returning `PagedResult<ClusterViewModel>` via the existing `ToViewModel()` mapping
- [x] 4.3 Update `GetPagedAsync(TableState, ClusterQueryRequest baseQuery)` — keep the `SortLabel`→`ClusterSortField` and `SortDirection`→`SortDescending` and 0-based→1-based `Page` mapping in place, assign onto `baseQuery`, then delegate to `GetPagedAsync(baseQuery)`. (This overload's public signature stays unchanged.)
- [x] 4.4 Add `using MultiClusterMgmtSys.Common.Queries;` to the service; remove any now-unused `using` for `Requests` only if no other method in the file still needs it (other methods reference `ClusterQueryRequest`, so keep the `Requests` import)

## 5. Update filter bar

- [x] 5.1 In `Components/Clusters/Shared/ClusterFilterBar.razor`, change the version `MudSelect`'s `@bind-Value` from `Query.Version` to `Query.VersionFilter` and update the items: `Value="@VersionFilter.All"` (text "全部"), `Value="@VersionFilter.OnlyNull"` (text "未知"), and per-available-version items binding `Value="@VersionFilter.Specific"` with the version carried — since `MudSelect<T>` binds a single value, set `Query.Version = version` and `Query.VersionFilter = VersionFilter.Specific` atomically via a small `OnVersionChanged(VersionFilter? selected, string version)` handler, OR if the per-version items set `Value="@VersionFilter.Specific"` only, set `Query.Version` from a separate select. Simplest: keep `MudSelect<VersionFilter>` for the mode (All/OnlyNull/Specific), and when `Specific` is chosen, populate `Query.Version` from `AvailableVersions`. Confirm the chosen approach keeps the existing UX (full list selectable)
- [x] 5.2 Add `@using MultiClusterMgmtSys.Common.Enums` to the top of `ClusterFilterBar.razor` (so `VersionFilter` resolves)
- [x] 5.3 Update `ResetFilter` to set `Query.VersionFilter = VersionFilter.All;` and `Query.Version = null;` instead of `Query.Version = "__ALL__"`
- [x] 5.4 Search the whole repo for any other literal `"__ALL__"` / `"__NULL__"` references and confirm none remain

## 6. Verify

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` — must succeed with no warnings about the removed `Components.*` import
- [ ] 6.2 `dotnet run --project MultiClusterMgmtSys` (http profile), browse `/clusters`, log in as `admin` / `Changeme_123`
- [ ] 6.3 Smoke: with no filters, the list loads and paginates as before
- [ ] 6.4 Smoke: set version filter to "全部" — all clusters shown; set to "未知" — only clusters with null/empty Version shown; set to a specific version — only clusters at that version shown
- [ ] 6.5 Smoke: click each sortable column header (名称/状态/版本/节点数/创建时间) ascending and descending — order matches pre-refactor (stable secondary sort by Id)
- [ ] 6.6 Smoke: enter a cluster name fragment, pick a group, pick a status, pick a date range — row set narrows as before
- [ ] 6.7 Smoke: click 重置 — all filters clear, `VersionFilter` returns to "全部", table reloads
- [ ] 6.8 Smoke: click into a cluster's detail, refresh status, edit, delete (admin) — none of these paths regressed (they don't touch `GetPagedAsync` but confirm the service still constructs `ClusterPageQuery` cleanly and `ProbeAsync`/`UpdateAsync` still work)