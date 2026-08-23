## ADDED Requirements

### Requirement: ConfigMap list page cluster selection sidebar and empty state

The system SHALL render the ConfigMap list page (`/configmaps`, `/configmaps/{ClusterId}`) as a two-column layout: a persistent left `ConfigMapClusterSidebar` component and a right content pane. The sidebar SHALL be a 240px `MudPaper` with a 集群选择 header and a `MudNavMenu` listing clusters grouped by `GroupName` ("未分组" last); each group SHALL be collapsible via its header; each cluster row SHALL show the cluster name with a status color-dot and SHALL receive an active-link highlight when it matches the current selection; the sidebar SHALL have NO search box. Clicking a cluster row SHALL `NavigateTo("/configmaps/{ClusterId}")`.

The sidebar's cluster list SHALL be loaded once per page mount from `ClusterService.GetPagedAsync` (page size 1000, sorted by name); grouping happens inside the sidebar component. The page SHALL NOT issue additional cluster-loading calls on re-render.

When the URL carries no `ClusterId`: if `ClusterSelectionState.SelectedClusterId` is null, the right pane SHALL render a vertically-centered "请从左侧选择一个集群" empty state (a large secondary-colored icon and a secondary h6 message). If a cluster id is remembered in `ClusterSelectionState`, the page SHALL load and render that cluster's ConfigMap list **in place** — the URL SHALL remain `/configmaps` (no redirect, no landing card) and the sidebar SHALL highlight the restored cluster row.

#### Scenario: User opens the bare `/configmaps` route with no prior selection

- **WHEN** an authenticated user navigates to `/configmaps` (no `ClusterId`) and `ClusterSelectionState.SelectedClusterId` is null
- **THEN** the page renders the left `ConfigMapClusterSidebar` with the full grouped cluster list
- **AND** the right pane renders the 请从左侧选择一个集群 empty state
- **AND** no 前往集群列表 button is rendered

#### Scenario: User opens the bare `/configmaps` route after previously selecting a cluster in the same circuit

- **WHEN** the user navigates to `/configmaps` and `ClusterSelectionState.SelectedClusterId` is 3 (from a previous visit to `/clusters/3` or `/configmaps/3` in the same circuit)
- **THEN** the URL remains `/configmaps` (no redirect)
- **AND** the right pane renders cluster 3's ConfigMap list in place
- **AND** the sidebar highlights cluster 3's row

#### Scenario: User switches via Drawer between feature pages within the same circuit

- **WHEN** a user is viewing `/configmaps/3` and then clicks the Drawer link to `/nodes`, and later clicks the Drawer link back to 配置管理
- **THEN** the `/configmaps` page restores cluster 3's ConfigMap list in place (because `OnParametersSet` / `LoadAsync` on `/configmaps/3` set `ClusterSelectionState.SelectedClusterId` == 3)
- **AND** the `/nodes` page analogously restores cluster 3 in place rather than rendering an empty state

#### Scenario: User switches via Drawer to 配置管理 after viewing 集群详情

- **WHEN** a user is viewing `/clusters/3` and then clicks the Drawer link to `/configmaps`
- **THEN** the `/configmaps` page's right pane shows cluster 3's ConfigMap list in place (because `ClusterDetail.razor::LoadAsync` set `ClusterSelectionState.SelectedClusterId` == 3)
- **AND** the URL remains `/configmaps` and the sidebar highlights cluster 3's row

#### Scenario: User clicks a cluster in the sidebar

- **WHEN** the user clicks cluster 5's row in `ConfigMapClusterSidebar` on the bare `/configmaps` route
- **THEN** the system `NavigateTo("/configmaps/5")` and the right pane loads cluster 5's ConfigMap list

### Requirement: ConfigMap list page for a selected cluster

The system SHALL render a ConfigMap list page at `/configmaps/{ClusterId}` in the right pane beside the persistent `ConfigMapClusterSidebar`, with a list shell mirroring `Clusters.razor`: a single `MudPaper` containing a title row and the merged `ConfigMapListFilterBar`, followed by a `MudTable` styled like `ClusterTable`.

