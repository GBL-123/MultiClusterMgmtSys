# Tasks: refactor-clusters-group-sidebar-layout

## 1. Data layer

- [x] 1.1 Remove `Description` from `MultiClusterMgmtSys/Data/Entities/ClusterGroup.cs` (entity property)
- [x] 1.2 Remove `Description` propagation in `Components/Clusters/ViewModels/Mappings/GroupMappingExtensions.cs`
- [x] 1.3 Remove `Description` from `Components/Clusters/ViewModels/ClusterGroupViewModel.cs`
- [x] 1.4 Remove `Description` from `Components/Clusters/ViewModels/GroupCreateViewModel.cs`
- [x] 1.5 Modify `Data/Repositories/ClusterRepository.cs` `GetPagedAsync` to branch `q.GroupId == 0` into `WHERE GroupId == null` predicate (keep `null` = no filter, `>0` = equality)
- [x] 1.6 Add `ClusterRepository.SetGroupIdForClustersAsync(IEnumerable<int> clusterIds, int? targetGroupId)` — single `UPDATE Clusters SET GroupId=@tg WHERE Id IN (...)`
- [x] 1.7 Add `GroupRepository.RenameAsync(int id, string newName)` — fetch entity, mutate Name, SaveChangesAsync
- [x] 1.8 Delete the existing `MultiClusterMgmtSys.db` (and any `clusters.db` / `*-wal` / `*-shm` files); commit nothing — runtime artifacts only. Confirm by inspection that the next `EnsureCreated()` rebuilds the schema without the `Description` column.

## 2. Services

- [x] 2.1 In `Components/Clusters/Services/GroupService.cs`, change ctor to accept `ClusterRepository` (preserve existing `GroupRepository` + `ILogger`)
- [x] 2.2 Add `GroupService.RenameGroupAsync(int id, string newName)` — resolve entity via `repo.GetByIdAsync`, set Name, call `repo.RenameAsync`
- [x] 2.3 Add `GroupService.MoveClustersToGroupAsync(IEnumerable<int> clusterIds, int? targetGroupId)` — throw `ArgumentException` if `targetGroupId == 0`; delegate to `clusterRepo.SetGroupIdForClustersAsync`
- [x] 2.4 Update `GroupService.AddGroupAsync` to no longer set `Description` from the VM (field removed in 1.4)
- [x] 2.5 Verify `ClusterService.GetPagedAsync` already passes the raw `GroupId` through `ClusterPageQuery` (it does — no change needed); add a code comment near `ToPageQuery` noting `GroupId==0` is the ungrouped sentinel, handled at repo layer

## 3. Query translation contract (delta spec)

- [x] 3.1 In `Common/Queries/ClusterPageQuery.cs`, add an XML doc comment on the `GroupId` field stating: `null` = no filter; `0` = ungrouped sentinel (repo translates to `IS NULL`); `>0` = equality
- [x] 3.2 In `Components/Clusters/Requests/ClusterQueryRequest.cs` (or wherever `ClusterQueryRequest.GroupId` lives), add the same comment if the field is exposed there

## 4. Group management dialogs

- [x] 4.1 Create `Components/Clusters/Shared/EditGroupDialog.razor` — single required `分组名称` `MudTextField`. Ctor params via `[Parameter] int? GroupId` + `[Parameter] string? InitialName`. On submit: if `GroupId is null`, call `GroupService.AddGroupAsync(new GroupCreateViewModel { Name = name })`; else call `GroupService.RenameGroupAsync(GroupId.Value, name)`. Close with `DialogResult.Ok(true)`.
- [x] 4.2 Delete `Components/Clusters/Shared/CreateGroupDialog.razor`
- [x] 4.3 Delete `Components/Clusters/Shared/ManageGroupsDialog.razor`
- [x] 4.4 Verify no other code references the deleted dialogs (grep `CreateGroupDialog` / `ManageGroupsDialog`) — only `Clusters.razor` still references them; section 7 rewrites that file.

## 5. ClusterFilterBar — drop the group dropdown

- [x] 5.1 In `Components/Clusters/Shared/ClusterFilterBar.razor`, remove the `分组` `MudSelect` block (lines ~14-24) plus the `Query.GroupId = null` reset in `ResetFilter`
- [x] 5.2 Remove the `Groups` parameter from `ClusterFilterBar` (no longer used there)
- [x] 5.3 Remove the `Groups` parameter passing from the parent `Clusters.razor` `<ClusterFilterBar ...>` tag — covered by section 7 rewrite.

