# 详情页 Tab 化重构(detail-pages-tab-redesign)

## Why

四个详情页模板(ClusterDetail、NodeDetail、WorkloadDetailView、ConfigMapDetail)目前全部是「工具栏 + 纵向卡片堆」结构:NodeDetail 有 6 个区块需要长距离滚动;各页信息区块之间没有层级导航;ConfigMap 详情只能看 YAML,看不到实际的 data 键值内容(数据早已在 ViewModel 中,却无处展示)。全仓库尚未使用过 `MudTabs`,详情页是其首个天然落点。

## What Changes

- 将四个详情页模板从纵向卡片堆改为 `MudTabs` 分页布局,工具栏(返回/标题/状态/操作)保持在 tab 区之外:
  - **ClusterDetail**(`/clusters/{id}`)→ 概览 | 集群端点ⁿ | 节点ⁿ
  - **NodeDetail**(`/nodes/{cid}/{name}`)→ 基本信息 | 资源容量 | 条件 | 标签与注解 | 系统信息(现六区块 1:1 平移;「标签+注解」双列行合并为一个 tab)
  - **WorkloadDetailView**(4 种 Kind 共用)→ 运行状态 | YAML
  - **ConfigMapDetail**(`/configmaps/{cid}/{ns}/{name}`)→ YAML | 键值(**新增**键值视图:表格展示 `Data` 字典,长 value 截断 + 点击弹对话框查看全文)
- Tab 状态为页面内局部状态(方案 A):`ActivePanelIndex` 绑定,不进 URL;刷新后回到第一个 tab。
- 卡片保留为 tab 面板内的内容容器;loading(`MudProgressLinear`)、未找到/不可达空态分支保持在 tab 区之外。
- 有计数语义的 tab 标签带等宽计数(如 `端点 3`、`节点 12`)。
- 为 `MudTabs` 增加贴合 Swiss Industrial Print 的自定义样式(琥珀指示条、tab 栏下沿发丝线、去默认阴影/圆角),补入 app.css 并同步 ui-theme spec。

**不做**:URL 化 tab 路由(方案 B)、tab 懒加载数据、tab 状态持久化。页面数据仍在进入页面时一次性拉取,tab 仅是展示层分组。

## Capabilities

### New Capabilities

- `detail-page-tabs`: 详情页 tab 架构的横切契约——哪些页面采用 tab 布局、tab 区内外的内容归属(工具栏/加载态/空态在 tab 外)、局部 `ActivePanelIndex` 状态模型、tab 标签计数约定、键值长文本截断 + 展开对话框交互。

### Modified Capabilities

- `cluster-detail`: 「三卡片纵列布局」 requirement 改为「工具栏 + 三 tab(概览 | 集群端点 | 节点)」;移除 "MUST NOT split into tabs" 限制;概览/端点/节点预览各 tab 的内容契约不变。
- `node-detail-layout`: 「五卡片纵列布局」 requirement 改为「工具栏 + 五 tab(基本信息 | 资源容量 | 条件 | 标签与注解 | 系统信息)」;各卡片的内容契约不变,「标签+注解」由双列行改为单 tab 内并排。
- `configmaps-page`: 「ConfigMap 详情页为只读 YAML」 requirement 改为「工具栏 + 双 tab(YAML | 键值)」;移除 "SHALL NOT render MudTabs" 限制;新增键值只读表格与长 value 展开对话框契约。
- `workload-management`: 「工作负载详情页」 requirement 中「YAML 视图与信息卡片」的布局改为「工具栏 + 双 tab(运行状态 | YAML)」;工具栏动作与可用性矩阵不变。
- `ui-theme`: 新增 `MudTabs` 视觉样式 requirement(琥珀指示条、发丝线、无阴影圆角、密集 tab 高度),纳入组件优先策略的设计词汇。

## Impact

- **代码**(仅展示层,无服务层/数据层变更):
  - `Components/Clusters/Pages/ClusterDetail.razor`、`Components/Nodes/Pages/NodeDetail.razor`、`Components/Configmaps/Pages/ConfigMapDetail.razor`、`Components/Workloads/Shared/WorkloadDetailView.razor` 的标记区重组(`@code` 逻辑基本不动)。
  - 新增 ConfigMap 键值表格组件(或内联标记)+ 长值查看对话框;`ConfigMapDetailViewModel.Data` 已存在,无需改服务。
  - `wwwroot/app.css` 增加 MudTabs 样式段。
- **Specs**:5 个既有 spec 出 delta,新增 1 个横切 spec。
- **测试**:详情页主体目前无 bUnit 测试,`ClusterDetailToolbarTests` 不受影响(工具栏不动);按 unit-testing 约定可新增 tab 接线契约测试。
- **风险**:MudBlazor 9.9 `MudTabs` API 误用由 MUD0002 分析器在编译期拦截;YAML 卡片的 `yaml-textarea` 全高布局在 tab 内的适配是主要样式回归点。
