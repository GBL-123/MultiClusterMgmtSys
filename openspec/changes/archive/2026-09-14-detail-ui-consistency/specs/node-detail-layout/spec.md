## MODIFIED Requirements

### Requirement: Node detail page five-tab composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by a `MudTabs` area with exactly five tabs in this order: 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息. The 基本信息 tab contains `NodeOverviewCard`, `NodeAddressesCard` and `NodeTaintsCard` stacked vertically, each full width within the tab panel; the 资源容量 tab contains `NodeResourcesCard`; the 条件 tab contains `NodeConditionsCard`; the 标签与注解 tab contains `NodeLabelsCard` and `NodeAnnotationsCard` stacked vertically, each full width within the tab panel; the 系统信息 tab contains `NodeSystemInfoCard`. The former standalone `NodeSchedulingCard` and `NodeMetadataCard` components SHALL remain deleted. Tab selection is page-local state per the `detail-page-tabs` capability; the 基本信息 tab SHALL be selected by default.

#### Scenario: Tab composition order
- **WHEN** a reachable node is rendered
- **THEN** the tabs appear in this order: 基本信息, 资源容量, 条件, 标签与注解, 系统信息, with 基本信息 selected by default

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

## REMOVED Requirements

### Requirement: Node overview card consolidates scheduling, metadata, addresses, and taints
**Reason**: 按主题拆分卡片——地址与污点内容量大且彼此独立,继续塞在概览卡内导致卡片边界不清;改为 `NodeAddressesCard` 与 `NodeTaintsCard` 独立卡片。
**Migration**: `NodeOverviewCard` 移除 地址/污点 区段,只保留身份/调度/元数据字段;地址表(含每行备注)迁至 `NodeAddressesCard`,污点表迁至 `NodeTaintsCard`;地址备注管理入口随地址区段迁入 `NodeAddressesCard`(引用见 `node-ip-notes`)。

## ADDED Requirements

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
