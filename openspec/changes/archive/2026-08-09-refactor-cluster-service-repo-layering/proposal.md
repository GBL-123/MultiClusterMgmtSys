## Why

`ClusterRepository.GetPagedAsync(ClusterQueryRequest)` causes a reverse layer dependency (`Data.Repositories` → `Components.Clusters.Requests`) and leaks UI-layer sentinel strings (`"__NULL__"` / `"__ALL__"`) into the data-access layer. This violates the project's stated layering convention ("repositories surface data, services compose logic") and sets a precedent that future features could follow. Refactoring now corrects the boundary before more code copies the pattern, and replaces the fragile string sentinels with a typed model so the compiler catches misuse.

## What Changes

- **Introduce `ClusterPageQuery`** — a pure, sentinel-free query specification record consumed by the repository layer (carries semantic fields like `HasVersion` instead of magic strings).
- **Introduce `VersionFilter` enum** — typed replacement for the version-dropdown sentinels, used by both the UI request model and the repository query model.
- **`ClusterRepository.GetPagedAsync`** signature changes from `ClusterQueryRequest` → `ClusterPageQuery`; repository body keeps the same EF query assembly logic but reads semantic fields instead of matching sentinel strings. No longer references `Components.Clusters.Requests`.
- **`ClusterService`** takes responsibility for translating `ClusterQueryRequest` (UI) + `MudBlazor.TableState` → `ClusterPageQuery` (repo). Sentinel-to-semantics translation happens here and only here.
- **`ClusterQueryRequest.Version`** type changes from `string` (default `"__ALL__"`) to `VersionFilter` (default `All`). **BREAKING** at the type level for any consumer binding this property, but the only known consumer is `ClusterFilterBar.razor`.
- **`ClusterFilterBar.razor`** version `MudSelect` items bind to `VersionFilter` enum values instead of literal `"__ALL__"` / `"__NULL__"` strings; `ResetFilter` resets to `VersionFilter.All`.
- `GroupRepository` / `GroupService` / `ConfigMapService` / Razor pages: no signature changes, no edits.

## Capabilities

### New Capabilities
- `cluster-query-layering`: Separation of concerns between the clusters service and repository for paged cluster queries — repository consumes a pure query specification, service translates UI request/table state into that specification, and no UI-layer types or sentinel strings cross the boundary.

### Modified Capabilities
<!-- No existing specs to modify. -->

## Impact

- **Code**: `Data/Repositories/ClusterRepository.cs`, `Components/Clusters/Services/ClusterService.cs`, `Components/Clusters/Requests/ClusterQueryRequest.cs`, `Components/Clusters/Shared/ClusterFilterBar.razor`. New files: a `ClusterPageQuery` record, a `VersionFilter` enum in `Common/Enums`.
- **Dependencies**: Removes `using MultiClusterMgmtSys.Components.Clusters.Requests;` from the data layer. No new package dependencies.
- **APIs**: No public API surface; this repo is a Blazor server app with no external API contract. Internal method-signature change on `ClusterRepository.GetPagedAsync` is **BREAKING** for any in-repo caller, but only `ClusterService` calls it.
- **Data**: No schema changes; SQLite DB unaffected. No migrations.
- **Tests**: No test project exists; verification is `dotnet build` + manual smoke of the clusters list page.