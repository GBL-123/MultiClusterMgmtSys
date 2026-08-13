## Why

The cluster detail page (`/clusters/{id}`) is currently fully commented out at `Components/Clusters/Pages/ClusterDetail.razor`, so navigating from the cluster list's "view detail" action leads to a 404. The original 412-line commented design is also unsatisfying: it is a homogeneous stack of `MudCard` cards that ignores richer operational reality — in particular, clusters in this system may be reachable through **multiple cloud-vendor-supplied virtual IPs and multiple domains**, on top of the single `ApiServer` already stored on `ClusterInfo`. These endpoints are not discoverable from the Kubernetes API (they are provisioned out-of-band by the cloud provider), so they must be first-class admin-managed metadata that is persisted in the app DB and surfaced in the rebuilt detail view.

## What Changes

- **Rebuild** `Components/Clusters/Pages/ClusterDetail.razor` as an active "archive page" (the "A" layout tier): top toolbar with cluster name + status chip + Admin-only action group, followed by four discrete cards — Overview, **Cluster Endpoints** (new), Connection Info (Admin-only, payload-unmasked-on-demand), and a compact Nodes card with a "view all" link to `/nodes/{id}`. Delete the surrounding `@* ... *@` comment block so the `@page "/clusters/{Id:int}"` route becomes registered.
- **Add** a new `ClusterEndpoint` child entity (`int` PK, FK to `ClusterInfo.Id` with cascade delete, `Kind` enum, `Value`, `Note`, `IsPrimary`, `SortOrder`) persisted in SQLite. `Kind` starts with `Vip = 0` and `Domain = 1`; future kinds (e.g. `Bastion`) extend the enum.
- **Add** endpoint management UI as a dedicated `ClusterEndpointsDialog.razor` (Shared), invoked from a "管理" button on the detail page's Endpoints card. The dialog uses a full-replacement editor: add/remove rows inline, each row exposes `Kind` (select), `Value` (required, ≤ 256 chars), `Note` (optional, ≤ 64 chars), `IsPrimary` (radio, at most one primary per `Kind`), and an inline delete affordance. Saving submits a complete list — the service clears the existing set and re-inserts the surviving/new rows.
- **Add** one-click clipboard copy of any endpoint `Value` via MudBlazor's clipboard support. Each endpoint row in the detail view renders a copy affordance so operators can paste VIPs/domains straight into SSH/curl commands.
- **Extend** `AddClusterDialog.razor` to also embed the `ClusterEndpointEditor` block, so endpoints can be captured at cluster creation time. Leave `EditClusterDialog.razor` untouched — per `refactor-clusters-group-sidebar-layout/proposal.md` it is reserved for a separate change. (Cluster non-endpoint editing remains non-functional until that separate change; out of scope here.)
- **BREAKING (schema):** A new `ClusterEndpoints` table is added. Because the repo uses `db.Database.EnsureCreated()` with no EF migrations, this requires deleting `MultiClusterMgmtSys.db` (plus `-wal` / `-shm`) and letting the next startup rebuild the schema. Existing local cluster rows are lost; the admin seed `admin / Changeme_123` re-creates automatically on next startup.

## Capabilities

### New Capabilities
- `cluster-endpoints`: Persist, edit, and display administrator-defined per-cluster endpoints (VIPs and domains, with note and primary/secondary flag) as first-class metadata stored in the app database. Independent of Kubernetes API reachability.
- `cluster-detail`: The `/clusters/{id}` "archive page" — a single-page detail view that composes the cluster's own fields, its registered endpoints, its connection secrets (Admin-only), and a compact nodes preview into one navigable surface.

### Modified Capabilities
<!-- None. `cluster-query-layering` (the paged query contract) and `clusters-group-navigation` (the sidebar layout) are untouched. The repository's `GetByIdAsync` gains an `.Include(c => c.Endpoints)` call but that is not a spec-level requirement of `cluster-query-layering`. -->

## Impact

- **Code:**
  - `Data/Entities/ClusterEndpoint.cs` (new) — entity + `ClusterEndpointKind` enum lives in `Common/Enums/`.
  - `Data/Entities/ClusterInfo.cs` — add `Collection<ClusterEndpoint> Endpoints` navigation.
  - `Data/ApplicationDbContext.cs` — `DbSet<ClusterEndpoint>` + `OnModelCreating` FK/cascade config.
  - `Common/Enums/ClusterEndpointKind.cs` (new) — `Vip = 0`, `Domain = 1`.
  - `Data/Repositories/ClusterRepository.cs` — `GetByIdAsync` adds `.Include(c => c.Endpoints)`; `GetPagedAsync` unchanged (list view does not need endpoints).
  - `Components/Clusters/Services/ClusterService.cs` — `AddClusterAsync` persists endpoints from the create VM; new `UpdateClusterEndpointsAsync(int clusterId, List<ClusterEndpointEditItem>)` performs the full-replace strategy; validation throws `ArgumentException` when more than one `IsPrimary` row exists per `Kind`.
  - `Components/Clusters/ViewModels/ClusterEndpointViewModel.cs` (new) — detail view VM with `KindText`, `Value`, `Note`, `IsPrimary`.
  - `Components/Clusters/ViewModels/ClusterEndpointEditItem.cs` (new) — editor row VM (`Id?`, `Kind`, `Value`, `Note`, `IsPrimary`, `IsDeleted`).
  - `Components/Clusters/ViewModels/ClusterCreateViewModel.cs` — `+ List<ClusterEndpointEditItem> Endpoints` (default empty).
  - `Components/Clusters/ViewModels/ClusterDetailViewModel.cs` — `+ List<ClusterEndpointViewModel> Endpoints`.
  - `Components/Clusters/ViewModels/Mappings/ClusterMappingExtensions.cs` — `ToDetailViewModel` and `ToEditViewModel` project endpoints; helper `ApplyEndpoints(entity, items)` for full-replace.
  - `Components/Clusters/Shared/ClusterEndpointEditor.razor` (new) — embeddable row-based editor, reused by AddCluster + ClusterEndpointsDialog.
  - `Components/Clusters/Shared/ClusterEndpointsDialog.razor` (new) — wraps `ClusterEndpointEditor` in a `MudDialog`; loads existing endpoints into the editor on init; on OK calls `UpdateClusterEndpointsAsync`.
  - `Components/Clusters/Shared/AddClusterDialog.razor` — embed `ClusterEndpointEditor`, submit a `ClusterCreateViewModel.Endpoints` list.
  - `Components/Clusters/Pages/ClusterDetail.razor` — rewrite (un-comment + restructure into the archive layout). Reuses existing `ClusterService.GetClusterDetailAsync`, `RefreshClusterStatusAsync`, `DeleteClusterAsync`; calls `GetClusterForEditAsync` only for the Admin-only "show secret" toggle of KubeConfig/Token (unchanged from the commented original).
- **Database:** New `ClusterEndpoints` table. Existing `MultiClusterMgmtSys.db` MUST be deleted; `EnsureCreated()` rebuilds on next startup.
- **URL contract:** `/clusters/{id}` becomes a live route (previously fell through to `/not-found`).
- **Permissions:** Admin-only actions on the detail page: edit endpoints, show secret, refresh status, edit cluster (button remains but `EditClusterDialog` is still disabled — out of scope), delete cluster. Member-visible: the Endpoints card itself (read-only), Overview, Nodes preview.