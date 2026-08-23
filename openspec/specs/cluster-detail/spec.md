# cluster-detail

## Purpose

Define the contract for the cluster detail page at `/clusters/{id}`: a live Blazor route that composes a top toolbar plus exactly three cards (Overview → Cluster Endpoints → Nodes Preview) from a single eager-loaded `ClusterDetailViewModel`. The page merges identity and connection metadata into one Overview surface, renders all cluster endpoints in a single table with a type column, groups Admin-only actions in an isolated toolbar action group (refresh / disabled edit / confirm-delete), and gates secret reveal behind a lazy Admin-only toggle that reuses the existing `GetClusterForEditAsync` service path.

## Requirements

### Requirement: Cluster detail page is a live route at `/clusters/{id}`

The cluster detail page SHALL be a registered Blazor route at `/clusters/{Id:int}`. Navigating to that route from the cluster list's "view detail" action MUST render the detail page, not fall through to the not-found handler. The existing commented-out `@* ... *@` block at `Components/Clusters/Pages/ClusterDetail.razor` MUST be removed.

#### Scenario: Route is registered

- **WHEN** the application starts and Razor components are compiled
- **THEN** `ClusterDetail.razor` begins with `@attribute [Authorize]` and `@page "/clusters/{Id:int}"` directives (no surrounding `@* ... *@` comment)

#### Scenario: List-row click reaches the detail page

- **WHEN** a user clicks a cluster name row in `/clusters`
- **THEN** the client navigates to `/clusters/{id}` and the detail page's `OnInitializedAsync` loads `ClusterDetailViewModel` via `ClusterService.GetClusterDetailAsync(id)`

### Requirement: Detail page composes three discrete cards in archive layout

The detail page SHALL render a top toolbar followed by exactly three cards in this vertical order: Overview → Cluster Endpoints → Nodes Preview. Each card's content is sourced from the single `ClusterDetailViewModel` eager-loaded by the page's `OnInitializedAsync`. The page MUST NOT split into multiple pages or tabs for the first version. The Overview card merges identity metadata and connection metadata (连接方式, API Server) into one surface; no separate "Connection Info" card exists.

#### Scenario: Top toolbar reflects cluster identity

- **WHEN** the detail page renders with a loaded cluster
- **THEN** the toolbar shows: a back affordance to `/clusters`, the cluster's `Name` as title, a status `MudChip` colored by `GetStatusColor(cluster.Status)` with `cluster.StatusText` as label, and an Admin-only action group

#### Scenario: Overview card surfaces identity and connection fields

- **WHEN** the Overview card renders
- **THEN** it shows: 集群名称, 版本 (k8s `Version`, `—` if null), 节点数 (`NodeCount`), 所属分组 (`GroupName` or "未分组"), 连接方式 (Kubeconfig / Token), API Server (`ApiServer` or `—`), 创建时间 (`CreatedAt` formatted `yyyy-MM-dd HH:mm`), 最后检测时间 (`LastCheckedAt` or `—`)

#### Scenario: Secret reveal is Admin-only

- **WHEN** a user in role `Member` views the detail page
- **THEN** the 显示密文 toggle, the KubeConfig/Token value fields, and any fetched `ClusterEditViewModel` are absent from the rendered DOM — while the non-secret fields (连接方式, API Server) remain visible to all roles

#### Scenario: Nodes preview links to dedicated nodes page

- **WHEN** the Nodes Preview card renders and the cluster is reachable with at least one node
- **THEN** the card shows a compact table of the first few nodes with a "查看全部" link navigating to `/nodes/{Id}` (the existing `Nodes.razor` route)

### Requirement: Cluster Endpoints card renders a single table with a type column

The Cluster Endpoints card SHALL render all of the cluster's endpoints in one `MudTable` — no per-kind group headers — with columns 类型, 地址, 备注, and a one-click copy affordance for `Value`. The 类型 column SHALL be a `MudChip` labeled with `KindText` ("VIP" / "域名"). Rows SHALL be sorted by `Kind` (VIP first, then Domain), then by `SortOrder` ascending. An empty-endpoint cluster shows a placeholder message and, for Admin users, a "管理" button to open the management dialog.

#### Scenario: Rows render in a stable order in a single table

- **WHEN** the Endpoints card renders with at least one `Vip` and one `Domain` row
- **THEN** a single `MudTable` renders both rows (no group headers), VIP rows appear above Domain rows, and within each kind rows are sorted by `SortOrder` ascending

#### Scenario: Empty endpoints state

- **WHEN** the Endpoints card renders for a cluster whose `Endpoints` list is empty
- **THEN** the card body shows the message "未登记任何端点" and, inside `<AuthorizeView Roles="Admin">`, a "管理" button opening `ClusterEndpointsDialog`

#### Scenario: Copy-to-clipboard per row

- **WHEN** an Admin or Member user clicks the copy affordance next to an endpoint row
- **THEN** the row's `Value` is copied to the system clipboard and a snackbar confirms success ("已复制到剪贴板") or reports failure

#### Scenario: Offline cluster still shows endpoints

- **WHEN** a cluster with `Status == Offline` and a non-empty endpoints set is viewed
- **THEN** the Endpoints card renders the rows identically to the Online case (endpoints are app-owned metadata, not k8s-fetched)

### Requirement: Toolbar Admin actions are grouped and isolated

The detail page toolbar SHALL render an Admin-only action group containing: 刷新状态 (refresh), 编辑 (visible but disabled with tooltip "编辑集群功能暂未实现" because `EditClusterDialog` is out of scope for this change), 删除 (with confirmation). Common actions (刷新, 返回列表) MAY be visible to Members in non-destructive form; 删除 is Admin-only.

#### Scenario: Edit button is disabled and explained

- **WHEN** an Admin views the toolbar and the commented-out `EditClusterDialog.razor` has not been resurrected by this change
- **THEN** the 编辑 button renders as `Disabled` with a tooltip indicating that cluster field editing is not yet implemented; clicking it does nothing

#### Scenario: Delete confirmation flow

- **WHEN** an Admin clicks 删除
- **THEN** a confirm messagebox appears with the cluster's name, yesText "删除", noText "取消"; confirmation calls `ClusterService.DeleteClusterAsync`, shows a snackbar, and navigates back to `/clusters`

#### Scenario: Refresh reuses existing service path

- **WHEN** an Admin clicks 刷新状态
- **THEN** `ClusterService.RefreshClusterStatusAsync(Id)` is awaited, the page reloads via `LoadAsync`, and a snackbar reports "集群状态已刷新" on success or the error message on failure

### Requirement: Show-secret toggle reuses `GetClusterForEditAsync` as-is

The Admin-only "show secret" affordance lives in the Overview card's header actions and SHALL reuse the existing `ClusterService.GetClusterForEditAsync(id)` call to surface `KubeConfig` or `Token` in a read-only masked `MudTextField` with a visibility adornment. The page MUST NOT introduce a new service method for secret reading in this change.

#### Scenario: Secret toggle is lazy

- **WHEN** the Admin clicks "显示密文"
- **THEN** `GetClusterForEditAsync(Id)` is called only the first time the toggle is turned on per page session; subsequent toggles reuse the cached `ClusterEditViewModel` and only flip the local `showSecret` / `showSecretContent` flags

#### Scenario: Secret never exposed to Members

- **WHEN** a user in role `Member` views the detail page
- **THEN** the show-secret button, the fetched `ClusterEditViewModel`, and the KubeConfig/Token text fields do not exist on the rendered DOM
