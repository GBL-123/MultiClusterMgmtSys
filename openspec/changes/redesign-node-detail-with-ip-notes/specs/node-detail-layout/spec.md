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
