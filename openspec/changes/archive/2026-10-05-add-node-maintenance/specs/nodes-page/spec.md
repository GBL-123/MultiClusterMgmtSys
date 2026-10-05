# Spec Delta

## MODIFIED Requirements

### Requirement: Node list filter bar with four filters
The list page SHALL render `NodeListFilterBar` containing exactly four filter controls bound to a single `NodeListFilter` draft held by the page: a free-text Name field (with a search adornment), a Role drop-down, a Status drop-down, and a Schedulability drop-down, followed by a "查询" filled primary button that invokes `OnQuery` and a "重置" outlined button that clears the filter state and invokes `OnReset`. The bar SHALL match the other cluster-scoped list pages' filter bars in structure (spacer pushed query/reset buttons) and SHALL NOT add extra top margin. Editing a control alone SHALL NOT change the table; filtering SHALL be applied only when the user clicks "查询". Filtering MUST be performed client-side against the loaded `List<ClusterNodeViewModel>` — no new server round-trip is introduced when a filter is applied. The Role and Status drop-down option labels SHALL follow `display-conventions` (e.g. 控制平面 (control-plane), 就绪 (Ready)) while the underlying filter values remain the raw Kubernetes strings.
节点行可视表达 SHALL 增补维护语义:对 `Unschedulable=true` 的节点行 SHALL 追加「已封锁」小徽标(逐行按 `Unschedulable` 投影条件渲染),状态列的三态徽章语义保持不变。

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

#### Scenario: 已封锁行增补徽标
- **WHEN** 节点 `Unschedulable=true` 且出现在列表
- **THEN** 该行增补「已封锁」小徽标,状态列三态徽章语义保持不变

### Requirement: Node list table columns and row interaction
`NodeListTable` SHALL render a `MudTable<ClusterNodeViewModel>` with `Dense`, `Hover`, a client-paging `MudTablePager`, and exactly six columns in this left-to-right order: 名称, 状态, 角色, Kubelet 版本, 操作系统, IP 地址. The 名称 cell SHALL be a clickable underline-styled `MudText` that navigates to `/nodes/{ClusterId}/{NodeName}`. The 状态 cell SHALL render a `ui-theme` status badge (`.status-badge`) with the standard node-status color helper (`Ready` → online, `NotReady` → offline, otherwise unknown), displaying 就绪 / 未就绪 / 未知 as the primary line and the English raw value (`Ready` / `NotReady` / `Unknown`) as a secondary mono line. The 角色 cell SHALL follow `display-conventions`, displaying Chinese-primary roles (e.g. 控制平面 / 工作节点) with the raw value as a secondary mono line.
Admin 用户 SHALL 额外在行尾看到节点维护操作入口(封锁/解封/排空,按节点 `Unschedulable` 状态条件渲染,见 `node-maintenance` 契约);Member 用户 SHALL NOT 看到维护操作入口。名称相邻位置对 `Unschedulable=true` 的行 SHALL 渲染「已封锁」小徽标。

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

#### Scenario: Admin 看到维护入口
- **WHEN** Admin 查看节点列表的某一行
- **THEN** 行尾按节点封锁状态渲染对应维护操作入口(未封锁:封锁+排空;已封锁:解封+排空)

#### Scenario: Member 无维护入口
- **WHEN** Member 查看任意节点行
- **THEN** 不渲染任何维护操作入口,行结构与既有六列一致

### Requirement: Node detail toolbar
`NodeDetailToolbar` SHALL render a `MudPaper pa-4 mb-4` containing: a "返回节点列表" text button (target `/nodes/{ClusterId}`), the node's `Name` as an `h4` heading, a `MudChip` colored by the node status color helper showing `node.Status`, and a "刷新" outlined button (disabled while `Processing`, with a small inline progress spinner while processing). The toolbar MUST NOT be gated by `AuthorizeView` (Refresh is a read action).
工具栏 SHALL 保留既有元素并按 `node-maintenance` 契约以 Admin 条件渲染节点维护操作入口(封锁/解封/排空);对已封锁节点在名称相邻位置 SHALL 渲染「已封锁」小徽标。维护操作入口的渲染不出现在 Member 视图中。

#### Scenario: Refresh is always available
- **WHEN** the page is viewed by any authenticated user (Admin or Member)
- **THEN** the "刷新" button is rendered and enabled (no `AuthorizeView` wrapping)

#### Scenario: Back navigation
- **WHEN** the user clicks "返回节点列表"
- **THEN** the browser navigates to `/nodes/{ClusterId}`

#### Scenario: 详情工具栏维护入口
- **WHEN** Admin 打开未封锁节点详情
- **THEN** 工具栏含封锁与排空入口,名称旁无「已封锁」徽标

#### Scenario: 已封锁节点详情
- **WHEN** Admin 打开 `Unschedulable=true` 节点详情
- **THEN** 工具栏含解封与排空入口,名称旁渲染「已封锁」徽标
