# Tasks: redesign-cluster-detail-with-endpoints

## 1. Schema — endpoint kind enum + child entity

- [x] 1.1 Create `MultiClusterMgmtSys/Common/Enums/ClusterEndpointKind.cs` with `enum ClusterEndpointKind { Vip = 0, Domain = 1 }` (namespace `MultiClusterMgmtSys.Common.Enums`)
- [x] 1.2 Create `MultiClusterMgmtSys/Data/Entities/ClusterEndpoint.cs` (namespace `MultiClusterMgmtSys.Data.Entities`) with: `int Id`, `int ClusterId`, `ClusterEndpointKind Kind`, `string Value` (required), `string? Note`, `int SortOrder`, `ClusterInfo? Cluster` navigation
- [x] 1.3 In `Data/Entities/ClusterInfo.cs` add `public ICollection<ClusterEndpoint> Endpoints { get; set; } = new List<ClusterEndpoint>();` (uninitialized collection is fine but matches repo style to init to `new List<...>()`)
- [x] 1.4 In `Data/ApplicationDbContext.cs` add `public DbSet<ClusterEndpoint> ClusterEndpoints => Set<ClusterEndpoint>();`
- [x] 1.5 In `Data/ApplicationDbContext.cs` `OnModelCreating` add a config block for `ClusterEndpoint`: `Property(e => e.Value).IsRequired().HasMaxLength(256)`, `Property(e => e.Note).HasMaxLength(64)`, FK to `ClusterInfo.Id` via `e.Cluster` navigation with `OnDelete(DeleteBehavior.Cascade)`
- [x] 1.6 Delete `MultiClusterMgmtSys/MultiClusterMgmtSys.db` and any `*-wal` / `*-shm` files. Delete the stray `clusters.db` if present per `AGENTS.md`. Document this in the eventual commit message. (No code change — runtime step.)

## 2. Repository — eager-load endpoints on detail fetch only

- [x] 2.1 In `Data/Repositories/ClusterRepository.cs` `GetByIdAsync`, append `.Include(c => c.Endpoints)` after the existing `.Include(c => c.Group)` so the detail-page load returns endpoints
- [x] 2.2 Leave `GetPagedAsync` unchanged — list view does not render endpoints and must not pay the join cost. (No code change; verify by inspection.)
- [x] 2.3 Remove any `AddAsync`-side manual endpoint handling in the repo — endpoint persistence flows through the `ClusterInfo` navigation collection via `UpdateAsync`, so the repository needs no new method. (Verify by inspection.)

## 3. ViewModels — endpoints on create / detail / edit-item VMs

- [x] 3.1 Create `Components/Clusters/ViewModels/ClusterEndpointViewModel.cs` (namespace `MultiClusterMgmtSys.Components.Clusters.ViewModels`) with: `int Id`, `ClusterEndpointKind Kind`, `string KindText` (computed in mapping: "VIP" / "域名"), `string Value`, `string? Note`, `int SortOrder`
- [x] 3.2 Create `Components/Clusters/ViewModels/ClusterEndpointEditItem.cs` (same namespace) with: `int Id` (0 = new), `ClusterEndpointKind Kind`, `string Value`, `string? Note`, `int SortOrder`, `bool IsDeleted` (default false — soft marker used by the editor and ignored on submit; the service takes the surviving rows as the authoritative list)
- [x] 3.3 In `Components/Clusters/ViewModels/ClusterCreateViewModel.cs` add `public List<ClusterEndpointEditItem> Endpoints { get; set; } = new();`
- [x] 3.4 In `Components/Clusters/ViewModels/ClusterDetailViewModel.cs` add `public List<ClusterEndpointViewModel> Endpoints { get; set; } = new();`

## 4. Mappings — project endpoints in both directions

- [x] 4.1 In `Components/Clusters/ViewModels/Mappings/ClusterMappingExtensions.cs` extend `ToDetailViewModel(ClusterInfo e)` to map `Endpoints = e.Endpoints.Select(ep => new ClusterEndpointViewModel { Id = ep.Id, Kind = ep.Kind, KindText = ep.Kind == ClusterEndpointKind.Vip ? "VIP" : "域名", Value = ep.Value, Note = ep.Note, SortOrder = ep.SortOrder }).OrderBy(ep => ep.Kind).ThenBy(ep => ep.SortOrder).ToList()`
- [x] 4.2 In the same file add a static helper `public static void ApplyEndpoints(this ClusterInfo entity, IEnumerable<ClusterEndpointEditItem> items)` that: validates every item's `Value` is non-empty-after-trim and ≤ 256 chars, `Note` ≤ 64 chars (throw `ArgumentException` on violation), then `entity.Endpoints.Clear()` followed by `entity.Endpoints.Add(new ClusterEndpoint { Kind = item.Kind, Value = item.Value.Trim(), Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(), SortOrder = item.SortOrder })` for each surviving item. SaveChanges is NOT called here; the caller's `UpdateAsync` commits.

