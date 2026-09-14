## 1. 导航改名与去计数

- [x] 1.1 `Components/Layout/Drawer.razor` 将 `MudNavGroup Title="工作负载"` 改为「工作负载管理」;在 `NamespacesPageTests` 或等价 Drawer 测试中补充标题断言(参照 `Drawer_contains_namespaces_link_with_prefix_match`),验证 `dotnet test` 通过
- [x] 1.2 `ClusterDetail.razor` 两处 `MudTabPanel` 由 `TabContent`+`ChildContent` 改为 `Text="集群端点"`/`Text="节点"`;验证 `dotnet test` 中 ClusterDetail 相关测试通过,且 `rg "集群端点&nbsp;|节点&nbsp;" MultiClusterMgmtSys` 无命中
- [x] 1.3 `SvcDetail.razor` 两处、`ConfigMapDetail.razor` 一处同样去计数;验证 `rg "端口&nbsp;|后端端点|键值&nbsp;" MultiClusterMgmtSys` 无命中且 `dotnet test` 通过
- [x] 1.4 `NamespaceLabelsCard.razor`/`NamespaceAnnotationsCard.razor` 标题移除 `<span class="font-mono">@Count</span>`;验证 `dotnet test` 通过且渲染测试断言标题为「标签」「注解」纯文本

## 2. 节点基本信息拆卡

- [x] 2.1 新建 `Components/Nodes/Shared/NodeAddressesCard.razor`:地址 `MudTable Dense Hover Elevation="0"`(类型 `StackedText`/地址/备注)+ Admin 管理按钮 + `OnRemarksChanged` 参数 + 空态「[ 暂无地址 ]」;新增 bUnit 测试覆盖三行地址渲染、备注文本与空态,验证测试通过
- [x] 2.2 新建 `Components/Nodes/Shared/NodeTaintsCard.razor`:污点 `MudTable`(键/值/效果,效果 `StackedText`)+ 空态「[ 暂无污点 ]」(始终渲染);新增 bUnit 测试覆盖有/无污点两分支,验证测试通过
- [x] 2.3 `NodeOverviewCard.razor` 移除地址/污点区段及相关 `MudText` 小标题、`OnRemarksChanged` 参数与不再使用的注入;概览卡渲染测试调整后通过(字段、`不可调度`、`阶段 (Phase)`、`运行中`、`控制平面` 仍在)
- [x] 2.4 `NodeDetail.razor` 基本信息 tab 改为 `MudStack Spacing="3"` 顺序放置 `NodeOverviewCard`→`NodeAddressesCard`→`NodeTaintsCard`,并把 `OnRemarksChanged="LoadAsync"` 传给地址卡;验证页面级 bUnit 测试渲染三卡顺序
- [x] 2.5 测试迁移:将 `NodeListTableTests.Overview_card_shows_fields_and_schedulable_state` 中的 `内网 IP` 断言移入 2.1 的地址卡测试;确认无测试仍从概览卡查找地址/污点,`dotnet test` 全绿

## 3. 工作负载运行状态拆卡与词汇统一

- [x] 3.1 `WorkloadStatusCard.razor` 字段块由 `MudSimpleTable` 改为 `MudGrid Spacing="3"` 标签/值布局(数值/UID/选择器/时间戳 `.font-mono`),移除死类名 `workload-status-table`;bUnit 断言字段文本仍在且卡片内无 `MudSimpleTable` 组件实例
- [x] 3.2 新建 `Components/Workloads/Shared/WorkloadConditionsCard.razor`:条件 `MudTable Dense Hover Elevation="0"`(`StackedText` 双语、Reason/Message 原文)+ 空态「[ 暂无条件 ]」;bUnit 覆盖条件行双语与空态两分支
- [x] 3.3 `WorkloadDetailView.razor` 运行状态 tab 用 `MudStack Spacing="3"` 放置 `WorkloadStatusCard` + `WorkloadConditionsCard`;页面测试断言两卡均渲染、状态卡在条件卡之前
- [x] 3.4 `WorkloadDetailView.razor` 将 YAML tab 移到运行状态之前并默认选中 YAML;页面测试断言初始渲染 YAML 卡、切到运行状态后渲染双卡,且默认面板索引为 0 指向 YAML
- [x] 3.5 `WorkloadTableTests.WorkloadStatusCardTests` 的 `Available` 条件断言迁至 3.2 的条件卡测试;`dotnet test` 全绿

## 4. 集成验证

- [x] 4.1 运行 `dotnet build MultiClusterMgmtSys.slnx`(0 错误)+ `dotnet test MultiClusterMgmtSys.Tests`(全绿,含新增测试)并记录新基线数量
- [x] 4.2 运行 `openspec validate detail-ui-consistency --strict` 通过;抽查实现与 5 份 delta spec 的条款一致(tab 无计数、导航标题、三卡/两卡组成、空态文案)
