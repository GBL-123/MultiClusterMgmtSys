## Why

The current cluster management page surfaces "groups" only as a filter dropdown plus an admin-only "分组管理" dialog. Modifying groups (rename, description) has no path at all — `GroupService` only supports add/list/delete — and moving a cluster between groups requires the single-cluster edit flow (which today is non-functional because `EditClusterDialog.razor` is fully commenteded out). The grouping model deserves to be a first-class navigation concept on the cluster page rather than a secondary dialog.

## What Changes

- **Replace** the "分组管理" button + `ManageGroupsDialog.razor` flow with a persistent left **GroupSidebar** on `/clusters`. The sidebar lists all groups and shows the cluster count per group; clicking a group filters (does not route) to that group's clusters.
- **Add** a "未分组" (ungrouped) item next to "全部集群" at the top of the sidebar, making "ungrouped" a first-class queryable concept. The repository translates the sentinel `GroupId == 0` to `WHERE GroupId IS NULL`.
- **Add** per-row hover actions on each sidebar group: ✏️ (rename, admin only) and 🗑️ (delete, admin only), each opening a small dialog containing only a group-name field (no description field). The "+" button at the sidebar header creates new groups (admin only).
- **BREAKING (schema):** Remove the `ClusterGroup.Description` field entirely — entity, view model, create/update view models, mappings. Dropping the column requires regenerating `MultiClusterMgmtSys.db` (repo uses `Database.EnsureCreated()` with no EF migrations).
- **Add** an admin-only "批量操作" button on the right panel that toggles a batch mode. In batch mode `ClusterTable` shows `MultiSelect` checkboxes and supports **cross-page** selection (selection persists across pages/sorts via a page-level `HashSet<int>` of cluster ids). A floating action bar exposes "移动到分组 ▼" — including a "未分组" target — so admins can move clusters out of any group. Only "移动到分组" is a batch action; no batch delete.
- **Add** URL-persisted group selection: `/clusters?group=<id>` (with `0` meaning ungrouped, omitted meaning all). Selection survives refresh, navigation back/forward, and is reflected by the sidebar highlight. Use `NavigationManager` + `LocationChanged`, not route parameters.
- **Extend** `GroupService` with `RenameGroupAsync(id, newName)` and `MoveClustersToGroupAsync(clusterIds, targetGroupId)` where `targetGroupId` is `null` for ungrouped.
- **Remove** the "分组" dropdown from `ClusterFilterBar.razor` — its role is now filled by the sidebar.
- **Leave** `EditClusterDialog.razor` untouched (currently fully commented out); it is out of scope and will be reworked by a separate change.

## Capabilities

### New Capabilities
- `clusters-group-navigation`: Persistent sidebar on `/clusters` that drives cluster filtering by group, including an "all" and an "ungrouped" pseudo-entry, with URL-persisted selection and admin-only inline management actions (create / rename / delete).

### Modified Capabilities
- `cluster-query-layering`: The repository's `ClusterPageQuery.GroupId` semantics expand from "null = no filter, value = equality" to "null = no filter, `0` = WHERE `GroupId IS NULL`, `>0` = equality". The service translates the sidebar selection (including the `0` sentinel) into the same query specification.

## Impact

- **Code:**
  - `Components/Clusters/Pages/Clusters.razor` — restructure into two-column layout, wire sidebar + per-cluster hash-set selection, subscribe to `NavigationManager.LocationChanged`.
  - `Components/Clusters/Shared/GroupSidebar.razor` (new) — list + inline actions + URL-driven highlight.
  - `Components/Clusters/Shared/ManageGroupsDialog.razor` — delete.
  - `Components/Clusters/Shared/CreateGroupDialog.razor` — keep or fold into a single `EditGroupDialog.razor` (only the name field remains).
  - `Components/Clusters/Shared/ClusterFilterBar.razor` — remove the "分组" `MudSelect`.
  - `Components/Clusters/Shared/ClusterTable.razor` — add `MultiSelect=true` + `SelectedItems` binding; expose `MultiSelectVisible` toggle.
  - `Common/Queries/ClusterPageQuery.cs` — document the new `GroupId == 0` sentinel.
  - `Data/Repositories/ClusterRepository.cs` — branch `GroupId==0` into `Is NULL` predicate, add `SetGroupIdForClustersAsync(IEnumerable<int> ids, int? targetGroupId)`.
  - `Data/Repositories/GroupRepository.cs` — add `RenameAsync(id, newName)`.
  - `Data/Entities/ClusterGroup.cs` — drop `Description`.
  - `Components/Clusters/Services/GroupService.cs` — add `RenameGroupAsync`, `MoveClustersToGroupAsync`; drop `description` from create VM.
  - `Components/Clusters/Services/ClusterService.cs` — pass through `GroupId==0` sentinel (no behavior change beyond that).
  - `Components/Clusters/ViewModels/ClusterGroupViewModel.cs` + `GroupCreateViewModel.cs` + `Mapping/GroupMappingExtensions.cs` — drop `Description`.
- **Database:** Schema change (`Description` column removed) = **delete `MultiClusterMgmtSys.db` and let `EnsureCreated()` rebuild** on next startup. Existing local data will be lost; the admin seed (`admin / Changeme_123`) will reseed automatically.
- **URL contract:** `/clusters?group=0` becomes a valid, bookmarkable URL meaning "ungrouped". `/clusters?group=<id>` with a non-existent id falls back to "all".
- **Permissions:** Admin-only: create/rename/delete group, batch mode toggle, move-to-group action. Member-visible: sidebar list, group filtering; checkbox column is hidden entirely for Member (batch mode is admin-only).