## 5. ClusterService — endpoint create + update + validation

- [x] 5.1 In `Components/Clusters/Services/ClusterService.cs` `AddClusterAsync(ClusterCreateViewModel vm)`, after `await repo.AddAsync(entity)` and before `await ProbeAsync(entity)`, call `entity.ApplyEndpoints(vm.Endpoints)`; `repo.UpdateAsync(entity)` will persist them in the same SaveChanges as the post-probe status update
- [x] 5.2 Add `public async Task UpdateClusterEndpointsAsync(int clusterId, List<ClusterEndpointEditItem> items)` in `ClusterService.cs`:
  - `var entity = await repo.GetByIdAsync(clusterId)` — throw `InvalidOperationException($"Cluster {clusterId} not found")` if null (`GetByIdAsync` already Includes Endpoints per task 2.1)
  - `entity.ApplyEndpoints(items)` — runs validation + full-replace mutation in-memory
  - `await repo.UpdateAsync(entity)` — commits DELETE + INSERT
  - Log `LogInformation("UpdateClusterEndpoints id={ClusterId} count={Count}", clusterId, items.Count)` on success
- [x] 5.3 Add a unit-checked comment near `ApplyEndpoints` documenting the invariant: "service is the single authority for what survives"

## 6. Endpoint editor shared component

- [x] 6.1 Create `Components/Clusters/Shared/ClusterEndpointEditor.razor`. Parameters: `[Parameter] public List<ClusterEndpointEditItem> Items { get; set; } = new();`. Spawn a `MudTable`-like row layout (or just a `MudStack` of rows) rather than relying on `MudTable` selection semantics.
- [x] 6.2 Per row render: `MudSelect<ClusterEndpointKind>` (items: Vip → "VIP", Domain → "域名") bound to `row.Kind`; `MudTextField string` `Label="地址"` `Required` `MaxLength="256"` bound to `row.Value`; `MudTextField string` `Label="备注"` `MaxLength="64"` bound to `row.Note`; `MudTextField<int>` `Label="排序"` bound to `row.SortOrder`; a `MudIconButton` 🗑 that removes the row from `Items`.
- [x] 6.3 At the bottom of the editor render a `MudButton Variant="Variant.Outlined" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" OnClick="@(() => Items.Add(new ClusterEndpointEditItem { Kind = ClusterEndpointKind.Vip, SortOrder = Items.Count }))"` labeled "+ 添加端点"
- [x] 6.4 In `_Imports.razor` confirm `MultiClusterMgmtSys.Common.Enums` and `MultiClusterMgmtSys.Components.Clusters.ViewModels` are already imported (they are, via the Clusters namespace) — if not, add the necessary `@using` only in this file's block. Do NOT modify `_Imports.razor` if the imports are already there in some form.

## 7. ClusterEndpointsDialog — manage existing endpoints

- [x] 7.1 Create `Components/Clusters/Shared/ClusterEndpointsDialog.razor`. Parameters: `[Parameter] public int ClusterId { get; set; }`, `[CascadingParameter] public IMudDialogInstance Dialog { get; set; } = default!`. Injects `ClusterService`, `ISnackbar`.
- [x] 7.2 In `OnInitializedAsync`: call `ClusterService.GetClusterDetailAsync(ClusterId)`; if null, snackbar + `Dialog.Cancel()`; else seed `Items` from `detail.Endpoints` projected to `ClusterEndpointEditItem` (with `Id` carried so the editor can tell pre-existing rows from new ones — though for persistence this is irrelevant since the service uses full-replace; the Id just travels for the future).
- [x] 7.3 Render `<ClusterEndpointEditor Items="@Items" />` inside `<DialogContent>`. DialogActions: 取消 (text) / 保存 (filled, color primary, start icon Save).
- [x] 7.4 `Submit` handler: `await ClusterService.UpdateClusterEndpointsAsync(ClusterId, Items)` inside try/catch; on success snackbar "端点已更新" + `Dialog.Close(DialogResult.Ok(true))`; on `ArgumentException` snackbar the exception message + keep dialog open (return without closing).
- [x] 7.5 Wrap the whole `<MudDialog>` content in `<AuthorizeView Roles="Admin"><Authorized>...</Authorized></AuthorizeView>` so non-admin callers can't see the editor controls even if the dialog is somehow opened.

## 8. AddClusterDialog — capture endpoints at creation

