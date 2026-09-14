## Context

See `proposal.md` — Why. Design-relevant current state:

- `NodeOverviewCard`（`Components/Nodes/Shared/`）同时渲染身份/调度字段、地址区段（表 + Admin「管理」按钮 + `OpenManageDialog`/`OnRemarksChanged` 参数）与污点区段（仅在污点非空时渲染）；`NodeDetail.razor` 的 基本信息 tab 只放这一张卡。
- `WorkloadStatusCard`（`Components/Workloads/Shared/`）用 `MudSimpleTable` 渲染副本数/就绪/已更新/标签选择器/UID/创建时间，并在同卡内 `@if (Detail.Conditions.Count > 0)` 渲染条件 `MudTable`。`MudSimpleTable` 是全仓库唯一使用点；它渲染 `.mud-simple-table` 根类，不命中 `app.css` 中 `.mud-table-head .mud-table-cell` 等设计词汇，`Class="workload-status-table"` 是死类名（无 CSS 规则）。
- 计数现状：`ClusterDetail.razor`（集群端点/节点）、`SvcDetail.razor`（端口/后端端点）、`ConfigMapDetail.razor`（键值）用 `<MudTabPanel><TabContent>` 携带 `.font-mono` 计数；`NamespaceLabelsCard`/`NamespaceAnnotationsCard` 标题内含计数。测试未断言这些计数。
- 现有卡片词汇：详情页标量字段用 `MudGrid` + 标签（`Typo.body2 mud-text-secondary`）/值（`Typo.body1`，数值用 `.font-mono`）；集合表用 `MudTable Dense Hover Elevation="0"`；空态用 `.empty-state`（`[ 暂无… ]`，见命名空间标签/注解卡）或 `—`（`NodeResourcesCard` 等既有约定）。

## Goals / Non-Goals

**Goals:**

- 节点 基本信息 tab 与工作负载 运行状态 tab 按主题拆卡，卡片边界清晰，拆出的卡为空时仍渲染 `.empty-state` 占位。
- 工作负载状态字段卡使用与 `NodeOverviewCard` 一致的标签/值词汇，消灭全站唯一的 `MudSimpleTable`。
- tab 标签与命名空间卡片标题不再出现计数；导航组标题统一「X管理」句式。
- 改动全部落在展示层；不改服务输入输出契约、ViewModel 字段、数据库 schema、路由与交互流程。

**Non-Goals:**

- 不改 `NodeConditionsCard` 的 `—` 空态、`NodeResourcesCard` 的 `—` 空态与列结构；不为工作负载条件表新增消息截断/tooltip 或时间列（`NodeConditionsCard` 的既有差异留待后续独立清理）。
- 不改其余导航文案（网络管理、账号管理等已符合句式）。
- 不引入新组件库；不清理与本变更无关的历史遗留 spec 条款。

## Decisions

### D1 — 节点基本信息拆为三张卡

- `NodeOverviewCard` 收敛为纯字段卡：移除 地址/污点 区段与对应 `MudText` 小标题，移除 `OnRemarksChanged` 参数与 `OpenManageDialog`/对话框依赖（`IDialogService`/`ISnackbar` 若不再需要则一并移除）。
- 新增 `NodeAddressesCard`：头部「地址」+ Admin-only「管理」按钮（沿用现有 `<AuthorizeView Roles="Admin">` 与按钮渲染条件），`MudTable Dense Hover Elevation="0"` 列 类型/地址/备注，类型用 `StackedText`；空表渲染 `.empty-state`「[ 暂无地址 ]」。接收 `Node` 与 `OnRemarksChanged` 参数，承接备注管理对话框交互。
- 新增 `NodeTaintsCard`：头部「污点」，`MudTable` 列 键/值/效果，效果用 `StackedText`；空表渲染 `.empty-state`「[ 暂无污点 ]」（不再隐藏整卡）。
- `NodeDetail.razor` 基本信息 tab 用 `MudStack Spacing="3"` 顺序放置三卡（与标签与注解 tab 的堆叠方式一致）。
- 组件名复用历史删除名 `NodeAddressesCard`/`NodeTaintsCard`：行为与旧实现同域，spec 已修订为「存在」；备选新名（如 `NodeAddressListCard`）无额外收益且增加命名噪音。