The title row SHALL contain: a 配置管理 h5, a `MudChip` showing the selected cluster's name and status text, a 刷新 `MudButton`, and a 新建 ConfigMap `MudButton` wrapped in `<AuthorizeView Roles="Admin">`. No back button SHALL be rendered — the selected cluster is identified by the sidebar's active-row highlight. The `ConfigMapListFilterBar` (命名空间 select + 名称 search + 查询 + 重置 buttons, see the namespace-filtering and name-search requirements) SHALL be rendered inside the same `MudPaper` below the title row.

The `MudTable` SHALL use `Items` with client-side paging and SHALL match `ClusterTable`'s visual attributes: `Hover`, `FixedHeader`, `MudTableSortLabel` sortable headers for 名称 / 命名空间 / Data 键数 / 创建时间 (client-side sorting), a 正在加载... `LoadingContent` fed from the page's loading state, and a 暂无 ConfigMap 或没有符合筛选条件的 ConfigMap empty state.

The page SHALL call `ClusterService.GetClusterDetailAsync` to obtain the cluster context (for the chip and `IsReachable` gate) and `ConfigMapService.ListConfigMapsAsync` / `GetNamespacesAsync` to obtain the data. The `ConfigMapListToolbar` component SHALL NOT exist — the title row is inlined in the page.

#### Scenario: Cluster selected and reachable

- **WHEN** an authenticated user navigates to `/configmaps/{ClusterId}` for a reachable cluster
- **THEN** the page renders the `MudPaper` (title row with the cluster name/status chip and the merged filter bar) followed by a `MudTable` of ConfigMaps in that cluster
- **AND** each row shows 名称 (clickable to detail), 命名空间, Data 键数, 键名预览 (ellipsis), 创建时间 (`yyyy-MM-dd HH:mm`), 操作 icons
- **AND** the 名称, 命名空间, Data 键数, and 创建时间 column headers are sortable client-side

#### Scenario: Cluster selected but unreachable

- **WHEN** the cluster exists but `IsReachable` is false
- **THEN** the page renders the `MudPaper` title row (with 新建 ConfigMap visible but Disabled) and a 集群不可达，无法获取 ConfigMap empty card
- **AND** no filter bar or table is rendered

#### Scenario: Cluster not found

- **WHEN** an authenticated user navigates to `/configmaps/{ClusterId}` for a non-existent cluster
- **THEN** the page renders a "未找到该集群" card with a 返回集群列表 button that navigates to `/clusters`

### Requirement: Cluster context is persisted across pages via ClusterSelectionState

The system SHALL provide a scoped DI service `ClusterSelectionState` (in `MultiClusterMgmtSys.Components.Common`) that records the most recently visited cluster id. The service SHALL expose `int? SelectedClusterId { get; }` (with private setter), `void Set(int clusterId)` that records the id, and `void Clear()` that nulls it. The service is registered as `AddScoped<ClusterSelectionState>()` in `Program.cs`.

The following pages SHALL call `ClusterSelectionState.Set` when a `ClusterId` is in scope and the cluster load succeeded:
- `ConfigMaps.razor`'s `OnParametersSet` — when `ClusterId.HasValue`; and its `LoadAsync` — after `ClusterService.GetClusterDetailAsync` returns a non-null cluster.
- `ConfigMapDetail.razor`'s `OnInitializedAsync` — unconditionally on entry (the route parameter is trusted to identify a real cluster because the page gates the entire body behind `detail is not null` anyway).
- `EditConfigMapYaml.razor`'s `OnInitializedAsync` — same as `ConfigMapDetail.razor`.
- `ClusterDetail.razor`'s `LoadAsync` — after `ClusterService.GetClusterDetailAsync` returns a non-null cluster (this enables Drawer-driven switches from a directly-viewed cluster into the ConfigMaps or Nodes feature pages).
- `Nodes.razor`'s `OnParametersSet` — when `ClusterId.HasValue`.
- `NodeDetail.razor`'s `OnParametersSet` — unconditionally on entry.

This requirement replaces the previous `NodeSelectionState` service (a Nodes-only scoped service): `NodeSelectionState` SHALL be renamed to `ClusterSelectionState` and its DI registration, inject sites, and call sites updated across the Nodes and Configmaps feature folders.

#### Scenario: `ClusterDetail.razor` records the cluster id