- [x] 8.1 In `Components/Clusters/Shared/AddClusterDialog.razor` add a class-level field `private List<ClusterEndpointEditItem> endpoints = new();` initialized to a fresh list per dialog session
- [x] 8.2 Render `<ClusterEndpointEditor Items="@endpoints" />` after the existing form fields (before `</MudForm>`) — keep inside the `MudForm` so the editor's required-Value validation participates in `form.ValidateAsync()`
- [x] 8.3 In `Submit`, populate `vm.Endpoints = endpoints` before calling `ClusterService.AddClusterAsync(vm)`
- [x] 8.4 Visually separate the endpoint editor from the connection fields with a `MudText Typo="Typo.subtitle2"` "集群端点" label or a `MudDivider Class="my-3"` so the form reads as two sections

## 9. ClusterDetail.razor — rewrite from the commented original

- [x] 9.1 Remove the surrounding `@* ... *@` comment block at `Components/Clusters/Pages/ClusterDetail.razor` (lines 1 and 412). The first two lines become `@attribute [Authorize]` + `@page "/clusters/{Id:int}"`.
- [x] 9.2 Update `@using` directives to include `MultiClusterMgmtSys.Components.Clusters.Services`, `MultiClusterMgmtSys.Components.Clusters.Shared`, `MultiClusterMgmtSys.Components.Clusters.ViewModels`, `MultiClusterMgmtSys.Common.Enums` (the existing commented file has `@using MultiClusterMgmtSys.Models` which is stale / wrong — drop it).
- [x] 9.3 Top Toolbar — render a `MudPaper Class="pa-4 mb-4"` containing a single row: `<MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.ArrowBack" OnClick="@(() => NavigationManager.NavigateTo("/clusters"))">返回列表</MudButton>`; `<MudText Typo="Typo.h4" Class="flex-auto ml-2">@cluster.Name</MudText>`; a status `<MudChip Color="GetStatusColor(cluster.Status)">@cluster.StatusText</MudChip>`; an Admin-only `<AuthorizeView Roles="Admin"><Authorized>` action group with `[刷新状态]` (filled primary, refresh icon, calls `RefreshStatus`), `[编辑]` (outlined, edit icon, `Disabled="true"`, tooltip "编辑集群功能暂未实现"), `[删除]` (filled error, delete icon, calls `DeleteCluster`).
- [x] 9.4 Card 1 — 概览: `MudCard Elevation="1" Class="mb-4"` containing the same 7 fields from the commented original (`名称`, `版本`, `节点数`, `所属分组`, `API Server`, `创建时间`, `最后检测时间`) laid out in a `MudGrid Spacing="2"` with `MudItem xs="12" sm="6" md="4"` cells. Reuse exact values & formatting (`cluster.Version ?? "—"`, `cluster.GroupName ?? "未分组"`, etc.) from the commented original.
- [x] 9.5 Card 2 — 集群端点: `MudCard Elevation="1" Class="mb-4"`. Header row: `<MudText Typo="Typo.h6">集群端点</MudText>` + Admin-only `MudButton Variant="Variant.Text" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Settings" OnClick="OpenEndpointsDialog"` labeled "管理". Body: if `cluster.Endpoints.Count == 0`, render `<MudText Class="mud-text-secondary">未登记任何端点</MudText>` plus an Admin-only "管理" prompt; otherwise render a single `MudTable` (no per-kind group headers) with columns 类型 (`MudChip` labeled with `KindText`), 地址 (`Value` in a monospace font subtly via `Style="font-family: monospace;"`), 备注 (`Note` if present), and a `MudIconButton` copy affordance (`Icons.Material.Filled.ContentCopy`, `OnClick` calls `CopyToClipboard(context.Value)`). Rows sorted by `Kind` (VIP first, then Domain), then by `SortOrder` ascending.
- [x] 9.6 `CopyToClipboard(string value)` — implement using `IJSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value)`; on completion `Snackbar.Add("已复制到剪贴板", Severity.Success)`; on `JSException` catch `Snackbar.Add($"复制失败: {ex.Message}", Severity.Error)`. Inject `IJSRuntime JS` at the top of the `@code` block.
- [x] 9.7 Card 3 — 连接信息: entire card wrapped in `<AuthorizeView Roles="Admin"><Authorized>`. Same content layout as the commented original (lines 92-157 of the commented file): `MudCard` with `MudCardHeader` showing "连接信息" + the show-secret toggle `MudButton` calling `ToggleSecret`; `MudCardContent` with `连接方式` (`KubeConfig` vs `Token`) + `API Server`; if `showSecret && editForSecret is not null` render the same `MudTextField` (ReadOnly, Lines, password/text toggle, adornment visibility icon) for either `editForSecret.KubeConfig` or `editForSecret.Token` per `ConnectionType`. Keep `ToggleSecret`, `editForSecret`, `showSecret`, `showSecretContent` fields and the lazy-load-via-`GetClusterForEditAsync` logic unchanged.
- [x] 9.8 Card 4 — 节点列表预览: `MudCard Elevation="1"` with `MudCardHeader` showing "节点列表" + a "查看全部" `MudButton EndIcon="@Icons.Material.Filled.ArrowForward"` navigating to `/nodes/{Id}`; body shows up to 5 of `cluster.Nodes` in the same `MudTable Dense Hover Striped Elevation="0"` style as the commented original (lines 187-220). If `!cluster.IsReachable` render `集群不可达，无法获取节点列表`; if `cluster.Nodes.Count == 0` render `暂无节点数据`; clicking a node name still navigates to `/nodes/{Id}/{nodeName}` via the existing `NavigateToNode` helper. Crop the table to the first 5 rows via `cluster.Nodes.Take(5)`.
- [x] 9.9 Delete the standalone "操作" card from the commented original (lines 230-267) — those buttons are now folded into the toolbar Admin action group per task 9.3.
- [x] 9.10 `@code` block: keep the existing `Id`, `cluster`, `loading`, `processing`, `showSecret`, `showSecretContent`, `editForSecret` fields; `OnInitializedAsync` → `LoadAsync`; `LoadAsync` calling `ClusterService.GetClusterDetailAsync(Id)`; `ToggleSecret`; `RefreshStatus` calling `RefreshClusterStatusAsync` + `LoadAsync`; `DeleteCluster` with messagebox confirm + `DeleteClusterAsync` + navigate to `/clusters`; `GetStatusColor`; `GetNodeStatusColor`; `NavigateToNode`. Add `OpenEndpointsDialog` — `DialogParameters<ClusterEndpointsDialog> { { d => d.ClusterId, Id } }` + `DialogService.ShowAsync<ClusterEndpointsDialog>("管理端点", parameters, options)`; on `!result.Canceled` call `await LoadAsync()` to refresh the Endpoints card.
- [x] 9.11 No `OpenEditClusterDialog` method — remove it from the new file (the commented original had one at lines 351-364). The "编辑" button in the toolbar doesn't need an `OnClick` since it is `Disabled`.

