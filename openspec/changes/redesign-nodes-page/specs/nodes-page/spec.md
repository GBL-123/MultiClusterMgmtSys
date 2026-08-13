## ADDED Requirements

### Requirement: Node list page route and layout

The system SHALL serve the node list page from a single `Components/Nodes/Pages/Nodes.razor` component that declares two routes: a parameterless `/nodes` route and a parameterized `/nodes/{ClusterId:int}` route. The `ClusterId` route parameter MUST be `int?` so the same component handles both routes. The page SHALL use a two-column layout mirroring the cluster management page (`Clusters.razor`): a fixed-width cluster-selection sidebar on the left (`NodeClusterSidebar`, see the dedicated requirement below) and a flexible content pane on the right. The right pane, when a `ClusterId` is present, displays the live nodes of the selected cluster using the project's current list-page visual vocabulary: a top toolbar `MudPaper` (back to cluster detail + cluster name + cluster status chip + refresh), a filter bar `MudPaper`, and a single client-paged `MudTable` listing `ClusterNodeViewModel` rows. The page MUST gate node loading on the cluster being reachable (`IsReachable == true`). The parameterless `/nodes` route is what the Drawer's "节点管理" navigation (`Href="/nodes"`) targets; its right pane behavior is defined by the "Cluster-selection empty state" requirement and the cluster-selection memory behavior.

#### Scenario: Parameterized route resolves to the cluster node list
- **WHEN** an authenticated user navigates to `/nodes/{ClusterId}`
- **THEN** the request resolves to `Components/Nodes/Pages/Nodes.razor` (no longer a commented/404 page)
- **AND** the page renders the sidebar and the right pane with the toolbar, filter bar, and table regions in that vertical order
- **AND** the sidebar highlights the cluster whose id equals `ClusterId`

#### Scenario: Parameterless route renders the two-column shell
- **WHEN** an authenticated user navigates to `/nodes` (e.g. via the Drawer "节点管理" nav link)
- **THEN** the request resolves to the same `Components/Nodes/Pages/Nodes.razor` component
- **AND** the left sidebar renders (groups + clusters)
- **AND** the right pane renders either the remembered cluster's node list (see the cluster-selection memory requirement) or the empty-state hint (see the "Cluster-selection empty state" requirement)

#### Scenario: Cluster context banner
- **WHEN** the cluster detail has loaded
- **THEN** `NodeListToolbar` renders a "返回集群详情" button targeting `/clusters/{ClusterId}`, the cluster's `Name` as an `h4` heading, the cluster's `StatusText` as a colored `MudChip`, and a "刷新" button
- **AND** clicking "返回集群详情" navigates to `/clusters/{ClusterId}`

#### Scenario: Unreachable cluster shows a message instead of the table
- **WHEN** `ClusterDetailViewModel.IsReachable == false`
- **THEN** the page renders a "集群不可达，无法获取节点列表" message in place of the table
- **AND** the page does not invoke `ClusterNodeService.GetClusterNodesAsync`

#### Scenario: Refresh re-fetches both cluster context and node list
- **WHEN** the user clicks the toolbar "刷新" button
- **THEN** the page calls `ClusterService.GetClusterDetailAsync(ClusterId)` and, if reachable, `ClusterNodeService.GetClusterNodesAsync(ClusterId)`, then re-renders

### Requirement: Node cluster sidebar with collapsible groups