- **WHEN** `ClusterDetail.razor`'s `LoadAsync` successfully loads cluster 5
- **THEN** `ClusterSelectionState.SelectedClusterId` is 5 (until overwritten by another successful load or `Clear()`)

#### Scenario: `ConfigMaps.razor` records the cluster id

- **WHEN** `ConfigMaps.razor`'s `LoadAsync` for `/configmaps/5` returns a non-null cluster
- **THEN** `ClusterSelectionState.SelectedClusterId` is 5 before the page body renders

#### Scenario: `ConfigMapDetail.razor` records the cluster id on entry

- **WHEN** the user navigates to `/configmaps/3/default/my-cm`
- **THEN** `ClusterSelectionState.Set(3)` is called in `OnInitializedAsync` before the `LoadAsync` call, so the context is locked even if the subsequent detail fetch returns null

#### Scenario: `EditConfigMapYaml.razor` records the cluster id on entry

- **WHEN** an Admin navigates to `/configmaps/3/default/my-cm/yaml`
- **THEN** `ClusterSelectionState.Set(3)` is called in `OnInitializedAsync` before the `LoadAsync` call, so the context is locked even if the subsequent fetch returns null

#### Scenario: `Nodes.razor` records the cluster id

- **WHEN** the user navigates to `/nodes/2`
- **THEN** `ClusterSelectionState.Set(2)` is called in `OnParametersSet` before any async load

### Requirement: Optional cluster context refresh on the list page

The list page's title row SHALL expose a 刷新 `MudButton` that re-runs the cluster detail + namespaces + ConfigMap list loads. Refresh SHALL NOT be gated by `AuthorizeView` — it is a read action.

#### Scenario: User clicks 刷新 on the title row

- **WHEN** an authenticated user clicks 刷新 on the list page title row
- **THEN** the page re-invokes `ClusterService.GetClusterDetailAsync`, `ConfigMapService.GetNamespacesAsync`, and `ConfigMapService.ListConfigMapsAsync`
- **AND** the table, filter bar namespace list, and title-row status chip reflect the latest server state

### Requirement: Namespace filtering on the list page

The filter bar SHALL provide a 命名空间 `MudSelect<string?>` whose options come from `ConfigMapService.GetNamespacesAsync(ClusterId)`, with a leading 全部命名空间 (Value=`null`) option. Changing the selection SHALL only update page state — it SHALL NOT trigger a fetch. Clicking the 查询 button SHALL re-invoke `ConfigMapService.ListConfigMapsAsync(ClusterId, selectedNamespace)` (or `ListConfigMapsAsync(ClusterId, null)` for 全部命名空间) and refresh the table. The 名称 search field SHALL retain its current value across namespace changes.

#### Scenario: User selects a namespace and clicks 查询

- **WHEN** the user selects namespace "default" in the filter bar and clicks 查询
- **THEN** the table re-fetches ConfigMaps for the "default" namespace only
- **AND** the 名称 search field retains its current value

#### Scenario: User changes the namespace without clicking 查询

- **WHEN** the user selects a namespace but does not click 查询
- **THEN** the table is not re-fetched and continues to show the previous results

#### Scenario: User selects "全部命名空间" and clicks 查询

- **WHEN** the user selects 全部命名空间 (empty value) and clicks 查询
- **THEN** the table re-fetches ConfigMaps across all namespaces in the cluster

### Requirement: Client-side name search on the list page

The filter bar SHALL provide a 名称 `MudTextField<string>` that, when the 查询 button is clicked, filters the already-loaded ConfigMap list client-side using `string.Contains(..., StringComparison.OrdinalIgnoreCase)`. Typing SHALL NOT trigger a server round-trip and SHALL NOT re-render the table — the filter applies only when 查询 is clicked. The 重置 button SHALL clear the namespace selection back to 全部命名空间 and the 名称 field to empty, then re-fetch the ConfigMap list.

#### Scenario: User types in the 名称 field and clicks 查询

- **WHEN** the user types "app" into the 名称 field and clicks 查询
- **THEN** the table filters its rows to ConfigMaps whose `Name` contains "app" case-insensitively
- **AND** no additional network call is made (the filter is applied to the already-loaded list)

#### Scenario: User types without clicking 查询