## 10. Verification

- [x] 10.1 `dotnet build MultiClusterMgmtSys.slnx` succeeds with no warnings about the new `ClusterEndpoint` entity or any removed field
- [x] 10.2 `openspec validate --changes redesign-cluster-detail-with-endpoints` passes (no orphaned requirements, all spec capabilities reference real files)
- [ ] 10.3 Manual: stop app, delete `MultiClusterMgmtSys.db`, restart, log in as `admin / Changeme_123`, navigate to `/clusters`, click any cluster name row → `/clusters/{id}` renders the rebuilt detail page with no 404
- [ ] 10.4 Manual: Admin clicks "管理" on the Endpoints card, dialog opens, adds 2 VIPs and 2 domains, assigns SortOrder integers, clicks 保存 → snackbar "端点已更新", dialog closes, detail page Endpoints card re-renders as a single table sorted per the spec (VIP rows above Domain rows, SortOrder within each kind)
- [ ] 10.5 Manual: Admin submits an empty Value for one row → service throws `ArgumentException`, snackbar surfaces the message, no rows are mutated in the DB
- [ ] 10.6 Manual: Admin creates a new cluster via "添加集群" with 3 endpoints at creation time, then opens the new cluster's detail page — endpoints show up without having to open the management dialog
- [ ] 10.7 Manual: Admin clicks the copy affordance on an endpoint row → "已复制到剪贴板" snackbar, OS clipboard contains the Value
- [ ] 10.8 Manual: Member logs in, navigates to `/clusters/{id}` — Overview, Endpoints (read-only, no 管理 button, no copy button visible or the copy button shows but functions — per design, copy stays available to Members since endpoints are read-only metadata; verify the 管理 button is hidden and the Connection Info card is hidden entirely)
- [ ] 10.9 Manual: Admin clicks 编辑 in the detail toolbar → button stays `Disabled`, tooltip "编辑集群功能暂未实现" shows on hover, no navigation occurs
- [ ] 10.10 Manual: Admin clicks 删除 in the detail toolbar → confirm messagebox shows cluster name, confirm success navigates back to `/clusters` with success snackbar; the cluster's `ClusterEndpoint` rows are gone from the DB (verify by re-creating the cluster and checking the table is empty or use a SQLite inspector)
- [ ] 10.11 Manual: Admin sets a cluster Status to Offline (or probes a dead cluster), views its detail page — Endpoints card still shows previously-registered endpoints per spec ("Endpoints are app-owned metadata, not k8s-fetched")
- [ ] 10.12 Delete stray local `MultiClusterMgmtSys.db` / `clusters.db` again after manual runs if they accumulate; per `AGENTS.md` these are gitignored runtime artifacts and should not be committed