### D2 — 工作负载运行状态拆卡 + 词汇替换

- `WorkloadStatusCard` 只保留状态字段：用 `MudGrid Spacing="3"` + `MudItem xs="12" sm="6" md="4"` 渲染 副本数/就绪/已更新/标签选择器/UID/创建时间，标签 `Typo.body2 mud-text-secondary`、值 `Typo.body1`，数值/UID/选择器/时间戳用 `.font-mono`，空选择器显示 `—`（对齐 `NodeOverviewCard`）。
- 新增 `WorkloadConditionsCard`：头部「条件」，承接条件 `MudTable`（`StackedText` 双语与 Reason/Message 原文行为不变）；条件为空时仍渲染并显示 `.empty-state`「[ 暂无条件 ]」。
- `WorkloadDetailView` 运行状态 tab 用 `MudStack Spacing="3"` 放置两张卡；ViewModel 与服务层零改动。
- `WorkloadDetailView` 双 tab 顺序改为 YAML → 运行状态：YAML 面板在前（`ActivePanelIndex` 默认 0 即 YAML），与 ConfigMap、服务、命名空间详情页一致；备选保持运行状态在前（否——用户要求对齐其他详情页）。
- 备选：字段块保留为 `MudTable`（否——站内无任何详情页用表格排标量字段，仍显异类）。

### D3 — 去除计数

- 三个详情页 5 个 `MudTabPanel` 由 `TabContent`+`ChildContent` 改为 `Text="…"` 单段形式（`ClusterDetail` 2 处、`SvcDetail` 2 处、`ConfigMapDetail` 1 处）。
- `NamespaceLabelsCard`/`NamespaceAnnotationsCard` 标题移除计数 `<span class="font-mono">@Count</span>`。
- 测试已确认未断言任何计数（`rg` 检查过 Tests 目录），无需改断言。

### D4 — 导航改名与测试

- `Drawer.razor` 的 `MudNavGroup Title="工作负载"` 改为 `"工作负载管理"`。
- 参照 `NamespacesPageTests.Drawer_contains_namespaces_link_with_prefix_match` 增加对导航组标题的 bUnit 断言。

### D5 — 测试迁移清单

- `NodeListTableTests.Overview_card_shows_fields_and_schedulable_state`：`内网 IP` 断言迁至新的 `NodeAddressesCard` 测试；概览卡测试保留字段/调度断言。
- `WorkloadStatusCardTests`：`Available` 条件断言迁至 `WorkloadConditionsCard` 测试；两张卡各补空态测试（`[ 暂无地址 ]`/`[ 暂无污点 ]`/`[ 暂无条件 ]`）。
- bUnit 只需断言自有标记（`.empty-state` 文案、`DataLabel` 列、卡片标题），不触碰 `.mud-*` 内部 DOM。

## Risks / Trade-offs

- [旧断言失效] → 按 D5 迁移，测试方法签名保持 `async Task` + `await using var ctx` 既有约定。
- [拆卡后 tab 内间距与层次] → 统一 `MudStack Spacing="3"`，与标签与注解 tab 一致；不引入新 CSS。
- [空态风格两种并存（`.empty-state` 与 `—`）] → 本变更仅统一新拆出的三卡为 `.empty-state`；既有卡不动，避免范围膨胀（记录为 Non-Goal）。
- [`NodeAddressesCard` 空节点与「管理」按钮条件] → 保持现状：仅在地址非空时渲染管理按钮，空态只显示占位。
- [组件名与历史删除名冲突认知] → spec 已修订并写明 `NodeSchedulingCard`/`NodeMetadataCard` 保持删除、地址/污点卡为现行组件；实现前用 `rg` 确认无残余旧引用。