- **WHEN** the user types into the 名称 field but does not click 查询
- **THEN** the table continues to show the unfiltered results

#### Scenario: Search yields no rows

- **WHEN** a name search filters out all rows
- **THEN** the table renders the 暂无 ConfigMap 或没有符合筛选条件的 ConfigMap empty state
- **AND** clicking 重置 clears the namespace and 名称 fields and re-fetches the list

### Requirement: Admin-only "新建 ConfigMap" button on the list page

The 新建 ConfigMap button in the list page's title row SHALL be wrapped in `<AuthorizeView Roles="Admin">`. The button SHALL be Disabled when `cluster is null` or `!cluster.IsReachable`. Clicking the button SHALL open `CreateConfigMapDialog` via `DialogService.ShowAsync<CreateConfigMapDialog>` with `ClusterId` as a dialog parameter.

#### Scenario: Admin user on a reachable cluster

- **WHEN** an Admin user opens `/configmaps/{ClusterId}` for a reachable cluster
- **THEN** the 新建 ConfigMap button is visible and enabled
- **AND** clicking it opens the create dialog scoped to `ClusterId`

#### Scenario: Member user (non-Admin) on any cluster

- **WHEN** a non-Admin user opens the list page
- **THEN** the 新建 ConfigMap button is not rendered

#### Scenario: Admin user on an unreachable cluster

- **WHEN** an Admin user opens the list page for a cluster whose `IsReachable` is false
- **THEN** the 新建 ConfigMap button is visible but Disabled

### Requirement: Row actions on the list table

Each table row SHALL expose three action icon buttons: 详情 (always visible to all authenticated users, navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}`), 编辑 YAML (wrapped in `<AuthorizeView Roles="Admin">`, navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml`), 删除 (wrapped in `<AuthorizeView Roles="Admin">`, opens a confirm dialog and then calls `ConfigMapService.DeleteConfigMapAsync`).

#### Scenario: Non-Admin user sees row actions

- **WHEN** a non-Admin user views the table
- **THEN** the 详情 icon button is rendered on each row
- **AND** the 编辑 YAML and 删除 icon buttons are not rendered

#### Scenario: Admin user deletes a ConfigMap

- **WHEN** an Admin user clicks the 删除 icon button on a row
- **THEN** the system opens a confirmation dialog asking 确认删除 ConfigMap「{name}」？此操作不可撤销
- **AND** if the user confirms, the system calls `ConfigMapService.DeleteConfigMapAsync(ClusterId, name, ns)`
- **AND** on success the system shows a 删除成功 snackbar and refreshes the table
- **AND** no undo affordance is provided

#### Scenario: Delete fails due to 404

- **WHEN** the delete API returns a 404 / Not Found error
- **THEN** the system shows a ConfigMap 不存在或已被删除 warning snackbar (not an error)

### Requirement: ConfigMap detail page as read-only YAML

The system SHALL render a ConfigMap detail page at `/configmaps/{ClusterId}/{Namespace}/{Name}` as a toolbar (`ConfigMapDetailToolbar`: 返回列表 + `{Name}` h4 + "Data 键数: {n}" `MudChip` + 编辑 YAML button admin-gated + 刷新 button) followed by a single `ConfigMapYamlViewCard` containing a read-only `MudTextField` Lines=30 monospace bound to `ConfigMapDetailViewModel.Yaml`. The page SHALL NOT render `MudTabs`, per-key `TabPanel`s, or any per-data-key editing surface — the YAML is the only view of the resource.

#### Scenario: Successful detail load

- **WHEN** an authenticated user navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}` for a real ConfigMap
- **THEN** the page renders the toolbar with the resource's `Name` in the h4 and a chip showing the count of `Data` keys
- **AND** a single read-only monospace YAML field renders the full `V1ConfigMap` YAML (including `data`, `binaryData`, `labels`, `annotations`, `metadata`)

#### Scenario: ConfigMap not found

- **WHEN** the detail page loads a ConfigMap whose `GetConfigMapAsync` returns null
- **THEN** the page renders a ConfigMap 不存在或已被删除 empty state with a 返回列表 button
- **AND** a warning snackbar appears

### Requirement: Admin-only edit-YAML navigation from the detail page

The 编辑 YAML button in `ConfigMapDetailToolbar` SHALL be wrapped in `<AuthorizeView Roles="Admin">` and SHALL navigate to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` on click. Non-Admin users see no edit affordance.

