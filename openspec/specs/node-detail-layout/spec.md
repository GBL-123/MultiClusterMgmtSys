# node-detail-layout

## Purpose

Define the redesigned node detail page composition (`/nodes/{ClusterId}/{NodeName}`): a focused five-tab layout (`NodeDetailToolbar` + `MudTabs`, tabs 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息) whose 基本信息 tab splits identity/scheduling fields, addresses (with stored remarks), and taints into the `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` cards, plus the matching node list surfaces: a toolbar mirroring the cluster management page, a sortable client-side six-column table, and multi-IP rendering with remarks.

## Requirements

### Requirement: Node detail page tab composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by a `MudTabs` area with exactly six tabs in this order: YAML → 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息. The YAML tab SHALL be selected by default and SHALL contain the read-only `NodeYamlViewCard` (`yaml-card` with a readonly `yaml-textarea`) rendering the node's serialized YAML, with no edit affordance; when the YAML text is empty the card SHALL show the `.empty-state` placeholder `[ 暂无 YAML ]`. The 基本信息 tab contains `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` stacked vertically, each full width within the tab panel; the 资源容量 tab contains `NodeResourcesCard`; the 条件 tab contains `NodeConditionsCard`; the 标签与注解 tab contains `NodeLabelsCard` and `NodeAnnotationsCard` stacked vertically, each full width within the tab panel; the 系统信息 tab contains `NodeSystemInfoCard`. The former standalone `NodeSchedulingCard` and `NodeMetadataCard` components SHALL remain deleted. Tab selection is page-local state per the `detail-page-tabs` capability.

#### Scenario: Tab composition order
- **WHEN** a reachable node is rendered
- **THEN** the tabs appear in this order: YAML, 基本信息, 资源容量, 条件, 标签与注解, 系统信息, with YAML selected by default

#### Scenario: YAML tab renders the serialized node read-only
- **WHEN** the YAML tab renders for a loaded node
- **THEN** it shows the node's serialized YAML in a readonly `yaml-textarea` inside the `yaml-card`
- **AND** no edit or save affordance is rendered

#### Scenario: Cards that lost their standalone component
- **WHEN** the detail page renders
- **THEN** no `NodeSchedulingCard` or `NodeMetadataCard` component is instantiated anywhere

#### Scenario: 基本信息 tab card composition
- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` appear stacked vertically in that order
- **AND** each card occupies the full width of the tab panel

#### Scenario: Labels and annotations share one tab
- **WHEN** the 标签与注解 tab renders
- **THEN** `NodeLabelsCard` and `NodeAnnotationsCard` appear stacked vertically
- **AND** each card occupies the full width of the tab panel (no `md=6` side-by-side pairing)

#### Scenario: Overview card content unchanged inside its tab
- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard` keeps its identity/scheduling/metadata field contract (no `Uid`, `—` for empty fields)
- **AND** the 地址 and 污点 sections move to `NodeAddressesCard` and `NodeTaintsCard`, rendered below it in the same tab

### Requirement: Node resources render human-readable capacity and allocatable values

The 资源容量 tab's `NodeResourcesCard` SHALL render a single unified table with one row per resource key present in the node's capacity or allocatable data, using columns 资源 / 容量 / 可分配 / 可分配占比 (no separate side-by-side Capacity and Allocatable tables). Quantity values SHALL be converted from Kubernetes quantity strings into human-readable form: CPU quantities rendered in cores with up to three decimals (`3800m` → `3.8 核`, `4` → `4 核`), byte-denominated quantities (memory, ephemeral-storage, hugepages) rendered in IEC binary units with up to one decimal (`16297496Ki` → `15.5 GiB`, `8Gi` → `8 GiB`), pod counts rendered as `110 个`. The original Kubernetes quantity string SHALL remain available through the unified tooltip shown on hover (per `display-conventions` and `ui-theme`); native `title` attributes SHALL NOT be used for this. Known resource keys SHALL show a Chinese label — `cpu` → CPU, `memory` → 内存, `ephemeral-storage` → 临时存储, `pods` → Pod, `hugepages-*` → 大页 — with the raw key shown as secondary mono text; unknown keys SHALL render the raw key as the label and the original quantity string as the value (no unit conversion). The 可分配占比 column SHALL render the allocatable share of capacity as a progress bar with a percentage; when the capacity value is missing, unparsable, or zero, the column SHALL render `—`. Rows SHALL be ordered with the known resources first (cpu, memory, ephemeral-storage, pods, hugepages) followed by unknown keys. When the node reports neither capacity nor allocatable entries, the card SHALL render its `—` empty state.

#### Scenario: CPU and memory values are humanized

- **WHEN** a node reports capacity `cpu=4`, `memory=16297496Ki` and allocatable `cpu=3800m`, `memory=15942336Ki`
- **THEN** the CPU row shows `4 核` capacity and `3.8 核` allocatable
- **AND** the memory row shows `15.5 GiB` capacity and `15.2 GiB` allocatable
- **AND** hovering each value shows the raw `3800m` / `16297496Ki` string in the unified tooltip

