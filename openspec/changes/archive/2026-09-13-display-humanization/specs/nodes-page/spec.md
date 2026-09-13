## MODIFIED Requirements

### Requirement: Node list table columns and row interaction

`NodeListTable` SHALL render a `MudTable<ClusterNodeViewModel>` with `Dense`, `Hover`, a client-paging `MudTablePager`, and exactly six columns in this left-to-right order: 名称, 状态, 角色, Kubelet 版本, 操作系统, IP 地址. The 名称 cell SHALL be a clickable underline-styled `MudText` that navigates to `/nodes/{ClusterId}/{NodeName}`. The 状态 cell SHALL render a `ui-theme` status badge (`.status-badge`) with the standard node-status color helper (`Ready` → online, `NotReady` → offline, otherwise unknown), displaying 就绪 / 未就绪 / 未知 as the primary line and the English raw value (`Ready` / `NotReady` / `Unknown`) as a secondary mono line. The 角色 cell SHALL follow `display-conventions`, displaying Chinese-primary roles (e.g. 控制平面 / 工作节点) with the raw value as a secondary mono line.

#### Scenario: Empty state copy
- **WHEN** the filtered row set is empty
- **THEN** the table's `NoRecordsContent` renders "暂无节点数据" or "没有符合当前筛选条件的节点" (the latter when at least one filter is active)

#### Scenario: Row name click navigates to detail
- **WHEN** the user clicks a row's 名称 cell
- **THEN** the browser navigates to `/nodes/{ClusterId}/{NodeName}` for that row

#### Scenario: Pager format matches cluster table
- **WHEN** the pager renders
- **THEN** it uses the same `RowsPerPageString` / `InfoFormat` ("共 {all_items} 条") convention as `ClusterTable.razor`

#### Scenario: Status and roles render bilingual
- **WHEN** a row renders a node with status `Ready` and role `control-plane`
- **THEN** the 状态 cell shows 就绪 with `Ready` on a secondary mono line
- **AND** the 角色 cell shows 控制平面 with `control-plane` on a secondary mono line

### Requirement: Node list filter bar with four filters
The list page SHALL render `NodeListFilterBar` containing exactly four filter controls bound to a single `NodeListFilter` draft held by the page: a free-text Name field (with a search adornment), a Role drop-down, a Status drop-down, and a Schedulability drop-down, followed by a "查询" filled primary button that invokes `OnQuery` and a "重置" outlined button that clears the filter state and invokes `OnReset`. The bar SHALL match the other cluster-scoped list pages' filter bars in structure (spacer pushed query/reset buttons) and SHALL NOT add extra top margin. Editing a control alone SHALL NOT change the table; filtering SHALL be applied only when the user clicks "查询". Filtering MUST be performed client-side against the loaded `List<ClusterNodeViewModel>` — no new server round-trip is introduced when a filter is applied. The Role and Status drop-down option labels SHALL follow `display-conventions` (e.g. 控制平面 (control-plane), 就绪 (Ready)) while the underlying filter values remain the raw Kubernetes strings.

#### Scenario: Query applies the drafted filters
- **WHEN** the user edits the Name/Role/Status/Schedulability controls and clicks "查询"
- **THEN** the table shows only nodes matching the drafted conditions

#### Scenario: Editing without query leaves the table unchanged
- **WHEN** the user edits one or more filter controls but does not click "查询"
- **THEN** the table continues to show the previously applied result set

#### Scenario: Reset clears the filter and restores the full list
- **WHEN** the user clicks "重置"
- **THEN** all four controls return to their default values and the table shows all loaded nodes

#### Scenario: Name filter matches by substring (case-insensitive)
- **WHEN** the applied Name filter is a non-empty string
- **THEN** the table shows only nodes whose `Name` contains that value (ordinalIgnoreCase)

#### Scenario: Role filter narrows by label-derived role
- **WHEN** the applied Role filter is a non-null string (e.g. `"control-plane"`, `"worker"`)
- **THEN** the table shows only nodes whose `Roles` field (a comma-joined list of `node-role.kubernetes.io/<role>` labels) contains that value as one of its comma-separated segments

#### Scenario: Status filter narrows by Ready condition
- **WHEN** the applied Status filter is `"Ready"` / `"NotReady"` / `"Unknown"`
- **THEN** the table shows only nodes whose `ClusterNodeViewModel.Status` equals that exact string

#### Scenario: Schedulability filter narrows by Unschedulable flag
- **WHEN** the applied Schedulable filter is `true`
- **THEN** the table shows only nodes whose `Unschedulable` is `true` (i.e. cordoned / not schedulable)
- **WHEN** the applied Schedulable filter is `false`
- **THEN** the table shows only nodes whose `Unschedulable` is `false` (i.e. schedulable)
- **WHEN** the applied Schedulable filter is `null`
- **THEN** the table applies no schedulability filter

#### Scenario: All filters compose
- **WHEN** multiple filters are applied simultaneously
- **THEN** the table shows the intersection of all active filters

#### Scenario: Filter option labels are bilingual
- **WHEN** the user opens the Role drop-down
- **THEN** it offers 全部 / 控制平面 (control-plane) / 工作节点 (worker)
- **AND** selecting 控制平面 (control-plane) sets the filter value to the raw string `control-plane`
- **WHEN** the user opens the Status drop-down
- **THEN** it offers 全部 / 就绪 (Ready) / 未就绪 (NotReady) / 未知 (Unknown)
