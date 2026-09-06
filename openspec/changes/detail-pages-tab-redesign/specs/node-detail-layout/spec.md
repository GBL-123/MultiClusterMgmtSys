# node-detail-layout delta

## REMOVED Requirements

### Requirement: Node detail page five-card composition

**Reason**: 详情页布局统一改为「工具栏 + MudTabs 分页」,纵向五卡片组成契约被 tab 组成契约取代;「标签+注解」由双列行改为单 tab 内并排展示。

**Migration**: 由下方 ADDED 的 "Node detail page five-tab composition" requirement 取代;各卡片(NodeOverviewCard / NodeResourcesCard / NodeConditionsCard / NodeLabelsCard / NodeAnnotationsCard / NodeSystemInfoCard)的内容契约保持不变,只是挂载位置从页面 MudStack 移入对应 tab 面板。

## ADDED Requirements

### Requirement: Node detail page five-tab composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by a `MudTabs` area with exactly five tabs in this order: 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息. The 基本信息 tab contains `NodeOverviewCard` (full width); the 资源容量 tab contains `NodeResourcesCard`; the 条件 tab contains `NodeConditionsCard`; the 标签与注解 tab contains `NodeLabelsCard` and `NodeAnnotationsCard` side by side (each `xs=12 md=6` within the tab panel); the 系统信息 tab contains `NodeSystemInfoCard`. The former standalone `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, and `NodeTaintsCard` components SHALL remain deleted. Tab selection is page-local state per the `detail-page-tabs` capability; the 基本信息 tab SHALL be selected by default.

#### Scenario: Tab composition order

- **WHEN** a reachable node is rendered
- **THEN** the tabs appear in this order: 基本信息, 资源容量, 条件, 标签与注解, 系统信息, with 基本信息 selected by default

#### Scenario: Cards that lost their standalone component

- **WHEN** the detail page renders
- **THEN** no `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, or `NodeTaintsCard` component is instantiated anywhere

#### Scenario: Labels and annotations share one tab

- **WHEN** the 标签与注解 tab renders
- **THEN** `NodeLabelsCard` and `NodeAnnotationsCard` appear side by side, each occupying `xs=12 md=6` inside the tab panel

#### Scenario: Overview card content unchanged inside its tab

- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard` keeps its existing contract: identity/scheduling fields, the 地址 section (with stored remarks), and the 污点 section rendered only when taints exist
