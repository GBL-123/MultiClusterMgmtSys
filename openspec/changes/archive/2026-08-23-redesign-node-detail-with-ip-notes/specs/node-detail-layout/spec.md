## ADDED Requirements

### Requirement: Node detail page five-card composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by exactly five focused MudCards in this vertical order: `NodeOverviewCard` (基本信息, full width), `NodeResourcesCard` (资源容量, full width), `NodeConditionsCard` (条件, full width), the paired row `NodeLabelsCard` + `NodeAnnotationsCard` (each `xs=12 md=6`), and `NodeSystemInfoCard` (系统信息, full width). The former standalone `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, and `NodeTaintsCard` components SHALL be deleted — their content is absorbed into `NodeOverviewCard` per the requirements below. The page MUST NOT render the previous 10-card composition.

#### Scenario: Card composition order
- **WHEN** a reachable node is rendered
- **THEN** the cards appear in this order: `NodeOverviewCard`, `NodeResourcesCard`, `NodeConditionsCard`, then the paired row `NodeLabelsCard` + `NodeAnnotationsCard`, then `NodeSystemInfoCard`

#### Scenario: Cards that lost their standalone component
- **WHEN** the detail page renders
- **THEN** no `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, or `NodeTaintsCard` component is instantiated anywhere

#### Scenario: Full-width cards
- **WHEN** `NodeConditionsCard` or `NodeSystemInfoCard` renders
- **THEN** each occupies `xs=12` as a direct child of the outer `MudStack` (no longer `md=6`-paired)

### Requirement: Node overview card consolidates scheduling, metadata, addresses, and taints

`NodeOverviewCard` (基本信息) SHALL render: the node's `Name`, a status `MudChip` (standard node-status color helper), `Roles`, `KubeletVersion`, `OsImage`, the `Unschedulable` chip (Warning/"不可调度" when true, Success/"可调度" when false), `Phase`, `PodCIDR`, and `CreatedAt` formatted `yyyy-MM-dd HH:mm`, using `—` for any empty string field. It SHALL NOT render `Uid`. The card SHALL contain an 地址 section listing every address from `ClusterNodeDetailViewModel.Addresses` (type + address, plus the stored remark when present); the card SHALL contain a 污点 section rendered ONLY when the node has taints (键 / 值 / 效果), and SHALL be omitted entirely when taints are empty.

#### Scenario: Uid no longer displayed
- **WHEN** `NodeOverviewCard` renders
- **THEN** it contains no `Uid` field

#### Scenario: Empty taints hide the section
- **WHEN** `node.Spec.Taints` is null or empty
- **THEN** `NodeOverviewCard` renders no 污点 section at all

#### Scenario: Taints present
- **WHEN** the node has at least one taint
- **THEN** the card renders a 污点 section listing 键 / 值 / 效果 for each taint

#### Scenario: All addresses render in the address section
- **WHEN** the node's `Status.Addresses` contains two `InternalIP` entries and one `ExternalIP` entry
- **THEN** the card's 地址 section lists all three rows (type + address)
- **AND** each row that has a stored remark displays the remark text

#### Scenario: Scheduling and metadata fields merged into the card
- **WHEN** `NodeOverviewCard` renders
- **THEN** it displays `Unschedulable`, `Phase`, `PodCIDR`, and `CreatedAt` alongside the pre-existing overview fields

### Requirement: Node list surfaces render multiple IPs with remarks

`ClusterNodeViewModel` SHALL replace the single `InternalIP` string with a `List<NodeIpViewModel>` (`Address` + `Note`). Both `NodeListTable.razor` (nodes list page) and `ClusterNodesCard.razor` (cluster detail page) SHALL render this list under a column header "IP 地址": each IP on its own row with the note shown as secondary text beside it; when the list is empty the cell SHALL render `—`. `ClusterNodesCard` SHALL retain its existing `Take(5)` truncation and "查看全部" button.

#### Scenario: Multi-IP node renders all IPs
- **WHEN** a node has `IpAddresses` with three entries (two with notes)
- **THEN** the "IP 地址" cell renders three stacked lines: `10.0.0.5 管理口`, `172.16.8.2 数据口`, and `203.0.113.10` (no note)
- **AND** the same rendering appears in both `NodeListTable.razor` and `ClusterNodesCard.razor`

#### Scenario: Column header renamed
- **WHEN** either node list surface renders its header row
- **THEN** the IP column header reads "IP 地址" (not "内网 IP")

#### Scenario: Empty address list
- **WHEN** a node has no addresses
- **THEN** the "IP 地址" cell renders `—`

#### Scenario: Cluster detail card truncation preserved
- **WHEN** `ClusterNodesCard` renders more than five nodes
- **THEN** it shows only the first five nodes with the existing "查看全部" button targeting `/nodes/{Cluster.Id}`

### Requirement: Node list page toolbar mirrors the cluster management page

The node list page's right pane SHALL follow the visual structure of `Clusters.razor`: a single `MudPaper pa-4` containing a title row and the filter bar in one card, followed by the table. The title row (`NodeListToolbar`) SHALL render: the page title "节点管理" as `Typo.h5`, the cluster name as an outlined `MudChip`, the cluster's `StatusText` as a status-colored `MudChip`, and a "刷新" outlined button (disabled while `Processing`, with a small progress spinner). The title row SHALL NOT contain a back-to-cluster-detail button (navigation back to the cluster detail is via the Drawer/sidebar, not this toolbar). The filter bar SHALL render inside the same `MudPaper` below the title row (no standalone `pa-4 mb-4` paper of its own).

#### Scenario: Title row and filter share one paper
- **WHEN** the node list renders for a reachable cluster
- **THEN** `NodeListToolbar` and `NodeListFilterBar` appear inside the same `MudPaper pa-4`
- **AND** the page title reads "节点管理" at `Typo.h5` with the cluster context shown as chips
- **AND** the title row contains no back-to-cluster-detail button

#### Scenario: Refresh keeps the toolbar visible
- **WHEN** the user clicks "刷新" while the node list is loaded
- **THEN** the toolbar and filter bar remain rendered and the table shows its loading state ("正在加载...") until the reload completes

### Requirement: Node list table uses sortable client-side columns

`NodeListTable` SHALL render a `MudTable<ClusterNodeViewModel>` with `Hover`, `FixedHeader`, a `Loading` parameter driving a "正在加载..." `LoadingContent`, and `MudTableSortLabel` headers for all six columns (名称 / 状态 / 角色 / Kubelet 版本 / 操作系统 / IP 地址, the last sorting by the first IP address). Sorting SHALL be client-side over the loaded `Items` — no new server round-trip.

#### Scenario: Column headers are sortable
- **WHEN** the user clicks a column header (e.g. 状态)
- **THEN** the table re-sorts its rows client-side by that column, toggling ascending/descending

#### Scenario: Loading content shows during refresh
- **WHEN** `Loading` is `true`
- **THEN** the table renders "正在加载..." in place of the rows