## 6. GroupSidebar component

- [x] 6.1 Create `Components/Clusters/Shared/GroupSidebar.razor`. Parameters: `IReadOnlyList<ClusterGroupViewModel> Groups`, `int? SelectedGroupId`, `EventCallback<int?> OnGroupSelected`, `EventCallback OnCreateGroup`, `EventCallback<ClusterGroupViewModel> OnRenameGroup`, `EventCallback<ClusterGroupViewModel> OnDeleteGroup`.
- [x] 6.2 Header row: title "分组" + Admin-only `<AuthorizeView><Authorized>` wrapping a `MudIconButton Icon="@Icons.Material.Filled.Add"` whose `OnClick` invokes `OnCreateGroup`
- [x] 6.3 Render a list with three entry kinds: "全部集群" (key `null`), "未分组" (key `0`), then each group (key `group.Id`). Each entry displays name + (count where applicable). The "全部集群" entry has no count badge.
- [x] 6.4 Highlight the entry whose key equals `SelectedGroupId` (MudNavLink active style or equivalent)
- [x] 6.5 On each group row, Admin-only hover buttons: ✏️ (invokes `OnRenameGroup(context)`), 🗑️ (invokes `OnDeleteGroup(context)`). Use MudIconButton small + Extinction styling.
- [x] 6.6 Click anywhere else on the row invokes `OnGroupSelected(key)` where key is the entry's pseudo-id (null / 0 / group.Id)

## 7. Clusters.razor page restructure

- [x] 7.1 Restructure `Clusters.razor` `<MudStack Class="d-flex flex-auto">` to a two-column layout: outer `MudStack Row="true"`, left = `<GroupSidebar ...>`, right = existing `MudStack` containing ClusterFilterBar + ClusterTable
- [x] 7.2 Remove the "分组管理" and "新建分组" buttons from the title row
- [x] 7.3 Add an Admin-only "批量操作" `MudButton` toggle to the title row next to "添加集群"; `@bind-Checked` or local `batchMode` field
- [x] 7.4 Inject `NavigationManager`. Add `LocationChanged` handler that parses `?group=` from `NavigationManager.Uri`, sets `selectedGroupId` and `query.GroupId`, calls `tableComponent.ReloadDataAsync()`. Subscribe in `OnInitialized` (or `OnAfterRenderAsync` first render), unsubscribe in `Dispose` (implement `IDisposable`)
- [x] 7.5 On `OnInitializedAsync`, parse the initial URL to populate `selectedGroupId` / `query.GroupId` *before* the first table render
- [x] 7.6 Replace `OpenManageGroupsDialog` and `OpenCreateGroupDialog` with `OpenCreateGroupDialog` and `OpenRenameGroupDialog(ClusterGroupViewModel)` that both invoke `DialogService.ShowAsync<EditGroupDialog>` with appropriate parameters; on `Ok` re-fetch `groups`
- [x] 7.7 Implement `SelectGroup(int? id)`: instead of mutating state directly, call `NavigationManager.NavigateTo("/clusters" + (id.HasValue ? "?group=" + id.Value : ""))`. Let `LocationChanged` be the only state mutator.
- [x] 7.8 Implement `DeleteGroup(ClusterGroupViewModel group)`: show confirm messagebox → call `GroupService.DeleteGroupAsync(group.Id)` → if `selectedGroupId == group.Id`, call `SelectGroup(null)` to reset URL → re-fetch groups

## 8. Batch mode & cross-page selection

