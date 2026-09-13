## MODIFIED Requirements

### Requirement: Node list page route and layout

The system SHALL serve the node list page from a single `Components/Nodes/Pages/Nodes.razor` component that declares two routes: a parameterless `/nodes` route and a parameterized `/nodes/{ClusterId:int}` route. The `ClusterId` route parameter MUST be `int?` so the same component handles both routes. The page SHALL use a two-column layout mirroring the cluster management page (`Clusters.razor`): a fixed-width cluster-selection sidebar on the left (`NodeClusterSidebar`, see the dedicated requirement below) and a flexible content pane on the right. The right pane, when a `ClusterId` is present, displays the live nodes of the selected cluster using the project's current list-page visual vocabulary, identical to the other cluster-scoped list pages (命名空间/服务/配置/工作负载): a top `MudPaper pa-4` containing the title row (page title + single cluster context chip + refresh) and — only when the cluster is reachable — the filter bar; followed by the node table when reachable, or by the "集群不可达，无法获取节点列表" card when not. The page MUST gate node loading on the cluster being reachable (`IsReachable == true`). While the cluster context is being loaded for the first time the pane SHALL show an indeterminate `MudProgressLinear` in place of the toolbar/filter/table region; a refresh MUST keep the toolbar visible and surface progress through the table's loading content (see the node-detail-layout toolbar requirement). When `GetClusterDetailAsync` returns `null` for the effective cluster id, the pane SHALL render a "未找到该集群" card with a "返回集群列表" button navigating to `/clusters`. The parameterless `/nodes` route is what the Drawer's "节点管理" navigation (`Href="/nodes"`) targets; its right pane behavior is defined by the "Cluster-selection empty state" requirement and the cluster-selection memory behavior.

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
- **THEN** `NodeListToolbar` renders the page title "节点管理" as `Typo.h5`, a single status-colored `MudChip` showing `{cluster.Name} · {cluster.StatusText}`, and a "刷新" button
- **AND** the title row renders no separate cluster-name chip, no inline progress spinner, and no back-to-cluster-detail button

#### Scenario: Unreachable cluster shows a message instead of the table
- **WHEN** `ClusterDetailViewModel.IsReachable == false`
- **THEN** the page still renders the toolbar `MudPaper` (title, cluster context chip, refresh) with the filter bar hidden
- **AND** the page renders a "集群不可达，无法获取节点列表" message card below the toolbar in place of the table
- **AND** the page does not invoke `ClusterNodeService.GetClusterNodesAsync`

#### Scenario: Refresh re-fetches both cluster context and node list
- **WHEN** the user clicks the toolbar "刷新" button
- **THEN** the page calls `ClusterService.GetClusterDetailAsync(ClusterId)` and, if reachable, `ClusterNodeService.GetClusterNodesAsync(ClusterId)`, then re-renders
- **AND** the toolbar and filter bar remain rendered while the reload is in flight

#### Scenario: Loading state shows a progress bar
- **WHEN** the right pane begins loading the cluster context for the first time (no cluster context loaded yet)
- **THEN** it renders an indeterminate `MudProgressLinear` until the load completes, replacing the toolbar/filter/table region while in flight

#### Scenario: Cluster not found
- **WHEN** `ClusterService.GetClusterDetailAsync` returns `null` for the effective cluster id
- **THEN** the right pane renders a "未找到该集群" card with a "返回集群列表" button
- **AND** clicking the button navigates to `/clusters`
