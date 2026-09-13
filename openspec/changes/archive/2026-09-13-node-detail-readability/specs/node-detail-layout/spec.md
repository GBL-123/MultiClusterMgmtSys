## MODIFIED Requirements

### Requirement: Node detail page five-tab composition

The system SHALL render the node detail page (`/nodes/{ClusterId}/{NodeName}`) as a `NodeDetailToolbar` `MudPaper` followed by a `MudTabs` area with exactly five tabs in this order: 基本信息 → 资源容量 → 条件 → 标签与注解 → 系统信息. The 基本信息 tab contains `NodeOverviewCard` (full width); the 资源容量 tab contains `NodeResourcesCard`; the 条件 tab contains `NodeConditionsCard`; the 标签与注解 tab contains `NodeLabelsCard` and `NodeAnnotationsCard` stacked vertically, each full width within the tab panel; the 系统信息 tab contains `NodeSystemInfoCard`. The former standalone `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, and `NodeTaintsCard` components SHALL remain deleted. Tab selection is page-local state per the `detail-page-tabs` capability; the 基本信息 tab SHALL be selected by default.

#### Scenario: Tab composition order
- **WHEN** a reachable node is rendered
- **THEN** the tabs appear in this order: 基本信息, 资源容量, 条件, 标签与注解, 系统信息, with 基本信息 selected by default

#### Scenario: Cards that lost their standalone component
- **WHEN** the detail page renders
- **THEN** no `NodeSchedulingCard`, `NodeMetadataCard`, `NodeAddressesCard`, or `NodeTaintsCard` component is instantiated anywhere

#### Scenario: Labels and annotations share one tab
- **WHEN** the 标签与注解 tab renders
- **THEN** `NodeLabelsCard` and `NodeAnnotationsCard` appear stacked vertically
- **AND** each card occupies the full width of the tab panel (no `md=6` side-by-side pairing)

#### Scenario: Overview card content unchanged inside its tab
- **WHEN** the 基本信息 tab renders
- **THEN** `NodeOverviewCard` keeps its existing contract: identity/scheduling fields, the 地址 section (with stored remarks), and the 污点 section rendered only when taints exist

## ADDED Requirements

### Requirement: Node resources render human-readable capacity and allocatable values

The 资源容量 tab's `NodeResourcesCard` SHALL render a single unified table with one row per resource key present in the node's capacity or allocatable data, using columns 资源 / 容量 / 可分配 / 可分配占比 (no separate side-by-side Capacity and Allocatable tables). Quantity values SHALL be converted from Kubernetes quantity strings into human-readable form: CPU quantities rendered in cores with up to three decimals (`3800m` → `3.8 核`, `4` → `4 核`), byte-denominated quantities (memory, ephemeral-storage, hugepages) rendered in IEC binary units with up to one decimal (`16297496Ki` → `15.5 GiB`, `8Gi` → `8 GiB`), pod counts rendered as `110 个`. The original Kubernetes quantity string SHALL remain available as each value's `title` attribute. Known resource keys SHALL show a Chinese label — `cpu` → CPU, `memory` → 内存, `ephemeral-storage` → 临时存储, `pods` → Pod, `hugepages-*` → 大页 — with the raw key shown as secondary mono text; unknown keys SHALL render the raw key as the label and the original quantity string as the value (no unit conversion). The 可分配占比 column SHALL render the allocatable share of capacity as a progress bar with a percentage; when the capacity value is missing, unparsable, or zero, the column SHALL render `—`. Rows SHALL be ordered with the known resources first (cpu, memory, ephemeral-storage, pods, hugepages) followed by unknown keys. When the node reports neither capacity nor allocatable entries, the card SHALL render its `—` empty state.

#### Scenario: CPU and memory values are humanized

- **WHEN** a node reports capacity `cpu=4`, `memory=16297496Ki` and allocatable `cpu=3800m`, `memory=15942336Ki`
- **THEN** the CPU row shows `4 核` capacity and `3.8 核` allocatable
- **AND** the memory row shows `15.5 GiB` capacity and `15.2 GiB` allocatable
- **AND** the raw `3800m` / `16297496Ki` strings are present as the values' `title` attributes

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
