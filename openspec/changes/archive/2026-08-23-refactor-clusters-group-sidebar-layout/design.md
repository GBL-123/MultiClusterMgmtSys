# Design: refactor-clusters-group-sidebar-layout

## Overview

Restore "groups" as a first-class navigation concept on `/clusters` by introducing a persistent left sidebar, plus a batch mode that lets admins move clusters between groups across pages. The page remains a single route (`/clusters`); the sidebar drives filter state through `Clusters.razor`, not through Blazor routing.

```
MainLayout (unchanged)
┌──────────────────────────────────────────────────────────────────┐
│ AppBar                                                           │
├─────┬────────────────────────────────────────────────────────────┤
│ Mini│ /clusters?group=<id>                                       │
│ Draw│ ┌────────────────┬───────────────────────────────────────┐
│  (..│ │ GroupSidebar    │  Right panel                           │
│     │ │ ───────────── │ ┌FilterBar (no group dropdown)────────┐│
│     │ │ 分组      [+] │ │ 名称 状态 版本 日期 [查询][重置]    ││
│     │ │ 全部集群    ● │ │                   [批量操作](Admin) ││
│     │ │ 未分组   (3)  │ ├────────────────────────────────────────┤│
│     │ │ 生产    (12) │ │ ClusterTable (MultiSelect in batch)  ││
│     │ │ 测试     (5)  │ │  ☐ 名称 ... 操作                    ││
│     │ │  ...hover... │ │  ...                                  ││
│     │ │  生产 ✏️ 🗑️  │ │  [Pager]                             ││
│     │ └────────────────┘│ └────────────────────────────────────┘│
│     │                   │ ┌Batch bar (when >0 selected)────────┐│
│     │                   │ │ N 个集群已选 [移动到分组 ▼] [清空] ││
│     │                   │ └────────────────────────────────────┘│
└─────┴────────────────────────────────────────────────────────────┘
```

## Component layout

### `Clusters.razor` (restructure)

Becomes a `MudGrid` (or `MudStack Row="true"`) with two children:

```
<MudStack Class="d-flex flex-auto">
  <MudStack Row="true" Style="gap:0; align-items:flex-start;">
    <MudPaper Style="width:240px;" Class="pa-0">
      <GroupSidebar Groups=groups
                    SelectedGroupId=selectedGroupId
                    OnGroupSelected=SelectGroup
                    OnCreateGroup=OpenCreateGroupDialog
                    OnRenameGroup=OpenRenameGroupDialog
                    OnDeleteGroup=DeleteGroup />
    </MudPaper>
    <MudStack Class="flex-auto">
      <MudPaper Class="pa-4">
        ... title row (no "分组管理"/"新建分组" buttons — sidebar owns them) ...
        <ClusterFilterBar ... /> (group dropdown removed)
      </MudPaper>
      <ClusterTable ... MultiSelectVisible=batchMode
                    SelectedIds=selectedClusterIds
                    OnSelectedIdsChanged=SyncSelection />
    </MudStack>
  </MudStack>

  @if (batchMode && selectedClusterIds.Count > 0)
  {
    <BatchActionBar ... />  // or inline MudStack; small component
  }
</MudStack>
```

