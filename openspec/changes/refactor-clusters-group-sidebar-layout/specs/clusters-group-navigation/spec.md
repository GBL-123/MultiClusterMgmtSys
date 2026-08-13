# clusters-group-navigation

## Purpose

Define the contract for a persistent group sidebar on the `/clusters` page that drives cluster filtering, inline group management (create / rename / delete), and admin-only cross-page batch assignment of clusters to groups. The sidebar is the single entry point for group selection on the page; the previous "分组管理" dialog and group filter dropdown are removed.

## ADDED Requirements

### Requirement: Persistent group sidebar on `/clusters`

The `/clusters` page SHALL render a left sidebar that lists, in order: a "全部集群" pseudo-entry (no cluster count), a "未分组" pseudo-entry (with count of clusters whose `GroupId` is null), then one entry per group (with the group's cluster count). Clicking any entry filters the right-side cluster table to that group without leaving `/clusters`.

#### Scenario: Sidebar structure
- **WHEN** the `/clusters` page renders
- **THEN** the sidebar shows "全部集群", "未分组", followed by all existing groups, each with its `ClusterCount` displayed in parentheses

#### Scenario: Clicking a sidebar entry filters without navigation
- **WHEN** a user clicks any sidebar entry
- **THEN** the visible URL changes to `/clusters?group=<id>` (where `0` means ungrouped and absence means all), and the cluster table reloads against the corresponding query without leaving the `/clusters` route

#### Scenario: Sidebar highlights the active selection
- **WHEN** the URL is `/clusters` or `/clusters?group=0` or `/clusters?group=<id>`
- **THEN** the matching sidebar entry is visually marked as selected (e.g., MudNavLink active state) and no other entry is marked

### Requirement: URL-persisted group selection

Selected group SHALL be encoded in the URL query string (`?group=<id>`), so that refresh, copy-paste of the URL, and back/forward navigation all restore the same selection. The page MUST resolve the URL on initial load via `NavigationManager` and react to `LocationChanged` events thereafter.

#### Scenario: Initial load with a group in the URL
- **WHEN** the page is opened with `/clusters?group=<id>` and the id corresponds to an existing group
- **THEN** the sidebar highlights that group and the cluster table is filtered to that group on first render

#### Scenario: Invalid group id in URL falls back gracefully
- **WHEN** the page is opened with `/clusters?group=<id>` and no group with that id exists (and id is not `0`)
- **THEN** the page falls back to the "全部集群" selection and surfaces a snackbar warning `分组不存在`; it MUST NOT crash

#### Scenario: Refresh preserves selection
- **WHEN** the user refreshes the page while a sidebar selection is active
- **THEN** the same sidebar entry is highlighted and the same filter is applied after the refresh

#### Scenario: Back/forward navigation updates the sidebar
- **WHEN** the user clicks browser back or forward to a URL with a different `?group=` value
- **THEN** the sidebar highlight and cluster table filter update to match the new URL

### Requirement: Admin-only inline group management via the sidebar

The sidebar SHALL expose group create / rename / delete actions. Each action SHALL be performed through a small dialog containing only a `分组名称` field (no description field). All management actions MUST be wrapped in `<AuthorizeView Roles="Admin">` and invisible to Members.

#### Scenario: Create group action is in the sidebar header
- **WHEN** an Admin views the sidebar
- **THEN** a "+" icon button is visible at the sidebar header, and clicking it opens a create-group dialog with a single required `分组名称` field

#### Scenario: Rename action is a per-row hover button
- **WHEN** an Admin hovers an existing group row in the sidebar
- **THEN** a ✏️ icon button appears on that row, and clicking it opens a rename-group dialog pre-filled with the group's current name

#### Scenario: Delete action is a per-row hover button with confirmation
- **WHEN** an Admin hovers an existing group row and clicks the 🗑️ icon button
- **THEN** a confirmation messagebox appears asking the user to confirm deletion; on confirm, the group is deleted and any clusters previously in that group become ungrouped (their `GroupId` becomes null)

#### Scenario: Management actions are hidden from Members
- **WHEN** a Member (role `Member`) views the sidebar
- **THEN** the "+" header button is not rendered and the per-row ✏️ and 🗑️ buttons are not rendered

### Requirement: Sidebar state refresh after structural changes

After any of the following structural changes, the sidebar's group list and cluster counts SHALL be re-fetched from `GroupService.GetGroupsAsync()`: successful create, successful rename (count unchanged but name updates), successful delete.

#### Scenario: Counts update after batch move
- **WHEN** an Admin successfully moves clusters into a group via batch mode (see batch requirement)
- **THEN** the sidebar's per-group `ClusterCount` values are refreshed before the next render

#### Scenario: Selection resets when the selected group is deleted
- **WHEN** an Admin deletes the group that's currently selected in the sidebar
- **THEN** the page redirects to `/clusters` (no `?group=` param) and the cluster table reloads unfiltered; the deleted group's id MUST NOT remain in the URL

### Requirement: Admin-only cross-page batch assignment of clusters to groups

The right panel SHALL expose an Admin-only "批量操作" toggle button. When toggled on, the cluster table shows a multi-select checkbox column and selection state is maintained in a page-level `HashSet<int>` of cluster ids that survives paging, sorting, and filter changes. A floating batch action bar SHALL appear when at least one cluster is selected, providing a "移动到分组" menu (including a "未分组" entry) and a "清空选择" button.

#### Scenario: Batch mode toggle is admin-only
- **WHEN** a Member views the cluster table
- **THEN** the "批量操作" toggle button is not rendered and the checkbox column is never visible

#### Scenario: Batch mode initializes with no selection
- **WHEN** an Admin clicks "批量操作" to enter batch mode
- **THEN** the checkbox column appears in the table, the selection set is empty, and no batch action bar is visible yet

#### Scenario: Selection persists across pages
- **WHEN** an Admin selects clusters on page 1, navigates to page 2, and selects more clusters
- **THEN** all previously selected clusters remain in the page-level selection set; the batch action bar shows the cumulative count `N 个集群已选`

#### Scenario: Selection persists across filter changes
- **WHEN** an Admin has selected clusters, then applies a different group filter via the sidebar (so the selected clusters may no longer be visible)
- **THEN** the page-level selection set is preserved unchanged; the count in the batch action bar reflects the still-selected (but possibly invisible) clusters; "清空选择" remains available to reset

#### Scenario: Unchecking a row removes it from the selection set
- **WHEN** an Admin unchecks a previously-selected row (regardless of which page it's on)
- **THEN** that cluster's id is removed from the page-level selection set and the batch bar count decreases accordingly

#### Scenario: Move to group action
- **WHEN** an Admin selects at least one cluster and clicks "移动到分组" then picks any menu entry (including "未分组")
- **THEN** the `MoveClustersToGroupAsync` service method is invoked with the selected cluster ids and the target group id (`null` if "未分组" was chosen), the affected clusters' `GroupId` is updated in the database, the selection set is cleared, batch mode stays on (or off if Admin explicitly exits), and the sidebar group counts refresh
- **AND** a success snackbar confirms the count of clusters moved

#### Scenario: Empty selection disables the move action
- **WHEN** the selection set is empty (no clusters selected in batch mode)
- **THEN** the batch action bar is hidden entirely and the "移动到分组" action is unavailable

#### Scenario: "清空选择" resets selection
- **WHEN** an Admin clicks "清空选择"
- **THEN** the page-level selection set is cleared to empty and any checkbox visual state on the current page is reset to unchecked

### Requirement: Group entity has no description field

The `ClusterGroup` entity, its view models, its create/rename dialogs, and its mappings SHALL NOT carry a `Description` field. Group management surfaces only the group name.

#### Scenario: Database is regenerated because schema changed
- **WHEN** the application starts after this change and the existing `MultiClusterMgmtSys.db` contains a `ClusterGroups` table with a `Description` column
- **THEN** startup behavior is undefined until the user deletes `MultiClusterMgmtSys.db`; `Database.EnsureCreated()` will recreate the schema without the `Description` column on a fresh `.db` file
- **AND** the admin seed (`admin` / `Changeme_123`) runs as usual to restore an initial admin account