#### Scenario: Unified rows include allocatable-only resources

- **WHEN** a resource key exists only in `allocatable` (e.g. `nvidia.com/gpu`)
- **THEN** it still renders as a row, with the 容量 cell showing `—`

#### Scenario: Allocatable share renders as progress

- **WHEN** a row has a parseable, non-zero capacity and a parseable allocatable value
- **THEN** its 可分配占比 cell renders a progress bar and the percentage of allocatable over capacity (e.g. `3800m` over `4` → `95%`)

#### Scenario: No resources reports empty state

- **WHEN** the node reports no capacity and no allocatable entries
- **THEN** the card shows its `—` empty state instead of a table

#### Scenario: Unknown resource keys fall back to raw names

- **WHEN** the node reports a resource key without a known Chinese label (e.g. `example.com/fpga=2`)
- **THEN** the 资源 cell shows `example.com/fpga`
- **AND** the 容量 cell shows the raw `2`

### Requirement: Node overview card renders identity, scheduling, and metadata fields

`NodeOverviewCard` (基本信息) SHALL render the node's `Name`, a `ui-theme` status badge (`.status-badge` with the standard node-status color helper), `Roles`, `KubeletVersion`, `OsImage`, the `Unschedulable` chip (Warning/"不可调度" when true, Success/"可调度" when false), `Phase`, `PodCIDR`, and `CreatedAt` formatted `yyyy-MM-dd HH:mm`, using `—` for any empty string field. Per `display-conventions`, the status, roles and phase SHALL be rendered Chinese-primary with the English raw value as a secondary mono line, and the `Phase` field label SHALL read 阶段 (Phase). It SHALL NOT render `Uid`. It SHALL NOT contain an 地址 or 污点 section.

#### Scenario: Uid no longer displayed
- **WHEN** `NodeOverviewCard` renders
- **THEN** it contains no `Uid` field

#### Scenario: Scheduling and metadata fields render
- **WHEN** `NodeOverviewCard` renders
- **THEN** it displays `Unschedulable`, `Phase`, `PodCIDR`, and `CreatedAt` alongside the identity fields

#### Scenario: Overview values render bilingual
- **WHEN** a node reports status `Ready`, role `control-plane`, and phase `Running`
- **THEN** the status badge shows 就绪 with `Ready` as a secondary mono line
- **AND** the roles show 控制平面 with `control-plane` as a secondary mono line
- **AND** the phase shows 运行中 with `Running` as a secondary mono line

### Requirement: Node addresses card lists addresses with stored remarks

The 基本信息 tab's `NodeAddressesCard` SHALL render every row from `ClusterNodeDetailViewModel.Addresses` in a table with columns 类型 / 地址 / 备注 (type + address, plus the stored remark when present). Per `display-conventions`, the address type SHALL be rendered Chinese-primary with the English raw value as a secondary mono line (`InternalIP` → 内网 IP). The card SHALL always render: when the address list is empty it SHALL show the `.empty-state` placeholder `[ 暂无地址 ]` instead of the table. The Admin-only remark manage affordance lives in this card per `node-ip-notes`.

#### Scenario: All addresses render in the addresses card
- **WHEN** the node's `Status.Addresses` contains two `InternalIP` entries and one `ExternalIP` entry
- **THEN** the card lists all three rows (type + address)
- **AND** each row that has a stored remark displays the remark text

#### Scenario: Empty addresses show empty state
- **WHEN** the node has no addresses
- **THEN** the card still renders and shows the `[ 暂无地址 ]` empty state instead of a table

#### Scenario: Address type renders bilingual
- **WHEN** an address row has type `InternalIP`
- **THEN** the 类型 cell shows 内网 IP with `InternalIP` as a secondary mono line

### Requirement: Node taints card lists taints

The 基本信息 tab's `NodeTaintsCard` SHALL render one row per taint with columns 键 / 值 / 效果. Per `display-conventions`, the taint effect SHALL be rendered Chinese-primary with the English raw value as a secondary mono line (`NoSchedule` → 禁止调度). The card SHALL always render: when the node has no taints it SHALL show the `.empty-state` placeholder `[ 暂无污点 ]` instead of the table (it SHALL NOT be omitted).

#### Scenario: Taints present
- **WHEN** the node has at least one taint
- **THEN** the card renders one row per taint listing 键 / 值 / 效果

#### Scenario: Empty taints show empty state
- **WHEN** `node.Spec.Taints` is null or empty
- **THEN** the card still renders and shows the `[ 暂无污点 ]` empty state instead of a table

#### Scenario: Taint effect renders bilingual
- **WHEN** a taint has effect `NoSchedule`
- **THEN** the 效果 cell shows 禁止调度 with `NoSchedule` as a secondary mono line

### Requirement: Node conditions and system info render bilingual

