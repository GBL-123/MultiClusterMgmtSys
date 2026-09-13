## MODIFIED Requirements

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

### Requirement: Node overview card consolidates scheduling, metadata, addresses, and taints

`NodeOverviewCard` (基本信息) SHALL render: the node's `Name`, a `ui-theme` status badge (`.status-badge` with the standard node-status color helper), `Roles`, `KubeletVersion`, `OsImage`, the `Unschedulable` chip (Warning/"不可调度" when true, Success/"可调度" when false), `Phase`, `PodCIDR`, and `CreatedAt` formatted `yyyy-MM-dd HH:mm`, using `—` for any empty string field. Per `display-conventions`, the status, roles, phase, address types and taint effects SHALL be rendered Chinese-primary with the English raw value as a secondary mono line, and the `Phase` field label SHALL read 阶段 (Phase). It SHALL NOT render `Uid`. The card SHALL contain an 地址 section listing every address from `ClusterNodeDetailViewModel.Addresses` (type + address, plus the stored remark when present); the card SHALL contain a 污点 section rendered ONLY when the node has taints (键 / 值 / 效果), and SHALL be omitted entirely when taints are empty.

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

#### Scenario: Overview values render bilingual
- **WHEN** a node reports status `Ready`, role `control-plane`, phase `Running`, address type `InternalIP`, and taint effect `NoSchedule`
- **THEN** the status badge shows 就绪 with `Ready` as a secondary mono line
- **AND** the roles show 控制平面 with `control-plane` as a secondary mono line
- **AND** the phase shows 运行中 with `Running` as a secondary mono line
- **AND** the address type shows 内网 IP with `InternalIP` as a secondary mono line
- **AND** the taint effect shows 禁止调度 with `NoSchedule` as a secondary mono line

## ADDED Requirements

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