#### Scenario: Admin user opens edit from detail

- **WHEN** an Admin user on the detail page clicks 编辑 YAML
- **THEN** the system navigates to `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml`

#### Scenario: Non-Admin user on the detail page

- **WHEN** a non-Admin user views the detail page
- **THEN** no 编辑 YAML button is rendered in the toolbar

### Requirement: YAML editor page for an existing ConfigMap

The system SHALL render a YAML editor page at `/configmaps/{ClusterId}/{Namespace}/{Name}/yaml` as a toolbar (`EditConfigMapYamlToolbar`: 返回列表 + 编辑 YAML: `{Name}` h4 + 保存 `MudButton`) followed by a single `ConfigMapYamlEditCard` containing an editable `MudTextField` Lines=30 monospace `@bind-Value=yamlContent`. The page SHALL load `ConfigMapService.GetConfigMapAsync` to seed `yamlContent` from `ConfigMapDetailViewModel.Yaml`. The page SHALL be reachable only by Admin users (`@attribute [Authorize(Roles="Admin")]`).

#### Scenario: Successful editor load

- **WHEN** an Admin user navigates to the YAML editor for an existing ConfigMap
- **THEN** the page renders the toolbar with the resource name and an editable monospace YAML field
- **AND** the YAML field is pre-filled with the resource's current full YAML

#### Scenario: Resource disappears during edit

- **WHEN** `GetConfigMapAsync` returns null on editor load
- **THEN** the page renders a ConfigMap 不存在或已被删除 empty state with a 返回列表 button
- **AND** no editable YAML field is rendered

### Requirement: YAML pre-parse before save on the edit page

The YAML editor 保存 handler SHALL first invoke `KubernetesYaml.Deserialize<V1ConfigMap>(yamlContent)` inside a try/catch. If the pre-parse throws, the handler SHALL show a YAML 格式错误: {ex.Message} error snackbar, SHALL NOT call the service, SHALL NOT close the page, and SHALL NOT clear `yamlContent`.

#### Scenario: User submits malformed YAML

- **WHEN** an Admin user clicks 保存 with a `yamlContent` that fails `KubernetesYaml.Deserialize<V1ConfigMap>`
- **THEN** the system shows an error snackbar containing YAML 格式错误 and the parse exception message
- **AND** no K8s API call is made

#### Scenario: User submits syntactically valid YAML

- **WHEN** an Admin user clicks 保存 with a `yamlContent` that parses cleanly
- **THEN** the handler calls `ConfigMapService.UpdateConfigMapFromYamlAsync(ClusterId, name, ns, yamlContent)`

### Requirement: YAML edit preserves metadata

The system SHALL update an existing ConfigMap by calling `ConfigMapService.UpdateConfigMapFromYamlAsync`, which SHALL preserve the existing `V1ConfigMap`'s `Metadata` (name, namespace, uid, labels, annotations, resourceVersion) and overwrite only `Data` and `BinaryData` from the deserialized user YAML. The user SHALL NOT be able to change `metadata.name`, `metadata.namespace`, `metadata.labels`, or `metadata.annotations` via this edit path.

#### Scenario: User's YAML contains a changed label

- **WHEN** the user's submitted YAML has `metadata.labels.foo: bar` that differs from the server-side value
- **THEN** the saved resource's `metadata.labels` retains the server-side value unchanged
- **AND** only `data` and `binaryData` reflect the user's submission

#### Scenario: User's YAML changes BinaryData

- **WHEN** the user's submitted YAML's `binaryData` block contains a new key
- **THEN** the saved resource's `BinaryData` reflects the new key/value pair

#### Scenario: User's YAML drops BinaryData entirely (by-design; target scenario does not involve binaryData)

- **WHEN** the user's submitted YAML has no `binaryData` block but the server-side resource had BinaryData
- **THEN** the saved resource's `BinaryData` is set to null/empty (server-side BinaryData is lost)
- **AND** no confirmation dialog or undo is presented (this is documented by-design behavior; the target deployment scenario does not involve binaryData-bearing ConfigMaps)