The system SHALL render a `NodeClusterSidebar` component (under `Components/Nodes/Shared/`) as the left column of the node list page. Its visual design SHALL mirror `Components/Clusters/Shared/GroupSidebar.razor` (fixed 240px `MudPaper`, header title, `MudNavMenu` list). It SHALL list the user's clusters grouped by their `GroupName` (ungrouped clusters under a "未分组" section), and each group section SHALL be collapsible/expandable — collapsed by default or remembered per interaction, at the implementer's choice, but collapse state MUST be per-group and toggleable by the user. Each cluster row SHALL show the cluster name and a status indicator colored by `ClusterStatus` (`Online` → Success, `Offline` → Error, otherwise Default); the row for the currently selected cluster (`SelectedClusterId`) SHALL be visually highlighted (primary background + primary-text color, same treatment as `GroupSidebar`'s active link). Clicking a cluster row SHALL navigate to `/nodes/{ClusterId}`. The sidebar SHALL NOT contain a cluster search box in this iteration.

#### Scenario: Groups render as collapsible sections with clusters underneath
- **WHEN** the user has clusters in groups "生产" and "测试" plus one ungrouped cluster
- **THEN** the sidebar renders three sections labelled 生产 / 测试 / 未分组 (未分组 listed last), each listing its cluster rows underneath
- **AND** each section header is clickable and collapses/expands its cluster rows

#### Scenario: Cluster rows carry a status indicator
- **WHEN** a cluster row renders for an online cluster
- **THEN** the row shows the cluster name with a green (Success) status indicator
- **WHEN** the row is for an offline cluster
- **THEN** the indicator is red (Error)

#### Scenario: Selected cluster is highlighted
- **WHEN** the page is at `/nodes/{ClusterId}` and the sidebar renders
- **THEN** the row whose cluster id equals `ClusterId` has the active-link highlight style

#### Scenario: Clicking a cluster navigates to its node list
- **WHEN** the user clicks a cluster row in the sidebar
- **THEN** the browser navigates to `/nodes/{ClusterId}` for that cluster
- **AND** the right pane loads and displays that cluster's nodes

#### Scenario: No search box in this iteration
- **WHEN** the sidebar renders
- **THEN** it contains no `MudTextField` or other cluster-search control (explicitly out of scope for this change)

### Requirement: Cluster-selection empty state for the right pane

When `Nodes.razor` renders the parameterless `/nodes` route AND no cluster is remembered in `ClusterSelectionState` (or the page does not apply memory — see the memory requirement below), the right content pane SHALL show a vertically-centered empty-state block: a `MudIcon` (`Icons.Material.Filled.DeviceHub`, `Size.Large`, `mud-text-secondary`) and a `Typo.h6` `mud-text-secondary` heading "请从左侧选择一个集群". The right pane MUST NOT show the toolbar, filter bar, or table in this state, MUST keep `cluster` / `nodes` empty, and MUST NOT invoke `ClusterService.GetClusterDetailAsync` or `ClusterNodeService.GetClusterNodesAsync`. (No "前往集群列表" button is needed — the cluster-selection affordance is the sidebar itself.)

#### Scenario: First-visit empty pane
- **WHEN** an authenticated user lands on `/nodes` with no remembered cluster
- **THEN** the right pane renders the icon and the "请从左侧选择一个集群" heading
- **AND** no `ClusterService` / `ClusterNodeService` call is made for the right pane during this render

### Requirement: Cluster selection is expressed in the URL and remembered in session

The selected cluster for the node list page SHALL be expressed by the URL path (`/nodes/{ClusterId}`) as the source of truth. In addition, the page SHALL remember the selection for the current scoped session by writing `ClusterId` into the shared `ClusterSelectionState` service (declared at `Components/Common/ClusterSelectionState.cs`, registered `AddScoped` in `Program.cs`, namespace `MultiClusterMgmtSys.Components.Common`; the service holds a single `int? SelectedClusterId` with `Set(int)` / `Clear()`). `Nodes.razor` and `NodeDetail.razor` SHALL `@inject ClusterSelectionState` and call `ClusterSelection.Set(ClusterId)` whenever they render with a non-null `ClusterId` (in `OnParametersSet`, before `OnParametersSetAsync` runs the data-load diff). When `Nodes.razor` renders the parameterless `/nodes` route and `ClusterSelectionState.SelectedClusterId` has a value, the page SHALL load and display that remembered cluster's node list in the right pane WITHOUT navigating the URL (the URL stays `/nodes`), so returning to the Drawer's "节点管理" entry restores the previously selected cluster; the sidebar SHALL highlight the remembered cluster. If the service has no value, the empty-state pane renders. The remembered value lives only in the scoped DI instance — a full browser refresh clears it.

#### Scenario: Write site — list page records the current cluster
- **WHEN** `Nodes.razor` is rendered with `ClusterId == 7`
- **THEN** `ClusterSelectionState.SelectedClusterId` is set to `7` synchronously in `OnParametersSet`
- **AND** subsequent reads of the service see the value `7`

#### Scenario: Write site — detail page records the current cluster
- **WHEN** `NodeDetail.razor` is rendered with `ClusterId == 7` and `NodeName == "worker-1"`
- **THEN** `ClusterSelectionState.SelectedClusterId` is set to `7` synchronously in `OnParametersSet` (regardless of which node name is selected)

#### Scenario: Return nav restores the last cluster in place
- **WHEN** the user, after having visited `/nodes/7`, side-tracks to `/clusters` then clicks the Drawer "节点管理" nav (`/nodes`)
- **THEN** `Nodes.razor` renders with `ClusterId == null`
- **AND** the page reads `ClusterSelectionState.SelectedClusterId == 7`
- **AND** the right pane loads and displays the node list for cluster `7` without changing the URL (browser URL remains `/nodes`)
- **AND** the sidebar highlights the row for cluster `7`

#### Scenario: First-time visitor with no recorded choice
- **WHEN** a fresh scoped session begins and the user navigates to `/nodes` for the first time
- **THEN** `ClusterSelectionState.SelectedClusterId` is `null`
- **AND** the right pane renders the empty-state hint ("请从左侧选择一个集群")

#### Scenario: Refresh clears the session memory
- **WHEN** the user is on `/nodes` showing the remembered cluster's list, refreshes the browser tab, then lands on `/nodes` again
- **THEN** `ClusterSelectionState.SelectedClusterId` is `null` (a new scoped instance was constructed post-refresh)
- **AND** the right pane renders the empty-state hint (the user picks a cluster from the sidebar again)

### Requirement: Node list filter bar with four filters

The list page SHALL render `NodeListFilterBar` containing exactly four filter controls bound to a single `NodeListFilter` state record held by the page: a free-text Name field, a Role drop-down, a Status drop-down, and a Schedulability drop-down. Filtering MUST be performed client-side against the loaded `List<ClusterNodeViewModel>` — no new server round-trip is introduced when a filter changes.

#### Scenario: Name filter matches by substring (case-insensitive)
- **WHEN** `NodeListFilter.Name` is a non-empty string
- **THEN** the table shows only nodes whose `Name` contains the filter value (ordinalIgnoreCase)

#### Scenario: Role filter narrows by label-derived role
- **WHEN** `NodeListFilter.Role` is a non-null string (e.g. `"control-plane"`, `"worker"`)
- **THEN** the table shows only nodes whose `Roles` field (a comma-joined list of `node-role.kubernetes.io/<role>` labels) contains that value as one of its comma-separated segments

#### Scenario: Status filter narrows by Ready condition
- **WHEN** `NodeListFilter.Status` is `"Ready"` / `"NotReady"` / `"Unknown"`
- **THEN** the table shows only nodes whose `ClusterNodeViewModel.Status` equals that exact string

#### Scenario: Schedulability filter narrows by Unschedulable flag
- **WHEN** `NodeListFilter.Schedulable` is `true`
- **THEN** the table shows only nodes whose `Unschedulable` is `true` (i.e. cordoned / not schedulable)
- **WHEN** `NodeListFilter.Schedulable` is `false`
- **THEN** the table shows only nodes whose `Unschedulable` is `false` (i.e. schedulable)
- **WHEN** `NodeListFilter.Schedulable` is `null`
- **THEN** the table applies no schedulability filter

#### Scenario: All filters compose
- **WHEN** multiple filters are set simultaneously
- **THEN** the table shows the intersection of all active filters

### Requirement: Node list table columns and row interaction

`NodeListTable` SHALL render a `MudTable<ClusterNodeViewModel>` with `Dense`, `Hover`, a client-paging `MudTablePager`, and exactly six columns in this left-to-right order: 名称, 状态, 角色, Kubelet 版本, 操作系统, 内网 IP. The 名称 cell SHALL be a clickable underline-styled `MudText` that navigates to `/nodes/{ClusterId}/{NodeName}`. The 状态 cell SHALL render a `MudChip` colored by the standard node-status color helper (`Ready` → Success, `NotReady` → Error, otherwise Default).

#### Scenario: Empty state copy
- **WHEN** the filtered row set is empty
- **THEN** the table's `NoRecordsContent` renders "暂无节点数据" or "没有符合当前筛选条件的节点" (the latter when at least one filter is active)

#### Scenario: Row name click navigates to detail
- **WHEN** the user clicks a row's 名称 cell
- **THEN** the browser navigates to `/nodes/{ClusterId}/{NodeName}` for that row

#### Scenario: Pager format matches cluster table
- **WHEN** the pager renders
- **THEN** it uses the same `RowsPerPageString` / `InfoFormat` ("共 {all_items} 条") convention as `ClusterTable.razor`

### Requirement: Node list view model exposes Unschedulable

`ClusterNodeViewModel` SHALL carry a read-only `bool Unschedulable` field populated by `ClusterNodeService.MapNode` from `node.Spec?.Unschedulable ?? false`. The field MUST be additive — no existing property on `ClusterNodeViewModel` is removed or renamed, and no method signature on `ClusterNodeService` changes.

#### Scenario: Mapper populates the new field
- **WHEN** `ClusterNodeService.MapNode(V1Node)` is invoked for a node whose `Spec.Unschedulable` is `true`
- **THEN** the returned `ClusterNodeViewModel.Unschedulable` is `true`
- **WHEN** the same is invoked for a node whose `Spec.Unschedulable` is `false` or absent
- **THEN** the returned `ClusterNodeViewModel.Unschedulable` is `false`

#### Scenario: Existing consumers are unaffected
- **WHEN** any existing reader of `ClusterNodeViewModel` (e.g. `ClusterNodesCard.razor`) renders a row
- **THEN** the row's visible columns are unchanged — `Unschedulable` is silently added and only consumed by the new filter

### Requirement: Node detail page route and layout

The system SHALL serve a node detail page at `/nodes/{ClusterId}/{NodeName}` that displays a single node using the project's current detail-page visual vocabulary: a `NodeDetailToolbar` `MudPaper` followed by a `MudStack flex-auto` of focused MudCards. Each card wraps one section of `ClusterNodeDetailViewModel`. The page MUST NOT render the legacy self-contained heading + flat `MudGrid` layout.

#### Scenario: Route resolves and loads the node
- **WHEN** an authenticated user navigates to `/nodes/{ClusterId}/{NodeName}`
- **THEN** the page invokes `ClusterNodeService.GetNodeDetailAsync(ClusterId, NodeName)`
- **AND** renders `NodeDetailToolbar` plus the stacked card components

#### Scenario: Node not found
- **WHEN** `GetNodeDetailAsync` returns `null`
- **THEN** the page renders a "未找到该节点" card with a "返回节点列表" button targeting `/nodes/{ClusterId}`

#### Scenario: Cluster unreachable detail
- **WHEN** `GetNodeDetailAsync` returns a `ClusterNodeDetailViewModel` whose `IsReachable == false`
- **THEN** the page renders a "集群不可达" message card and does not render the per-section cards

#### Scenario: Card composition order
- **WHEN** a reachable node is rendered
- **THEN** the cards appear in this order: `NodeOverviewCard`, `NodeSchedulingCard`, `NodeMetadataCard`, `NodeResourcesCard`, then the paired row `NodeAddressesCard` + `NodeConditionsCard`, then the paired row `NodeTaintsCard` + `NodeLabelsCard`, then the paired row `NodeAnnotationsCard` + `NodeSystemInfoCard`

### Requirement: Node detail toolbar

`NodeDetailToolbar` SHALL render a `MudPaper pa-4 mb-4` containing: a "返回节点列表" text button (target `/nodes/{ClusterId}`), the node's `Name` as an `h4` heading, a `MudChip` colored by the node status color helper showing `node.Status`, and a "刷新" outlined button. The toolbar MUST NOT be gated by `AuthorizeView` (Refresh is a read action).

#### Scenario: Refresh is always available
- **WHEN** the page is viewed by any authenticated user (Admin or Member)
- **THEN** the "刷新" button is rendered and enabled (no `AuthorizeView` wrapping)

#### Scenario: Back navigation
- **WHEN** the user clicks "返回节点列表"
- **THEN** the browser navigates to `/nodes/{ClusterId}`

### Requirement: Node detail cards render specific view-model sections

Each shared detail card component under `Components/Nodes/Shared/` SHALL render exactly one section of `ClusterNodeDetailViewModel` and accept that VM (or a child VM) as a `[Parameter]`. The cards MUST replicate the field-level layout of the legacy `NodeDetail.razor` sections, including status-chip coloring, `yyyy-MM-dd HH:mm` date formatting, and ellipsis-on-long-value cells for label/annotation/message strings.

#### Scenario: Overview card
- **WHEN** `NodeOverviewCard` renders
- **THEN** it shows `Name`, `Status` (chip), `Roles`, `KubeletVersion`, `OsImage`, `InternalIP`, using `—` for any empty string field

#### Scenario: Scheduling card
- **WHEN** `NodeSchedulingCard` renders
- **THEN** it shows `Unschedulable` (chip colored Warning when `true`, Success when `false`, with text "不可调度" / "可调度"), `Phase`, and `PodCIDR`

#### Scenario: Conditions card uses the type-aware color helper
- **WHEN** `NodeConditionsCard` renders a condition row whose `Type == "Ready"` and `Status == "True"`
- **THEN** the row's status chip is colored `Color.Success`
- **WHEN** the same row has `Type == "Ready"` and `Status == "False"`
- **THEN** the chip is `Color.Error`
- **WHEN** the row's `Status == "Unknown"`
- **THEN** the chip is `Color.Default`
- **WHEN** the row's `Type` is a non-Ready condition (e.g. `MemoryPressure`) and `Status == "False"`
- **THEN** the chip is `Color.Success` (inverted semantics for non-Ready conditions)

#### Scenario: Resources card shows Capacity and Allocatable side by side
- **WHEN** `NodeResourcesCard` renders
- **THEN** it shows two `MudTable`s, the left listing `Capacity` KV pairs, the right listing `Allocatable` KV pairs, each with header `资源` / `数量`

#### Scenario: Labels and annotations cards ellipsize long values
- **WHEN** `NodeLabelsCard` or `NodeAnnotationsCard` renders a value whose rendered width exceeds its cell
- **THEN** the value cell uses `overflow:hidden; text-overflow:ellipsis; white-space:nowrap` and exposes the full value via the element's `title` attribute

#### Scenario: System info card is full width
- **WHEN** the detail page composes its card grid
- **THEN** `NodeSystemInfoCard` occupies `xs=12` (full width) and renders the 10 fields of `NodeSystemInfoViewModel` in a `MudGrid` of `xs=12 sm=6 md=4` items, using `—` for any empty field

### Requirement: ClusterNodesCard entry point remains intact

`Components/Clusters/Shared/ClusterNodesCard.razor` SHALL continue to render its "查看全部" button navigating to `/nodes/{Cluster.Id}`. The redesign MUST NOT edit this file — the new node list page is the destination the existing button already targets.

#### Scenario: Existing entry-point navigation still works
- **WHEN** the user clicks "查看全部" on the nodes card inside `ClusterDetail`
- **THEN** the browser navigates to `/nodes/{Cluster.Id}` and lands on the new node list page

### Requirement: New shared components use the correct namespaces

All new files under `Components/Nodes/Shared/` and the rewritten `Components/Nodes/Pages/Nodes.razor` / `NodeDetail.razor` SHALL use `@using` directives consistent with the existing `Components/Nodes/**` namespace rule (`MultiClusterMgmtSys.Components.Nodes.*`, matching physical path) and reference `MultiClusterMgmtSys.Components.Nodes.Services` (for `ClusterNodeService`) and `MultiClusterMgmtSys.Components.Clusters.Services` / `...Clusters.ViewModels` (for `ClusterService` and `ClusterNodeViewModel` / `ClusterNodeDetailViewModel`). No file SHALL use the legacy non-existent namespaces `MultiClusterMgmtSys.Services`, `MultiClusterMgmtSys.Models`, or `MultiClusterMgmtSys.ViewModels` that the dead commented pages used.

#### Scenario: No legacy namespaces reintroduced
- **WHEN** any new or rewritten `.razor` file under `Components/Nodes/` is compiled
- **THEN** its `@using` block contains no reference to `MultiClusterMgmtSys.Services`, `MultiClusterMgmtSys.Models`, or `MultiClusterMgmtSys.ViewModels`

#### Scenario: Node feature uses the path-matched namespace root
- **WHEN** a new shared card or toolbar is referenced from a page
- **THEN** the page's `@using` includes `MultiClusterMgmtSys.Components.Nodes.Shared` (or the card is referenced via its full type name)
- **AND** no new file under `Components/Nodes/` declares a namespace rooted at `MultiClusterMgmtSys.Features.*`