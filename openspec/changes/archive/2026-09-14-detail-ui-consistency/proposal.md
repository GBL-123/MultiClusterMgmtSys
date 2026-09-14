## Why

详情页与侧边导航经多轮迭代（详情页 tab 化、`display-humanization`、`node-detail-layout` 卡片合并）后留下四处不统一：

1. 侧边导航「工作负载」组是唯一没有「管理」后缀的入口（同级为集群管理、节点管理、命名空间管理、配置管理，网络管理组亦然）。
2. 工作负载详情「运行状态」tab 的字段块是全站唯一使用 `MudSimpleTable` 的地方，不命中 `app.css` 中面向 `.mud-table-*` 的设计词汇，与其他详情页的标签/值观感明显不同。
3. 详情页 tab 标签与命名空间标签/注解卡标题携带计数（如「键值 1」），是被 `detail-page-tabs` 契约引入的视觉噪音，计数本身可从内容获得。
4. 节点「基本信息」与工作负载「运行状态」把多个主题塞进单卡（字段 + 地址 + 污点、字段 + 条件），卡片边界不清晰。

## What Changes

- **导航改名**：Drawer 中「工作负载」导航组标题改为「工作负载管理」。
- **去掉全部总数数字（BREAKING：可见文案变化）**：移除所有 tab 标签计数（集群详情的集群端点/节点、服务详情的端口/后端端点、ConfigMap 详情的键值）与命名空间详情标签/注解卡标题计数。
- **拆分多主题卡片**：节点「基本信息」tab 拆为 `NodeOverviewCard`（字段）+ `NodeAddressesCard`（地址，含 Admin 管理入口）+ `NodeTaintsCard`（污点，独立卡）；工作负载「运行状态」tab 拆为 `WorkloadStatusCard`（状态字段）+ `WorkloadConditionsCard`（条件表）。
- **空态不隐藏**：拆分出的地址/污点/条件卡在数据为空时仍渲染卡片，显示 `.empty-state` 占位（`[ 暂无地址 ]` / `[ 暂无污点 ]` / `[ 暂无条件 ]`，参照命名空间标签/注解卡）。
- **字段展示统一**：工作负载状态字段卡改用全站详情页统一的标签/值展示词汇（对齐 `NodeOverviewCard` 的 `MudGrid` + 标签/值样式），不再使用 `MudSimpleTable`。
- **工作负载 tab 顺序对齐**：工作负载详情双 tab 顺序改为 YAML → 运行状态并默认选中 YAML，与 ConfigMap、服务、命名空间详情页一致。
- 不新增服务方法、ViewModel 字段、NuGet 依赖；不改数据库 schema、路由与交互流程。

## Capabilities

### New Capabilities

（无）

### Modified Capabilities

- `workload-management`：导航组标题改为「工作负载管理」；详情双 tab 顺序改为 YAML → 运行状态并默认选中 YAML；「运行状态」tab 由单卡改为双卡组成，状态字段卡使用统一详情页标签/值词汇，条件卡空态占位。
- `detail-page-tabs`：反转「Tab 标签携带计数」需求为「tab 标签 SHALL NOT 携带计数」。
- `namespace-management`：标签/注解卡去计数措辞。
- `node-detail-layout`：基本信息 tab 卡片组成由单卡改为「概览 + 地址 + 污点」三卡；`NodeOverviewCard` 不再承载地址/污点区段；新增地址/污点卡契约与空态；修订「前独立组件保持删除」条款。
- `node-ip-notes`：地址备注管理入口由「基本信息卡的地址区段」改为「地址卡」。

## Impact

- **组件修改**：`Components/Layout/Drawer.razor`、`Components/Clusters/Pages/ClusterDetail.razor`、`Components/Svcs/Pages/SvcDetail.razor`、`Components/Configmaps/Pages/ConfigMapDetail.razor`、`Components/Nodes/Pages/NodeDetail.razor`、`Components/Nodes/Shared/NodeOverviewCard.razor`、`Components/Workloads/Shared/WorkloadDetailView.razor`、`Components/Workloads/Shared/WorkloadStatusCard.razor`、`Components/Namespaces/Shared/NamespaceLabelsCard.razor`、`Components/Namespaces/Shared/NamespaceAnnotationsCard.razor`。
- **组件新增**：`Components/Nodes/Shared/NodeAddressesCard.razor`、`Components/Nodes/Shared/NodeTaintsCard.razor`、`Components/Workloads/Shared/WorkloadConditionsCard.razor`。
- **测试**：`NodeOverviewCard` 的地址/污点断言迁移至新卡（`NodeListTableTests`）；`WorkloadStatusCardTests` 的条件断言迁至 `WorkloadConditionsCard`；补 Drawer 导航标题断言；现有测试均未断言 tab 计数，去计数不改测试断言。
- **规格**：`workload-management`、`detail-page-tabs`、`namespace-management`、`node-detail-layout`、`node-ip-notes`。