### Requirement: Save success navigation on the edit page

On a successful save, the YAML editor 保存 handler SHALL show a 修改成功 success snackbar and SHALL navigate to `/configmaps/{ClusterId}` (the list page, not the detail page). The 返回列表 button in the toolbar SHALL also navigate to `/configmaps/{ClusterId}`.

#### Scenario: Save succeeds

- **WHEN** `UpdateConfigMapFromYamlAsync` completes without throwing
- **THEN** the system shows a 修改成功 snackbar
- **AND** the system navigates to `/configmaps/{ClusterId}`

#### Scenario: Save fails with a 409 Conflict

- **WHEN** the K8s API returns 409 / Conflict
- **THEN** the system shows a 资源已被他人修改，请刷新后重试 warning snackbar
- **AND** the page stays on the editor route, retaining `yamlContent`

### Requirement: YAML-only create dialog with pre-filled minimal template

The system SHALL provide a `CreateConfigMapDialog` opened by the list page's 新建 ConfigMap button. The dialog body SHALL contain a single `MudTextField` Lines=25 monospace `@bind-Value=yamlContent` pre-populated on `OnInitializedAsync` with the literal string:
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: 
  namespace: 
data: {}
```

#### Scenario: Admin opens the create dialog

- **WHEN** an Admin user clicks 新建 ConfigMap on the list page
- **THEN** the dialog opens with the YAML field pre-filled with the minimal template above
- **AND** the cursor focus is in the YAML field

### Requirement: Create dialog pre-parse before submit

The CreateConfigMapDialog 提交 handler SHALL first invoke `KubernetesYaml.Deserialize<V1ConfigMap>(yamlContent)` inside a try/catch. If the pre-parse throws, the handler SHALL show a YAML 格式错误: {ex.Message} error snackbar and SHALL keep the dialog open. On pre-parse success, the handler SHALL call `ConfigMapService.CreateConfigMapFromYamlAsync(ClusterId, yamlContent)`.

#### Scenario: User submits malformed YAML in the dialog

- **WHEN** an Admin user clicks 创建 with a `yamlContent` that fails `KubernetesYaml.Deserialize<V1ConfigMap>`
- **THEN** the dialog shows an error snackbar containing YAML 格式错误 and the parse exception message
- **AND** the dialog stays open with the YAML content retained
- **AND** no K8s API call is made

#### Scenario: Valid YAML missing namespace

- **WHEN** the pre-parse succeeds but `metadata.NamespaceProperty` is null or whitespace
- **THEN** `ConfigMapService.CreateConfigMapFromYamlAsync` throws an `InvalidOperationException("YAML metadata.namespace 未指定")`
- **AND** the dialog shows the exception message as an error snackbar
- **AND** the dialog stays open

### Requirement: Create dialog success and conflict mapping

On a successful `CreateConfigMapFromYamlAsync` call, the dialog SHALL show a 创建成功 snackbar and close with `DialogResult.Ok(true)`. On a 409 / Conflict / Already Exists error from the K8s API, the dialog SHALL show a 同名 ConfigMap 已存在 warning snackbar and stay open. On any other exception, the dialog SHALL show a 创建失败: {ex.Message} error snackbar and stay open.

#### Scenario: Successful create

- **WHEN** `CreateConfigMapFromYamlAsync` completes without throwing
- **THEN** the dialog closes with `DialogResult.Ok(true)`
- **AND** a 创建成功 snackbar appears
- **AND** the list page refreshes its ConfigMap table

#### Scenario: Same-name ConfigMap already exists

- **WHEN** the K8s API returns 409 / Conflict / Already Exists
- **THEN** the dialog shows a 同名 ConfigMap 已存在 warning snackbar
- **AND** the dialog stays open so the user can rename in the YAML

### Requirement: Removed form-editor route

The system SHALL NOT register a `@page "/configmaps/{ClusterId:int}/{Namespace}/{Name}/edit"` route. The file `Components/Configmaps/Pages/EditConfigMap.razor` SHALL be deleted. A user navigating to that URL via a stale bookmark SHALL observe Blazor's default 404 / "Not found" route behavior.

#### Scenario: Stale bookmark to form editor

- **WHEN** a user navigates to `/configmaps/1/default/my-cm/edit`
- **THEN** the application does not match the route and renders the app's not-found fallback

### Requirement: Removed form-based view models

The system SHALL delete `Components/Configmaps/ViewModels/ConfigMapCreateViewModel.cs`, `Components/Configmaps/ViewModels/ConfigMapUpdateViewModel.cs`, and `Components/Configmaps/ViewModels/ConfigMapDataEntryViewModel.cs`. The system SHALL modify `Components/Configmaps/ViewModels/ConfigMapDetailViewModel.cs` to change the `Data` field type from `List<ConfigMapDataEntryViewModel>` to `Dictionary<string, string>` (preserving `Data.Count` for the toolbar chip). The system SHALL trim `Components/Configmaps/ViewModels/Mappings/ConfigMapMappingExtensions.cs` to only `ToConfigMapListViewModel(this V1ConfigMap)` and `ToConfigMapDetailViewModel(this V1ConfigMap)` with the `Data` assignment updated to `cm.Data?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? "") ?? new()`. `ConfigMapListViewModel` SHALL be reused unmodified.

#### Scenario: Build after cleanup

- **WHEN** `dotnet build MultiClusterMgmtSys.slnx` is run after the change is complete
- **THEN** the build succeeds with no warnings referencing removing types
- **AND** no remaining file references `ConfigMapCreateViewModel`, `ConfigMapUpdateViewModel`, or `ConfigMapDataEntryViewModel`

### Requirement: New service method CreateConfigMapFromYamlAsync

`ConfigMapService` SHALL expose `public async Task CreateConfigMapFromYamlAsync(int clusterId, string yaml)` that resolves the cluster via `ClusterRepository.GetByIdAsync`, builds a `Kubernetes` client with the existing `BuildConfig` helper, deserializes the YAML into a `V1ConfigMap`, and calls `CoreV1.CreateNamespacedConfigMapAsync(body, body.Metadata.NamespaceProperty)` throwing `InvalidOperationException("YAML metadata.namespace 未指定")` if the namespace is null or whitespace. `CreateConfigMapAsync(ConfigMapCreateViewModel)` and `UpdateConfigMapAsync(ConfigMapUpdateViewModel)` SHALL be deleted from `ConfigMapService`. `GetNamespacesAsync`, `ListConfigMapsAsync`, `GetConfigMapAsync`, `DeleteConfigMapAsync`, and `UpdateConfigMapFromYamlAsync` SHALL remain unchanged.

#### Scenario: Create via YAML succeeds

- **WHEN** `CreateConfigMapFromYamlAsync` is called with a valid `V1ConfigMap` YAML including a non-empty `metadata.namespace`
- **THEN** the method creates the ConfigMap in the specified namespace on the specified cluster
- **AND** returns without throwing

#### Scenario: YAML missing namespace

- **WHEN** `CreateConfigMapFromYamlAsync` is called with a YAML that has empty/whitespace `metadata.namespace`
- **THEN** the method throws `InvalidOperationException` with message containing YAML metadata.namespace 未指定
- **AND** no K8s API call is made

### Requirement: Namespace hygiene on new Configmaps code

All new `.razor` files under `Components/Configmaps/Shared/` and all rewritten pages under `Components/Configmaps/Pages/` SHALL use `MultiClusterMgmtSys.Features.Configmaps.*` namespaces (matching the existing rule for that folder per AGENTS.md; the opposite of the Nodes rule which is `Components.Nodes.*`). Each new file SHALL carry its own `@using` directives for `Features.Configmaps.Services`, `Features.Configmaps.ViewModels`, and `Features.Configmaps.ViewModels.Mappings` as needed — `_Imports.razor` SHALL NOT be edited by this change.

#### Scenario: New shared component namespace

- **WHEN** `ConfigMapListFilterBar.razor` is created under `Components/Configmaps/Shared/`
- **THEN** the `@code` block (or `@inherits` if used) declares a namespace under `MultiClusterMgmtSys.Features.Configmaps.*`
- **AND** the file carries an explicit `@using MultiClusterMgmtSys.Features.Configmaps.Services` or whichever `Features.Configmaps.*` namespaces it consumes