Title row retains "集群管理", "添加集群" (Admin); removes "分组管理"/"新建分组" buttons (now sidebar's "+", ✏️, 🗑️). Adds "批量操作" toggle (Admin only) next to them.

### `GroupSidebar.razor` (new)

A self-contained component with parameters:

- `IReadOnlyList<ClusterGroupViewModel> Groups`
- `int? SelectedGroupId` (null = all, 0 = ungrouped, >0 = group id)
- `EventCallback<int?> OnGroupSelected` (sidebar calls this on click; parent decides URL push)
- `EventCallback OnCreateGroup` (opens dialog; parent re-fetches groups on success)
- `EventCallback<ClusterGroupViewModel> OnRenameGroup`
- `EventCallback<ClusterGroupViewModel> OnDeleteGroup`

Renders a `MudNavMenu`-like list, but as a custom `MudList` (the "+" button lives in the header via `MudButton Icon`). Per-row hover-renders ✏️ and 🗑️ HTML wrapped in `<AuthorizeView Roles="Admin">`. The ✏️ click is admin-only because the icons themselves are inside the AuthorizeView; for non-admin the row simply renders without them.

Highlight is controlled by `SelectedGroupId == candidate` (null==null for "全部集群", 0==0 for "未分组", id==id otherwise).

### `EditGroupDialog.razor` (new, replaces `CreateGroupDialog.razor`)

Single field "分组名称" (`MudTextField`, required). Constructor takes optional `GroupId?` + `InitialName?`; when `GroupId` is null → create flow, when set → rename flow. Saves via `GroupService.AddGroupAsync` or `GroupService.RenameGroupAsync`. Replaces both `CreateGroupDialog.razor` and `ManageGroupsDialog.razor`; both are deleted from the repo.

### `ClusterTable.razor` (extend)

Add `MultiSelect=true` to `MudTable` plus:

- `[Parameter] bool MultiSelectVisible` — toggles checkbox column visibility. Implementation: bind `MultiSelect="MultiSelectVisible"` on the `MudTable`. (MudBlazor respects this at render time.)
- `[Parameter] HashSet<int> SelectedIds` — used to persist cross-page selection. On each page load (`LoadData` callback after `Items` are returned), the parent component is responsible for re-marking rows; the table exposes `SetSelectedItems(IEnumerable<ClusterViewModel>)` to programmatically set the row's selected state.
- `EventCallback<HashSet<int>> OnSelectedIdsChanged` — fires whenever `SelectedItemsChanged` triggers; parent syncs the `HashSet`.

Cross-page flow:
```
page 1 user selects cluster IDs {3, 7, 9}
   ↳ SelectedItemsChanged → parent.selectedClusterIds = {3,7,9}
page 2 loaded  
   ↳ parent sends SelectedIds to table  
   ↳ table's after-load hook re-checks visible rows where row.Id ∈ SelectedIds
user adds 12, 15 on page 2
   ↳ SelectedItemsChanged fires with {12,15} (current page)  
   ↳ parent merges: selectedClusterIds.UnionWith(newIds)  // = {3,7,9,12,15}
user navigates back to page 1
   ↳ after-load re-checks rows 3,7,9
```

This means parent must track selection as a **union** of pages. Implementation pitfall: `SelectedItemsChanged` gives only the *current page's* selected items. Whether the merge is union or replace depends on direction; design uses **union on add, subtract on uncheck**, by diffing last-reported page selection vs newly-reported page selection in `Clusters.razor`:

```
lastPageSelected: HashSet<int>          // what the last event reported
OnSelectedItemsChanged(currentPageSelected):
  added   = currentPageSelected - lastPageSelected
  removed = lastPageSelected - currentPageSelected
  globalSelection.ExceptWith(removed)
  globalSelection.UnionWith(added)
  lastPageSelected = currentPageSelected
```

### Right-panel `BatchActionBar` (inline — not a separate file)

`MudStack Row="true"` showing:
- `MudText`: "@selectedClusterIds.Count 个集群已选"
- `MudMenu` "移动到分组" containing each group's name + "未分组" + "---" + (optional "+ 新建分组…" that opens `EditGroupDialog`)
- `MudButton "清空选择"` → clears `selectedClusterIds`, unchecks the visible rows via `table.ClearSelection()`

Hidden entirely when `!batchMode` or `selectedClusterIds.Count == 0`.

## Query layer: the `GroupId` sentinel

`ClusterQueryRequest.GroupId` is `int?`. Semantics:

| `GroupId` value | Meaning                              | SQL predicate             |
|-----------------|--------------------------------------|---------------------------|
| `null`          | no filter (all clusters)             | (none)                    |
| `0`             | only clusters with no group assigned | `WHERE GroupId IS NULL`   |
| `>0`            | only clusters in that group          | `WHERE GroupId == value`  |

Repository `GetPagedAsync` becomes:

```csharp
if (q.GroupId.HasValue)
{
    if (q.GroupId == 0)
        query = query.Where(c => c.GroupId == null);
    else
        query = query.Where(c => c.GroupId == q.GroupId);
}
```

This is the only behavioral change to `cluster-query-layering` (delta spec captures it).

Sidebar → URL → Query flow:
```
user clicks "生产" (id=2)
   ↳ sidebar fires OnGroupSelected(2)
   ↳ parent.SelectGroup(2) → NavigationManager.NavigateTo("/clusters?group=2")
   ↳ LocationChanged handler reads query string → sets selectedGroupId + query.GroupId
   ↳ query.GroupId = 2 → ClusterTable.ReloadServerData()
```

The LocationChanged handler is the single source of truth for `selectedGroupId`. Direct state mutation is forbidden — only the handler writes it.

## Data layer changes

### Entity

`ClusterGroup.Description` removed. Because `EnsureCreated()` is the schema mechanism (no EF migrations), users must **delete `MultiClusterMgmtSys.db`** before next startup; the admin seed (`admin / Changeme_123`) automatically recreates on first connect. Existing clusters/groups will be lost locally — acceptable trade-off, user confirmed.

### `GroupService` new methods

```csharp
public async Task RenameGroupAsync(int id, string newName)
{
    var entity = await repo.GetByIdAsync(id) ?? throw ...;
    entity.Name = newName;
    await repo.RenameAsync(id, newName);  // or repo.UpdateAsync(entity)
}

public async Task MoveClustersToGroupAsync(IEnumerable<int> clusterIds, int? targetGroupId)
{
    // targetGroupId null = ungrouped. Reject targetGroupId == 0 (sentinel not valid at the data layer).
    if (targetGroupId == 0)
        throw new ArgumentException("target group id must be a real id or null for ungrouped");
    await clusterRepo.SetGroupIdForClustersAsync(clusterIds, targetGroupId);
}
```

Note `targetGroupId == 0` is a *UI* sentinel for "ungrouped"; the service translates it to `null` before reaching the repository. The sidebar's "未分组" item passes `0` up; the parent converts to `null` before calling `MoveClustersToGroupAsync`.

### `ClusterRepository.SetGroupIdForClustersAsync`

New method (one UPDATE with `WHERE Id IN (...)`). Adds a `ClusterRepository` dependency to `GroupService` — `GroupService` ctor becomes `GroupService(GroupRepository repo, ClusterRepository clusterRepo, ILogger<GroupService> logger)`.

## Batch permission flow

Checkbox column, "批量操作" toggle button, and "移动到分组" action are all wrapped in `<AuthorizeView Roles="Admin"><Authorized>`. Members never see the checkbox column (even when `MultiSelectVisible=true` would normally show it): easier to gate the *batch mode toggle* itself behind `AuthorizeView`; if `batchMode == false`, `MultiSelectVisible` stays false, and the column renders as empty cells. (Alternative — `AuthorizeView` wrapping only the toggle, but checkbox column shows for everyone.)

For members, the table looks identical to today's table (no checkbox, no batch bar).

## Risks and decisions

- **`EditClusterDialog.razor` is fully commented out today** and remains untouched in this change. Out of scope; a future change will re-do it. The new batch-move capability *does* restore the "move cluster between groups" power user goal — partially — without claiming to restore single-cluster edit.
- **MudTable cross-page selection is non-trivial.** MudBlazor's `MultiSelect` is intended for client-side table data. For server-data mode the pattern of "external HashSet + after-load re-check" is the standard workaround; documented `lastPageSelected` diff in the design to avoid the common "switch page resets selection" bug.
- **URL parse failure mode:** `?group=nonexistent_id` falls back to "全部集群" with a snackbar warning `分组不存在`. `?group=0` is valid (= ungrouped).
- **GroupDeleted-while-selected edge case:** if `selectedGroupId` points to a group the admin has just deleted, `LocationChanged` won't fire (no URL change) but state is dirty. Solution: after `DeleteGroupAsync`, parent forcibly calls `SelectGroup(null)` (resets to "all"). This is the only place where state is mutated outside `LocationChanged` — documented explicitly.
- **Sidebar count sync after batch move:** after a successful `MoveClustersToGroupAsync`, the sidebar's `ClusterCount` values are stale. Parent re-fetches `groups = await GroupService.GetGroupsAsync()` to update counts. **Gotcha found during implementation:** the re-fetch alone is NOT enough — the scoped `ApplicationDbContext` already tracks the `ClusterInfo` entities (loaded by `GetAllAsync`'s `Include(g => g.Clusters)`), and `SetGroupIdForClustersAsync` uses `ExecuteUpdateAsync` which bypasses the change tracker. A re-query then returns the stale tracked `GroupId`, so `g.Clusters.Count` stays old. Fix: `GroupRepository.GetAllAsync()` uses `AsNoTracking()` (the sidebar count is a pure read-model), so every refresh reads fresh `GroupId` values from the DB.
- **Cross-page selection preserves across filter changes** (e.g., admin selects 5 clusters, then clicks "未分组" in sidebar to see what's there). Decision: **selection persists** but new visible rows honor it. Could be confusing if a selected cluster no longer appears in filtered view — but selection state lives separately from page contents, which is what users expect from email/file UIs (keep selected, just invisible). No automatic clearing on filter change. "清空选择" button is the explicit reset.
- **Short group dialog shows a content scrollbar (MudBlazor outlined-label overflow).** The create/rename group dialog is only ~173px tall (title 57 + content 68 + actions 48). MudBlazor's outlined `MudTextField` renders its floating label as `fieldset > legend` (relative legend inside an absolute fieldset that exactly fills the 40px input); the legend's 0-height box produces a ~25px phantom scrollable-overflow region. In tall dialogs (添加集群 content ≈ 635px) the region is absorbed by the larger box; in the short group dialog it makes `mud-dialog-content`'s `scrollHeight` (93px) exceed its `clientHeight` (68px), and since `mud-dialog-content` defaults to `overflow:auto`, a right-edge scrollbar renders. Verified empirically in headless Edge: hiding the legend drops `scrollHeight` from 93 to 68 (= `clientHeight`), proving the legend is the sole cause.
  - **Padding-top does NOT fix it** — sweeping 8–80px top padding left `scrollHeight - clientHeight` constant at 25px, so the phantom region is not above the box. (Initial "方案B: `ContentClass="pt-4"`" was tried and rejected for this reason.)
  - **Decision: `ContentClass="group-dialog-content"` + app.css `overflow: visible`.** The rule MUST use the `overflow` shorthand to set both axes — leaving `overflow-x: auto` forces the CSS spec to compute `overflow-y: visible` back to `auto`. The phantom region has no visible pixels (identical screenshots under `visible` vs `hidden`), so `visible` is safe: the floating label stays fully visible and nothing overlaps the actions.
  - Scoped to `EditGroupDialog`; the same mechanism exists in every outlined labeled field, so other short dialogs should reuse the pattern if they ever show the scrollbar.

## Out of scope

- Drag-and-drop group assignment. Left as a future enhancement.
- Bulk cluster delete. Explicitly excluded by user.
- Member users seeing checkboxes. Batch mode is admin-only.
- Responsive/mobile collapse. Desktop only.
- Rediscovering "EditClusterDialog" — left to a separate change.