Per `display-conventions`, `NodeConditionsCard` SHALL render the 类型 and 状态 cells Chinese-primary with the English raw value as a secondary mono line (`Ready` → 就绪 / `Ready`; `True` → 成立 / `True`; `Unknown` → 未知 / `Unknown`); the condition Reason and Message SHALL remain raw English. `NodeSystemInfoCard` SHALL render its ten field labels as 中文 (English): 架构 (Architecture), 启动 ID (BootID), 容器运行时版本 (ContainerRuntimeVersion), 内核版本 (KernelVersion), KubeProxy 版本 (KubeProxyVersion), Kubelet 版本 (KubeletVersion), 机器 ID (MachineID), 操作系统 (OperatingSystem), 操作系统镜像 (OsImage), 系统 UUID (SystemUUID); the values SHALL remain unchanged.

#### Scenario: Condition type and status render bilingual
- **WHEN** a node condition row has `Type = Ready` and `Status = True`
- **THEN** the 类型 cell shows 就绪 with `Ready` as a secondary mono line
- **AND** the 状态 cell shows 成立 with `True` as a secondary mono line
- **AND** the badge color still follows the type-aware color helper

#### Scenario: Non-Ready condition type
- **WHEN** a condition row has `Type = MemoryPressure`
- **THEN** the 类型 cell shows 内存压力 with `MemoryPressure` as a secondary mono line

#### Scenario: Unknown condition type falls back
- **WHEN** a condition row has an unregistered type such as `CustomCondition`
- **THEN** the 类型 cell shows `CustomCondition` as the primary text with no fabricated Chinese

#### Scenario: System info labels are bilingual
- **WHEN** `NodeSystemInfoCard` renders
- **THEN** the ten field labels read 架构 (Architecture) / 启动 ID (BootID) / 容器运行时版本 (ContainerRuntimeVersion) / 内核版本 (KernelVersion) / KubeProxy 版本 (KubeProxyVersion) / Kubelet 版本 (KubeletVersion) / 机器 ID (MachineID) / 操作系统 (OperatingSystem) / 操作系统镜像 (OsImage) / 系统 UUID (SystemUUID)

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

The node list page's right pane SHALL follow the visual structure of the other cluster-scoped list pages (`Namespaces.razor`, `Svcs.razor`, `ConfigMaps.razor`, `WorkloadListView.razor`): a single `MudPaper pa-4` containing a title row and — only when the cluster is reachable — the filter bar in one card, followed by the table (reachable) or the "集群不可达，无法获取节点列表" card (unreachable). The title row (`NodeListToolbar`) SHALL render: the page title "节点管理" as `Typo.h5`, a single status-colored `MudChip` (`Size.Small`, `Variant.Filled`) showing `{cluster.Name} · {cluster.StatusText}`, a spacer, and a "刷新" `MudButton` (`Variant.Text`, `Color.Inherit`, disabled while `Processing`). The title row MUST NOT render a separate cluster-name chip, an inline progress spinner inside the refresh button, or a back-to-cluster-detail button (navigation back to the cluster detail is via the Drawer/sidebar, not this toolbar). The title row SHALL be rendered whenever the cluster context has loaded, including when `IsReachable == false`; the filter bar SHALL render inside the same `MudPaper` below the title row only when the cluster is reachable; the unreachable message card SHALL render below the `MudPaper`.

#### Scenario: Title row and filter share one paper
- **WHEN** the node list renders for a reachable cluster
- **THEN** `NodeListToolbar` and `NodeListFilterBar` appear inside the same `MudPaper pa-4`
- **AND** the page title reads "节点管理" at `Typo.h5` with the cluster context shown as a single `{Name} · {StatusText}` filled chip
- **AND** the title row contains no back-to-cluster-detail button

#### Scenario: Refresh keeps the toolbar visible
- **WHEN** the user clicks "刷新" while the node list is loaded
- **THEN** the toolbar and filter bar remain rendered and the table shows its loading state ("正在加载...") until the reload completes

#### Scenario: Unreachable cluster keeps the toolbar visible
- **WHEN** the selected cluster's `IsReachable` is `false`
- **THEN** `NodeListToolbar` still renders the title, the cluster context chip, and the refresh button inside the `MudPaper`
- **AND** `NodeListFilterBar` is not rendered
- **AND** the "集群不可达，无法获取节点列表" card renders below the `MudPaper` in place of the table

### Requirement: Node list table uses sortable client-side columns

`NodeListTable` SHALL render a `MudTable<ClusterNodeViewModel>` with `Hover`, `FixedHeader`, a `Loading` parameter driving a "正在加载..." `LoadingContent`, and `MudTableSortLabel` headers for all six columns (名称 / 状态 / 角色 / Kubelet 版本 / 操作系统 / IP 地址, the last sorting by the first IP address). Sorting SHALL be client-side over the loaded `Items` — no new server round-trip.

#### Scenario: Column headers are sortable
- **WHEN** the user clicks a column header (e.g. 状态)
- **THEN** the table re-sorts its rows client-side by that column, toggling ascending/descending

#### Scenario: Loading content shows during refresh
- **WHEN** `Loading` is `true`
- **THEN** the table renders "正在加载..." in place of the rows