- [x] 8.1 Add page-level fields to `Clusters.razor`: `bool batchMode`, `HashSet<int> selectedClusterIds = new()`, `HashSet<int> lastPageSelected = new()`
- [x] 8.2 Add Admin-only "批量操作" toggle button visible only via `<AuthorizeView Roles="Admin">`. Toggling doesn't clear selection but turns on/off the checkbox column.
- [x] 8.3 Pass `MultiSelectVisible="batchMode"` to `ClusterTable`
- [x] 8.4 In `ClusterTable.razor`: bind `MultiSelect="MultiSelectVisible"` on `MudTable`; add `[Parameter] HashSet<int> SelectedIds`; add `EventCallback<HashSet<int>> OnSelectedIdsChanged`. Implement `SelectedItemsChanged` that diffs against `lastPageSelected`: compute added = current - lastPage, removed = last - current; surface to parent via `OnSelectedIdsChanged`. Update `lastPageSelected` to current. Re-emit `SelectedItems` containing the union of visible-row-checked + invisible-externally-selected.
- [x] 8.5 After each successful `LoadData` (TableData return), programmatically re-check rows whose Id is in `SelectedIds` (e.g., by setting `SelectedItems` to the subset of returned items whose Id matches). MudBlazor's server-data MultiSelect requires this per-page re-check.
- [x] 8.6 Implement `BatchActionBar` inline in `Clusters.razor` — visible when `batchMode && selectedClusterIds.Count > 0`. Contains: `MudText` "{Count} 个集群已选", `MudMenu` "移动到分组" listing each group's Name + a "未分组" item; click handler calls `MoveClustersAsync(targetGroupId)`
- [x] 8.7 Implement `MoveClustersAsync(int? targetGroupId)` in `Clusters.razor`: if `targetGroupId == 0` convert to `null`; call `await GroupService.MoveClustersToGroupAsync(selectedClusterIds, targetGroupId)`; show success snackbar with count; clear `selectedClusterIds`; re-fetch groups; reload table
- [x] 8.8 Implement "清空选择" button: clears `selectedClusterIds`, calls `tableComponent.ClearSelection()` to uncheck visible rows
- [x] 8.9 Wrap the entire batch bar (and its actions) inside `<AuthorizeView Roles="Admin"><Authorized>`. The "批量操作" toggle button is also Admin-only.

## 9. Verification

- [x] 9.1 `dotnet build MultiClusterMgmtSys.slnx` succeeds with no warnings about the deleted Description field
- [ ] 9.2 `dotnet run --project MultiClusterMgmtSys` starts; first request to `/clusters` as Admin shows the sidebar with "全部集群" highlighted, "未分组" with count 0 (fresh db)
- [ ] 9.3 Manual: Admin clicks "+", creates a group named "测试", sees it appear in sidebar with (0) count; navigates to "添加集群", creates a cluster assigned to "测试", sidebar count updates after refresh to (1)
- [ ] 9.4 Manual: Admin clicks "测试" in sidebar → URL becomes `/clusters?group=2`, table shows only that group's clusters; refresh keeps selection
- [ ] 9.5 Manual: Admin clicks "未分组" → URL becomes `/clusters?group=0`, table shows only clusters with `GroupId IS NULL`
- [ ] 9.6 Manual: Admin clicks "全部集群" → URL becomes `/clusters` (no query), table shows all clusters
- [ ] 9.7 Manual: Admin clicks ✏️ on "测试", renames to "测试-2", sidebar updates; navigates back, URL still works because id didn't change
- [ ] 9.8 Manual: Admin clicks 🗑️ on a group that's currently selected; URL resets to `/clusters` and the deleted group's id is no longer in the URL
- [ ] 9.9 Manual: Admin enters batch mode, selects 2 clusters on page 1, navigates to page 2, selects 1 more, batch bar shows "3 个集群已选"; clicks "移动到分组" → "未分组", sees all 3 effective; selection clears
- [ ] 9.10 Manual: Admin enters batch mode, selects clusters, clicks sidebar filter, selection persists (count unchanged even when selected clusters are no longer visible)
- [ ] 9.11 Manual: Non-admin (Member) user logs in, navigates to `/clusters`; sidebar visible with no "+" or hover actions; "批量操作" button absent; cluster table has no checkbox column
- [ ] 9.12 Manual: `/clusters?group=999` (non-existent id) → snackbar `分组不存在`, sidebar highlights "全部集群"; `/clusters?group=0` works as ungrouped filter
- [ ] 9.13 Confirm database was regenerated: stop app, check `MultiClusterMgmtSys.db` was recreated without a `Description` column in `ClusterGroups` table; admin seed user `admin` / `Changeme_123` works
- [x] 9.14 `openspec validate --changes refactor-clusters-group-sidebar-layout` passes (no orphaned requirements, all spec capabilities reference real files)