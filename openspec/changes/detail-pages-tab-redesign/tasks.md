# tasks — 详情页 Tab 化重构

## 1. 共享样式与骨架验证

- [x] 1.1 在 `wwwroot/app.css` 新增 `.detail-tabs` 样式段:tab 栏卡面底色 + 下沿发丝线、琥珀指示条、激活墨色/未激活次文字、去面板圆角投影、密集 tab 高度、面板容器 flex 传递规则(按 ui-theme delta 的 token 取值)
- [x] 1.2 在 ClusterDetail.razor 上小步验证 MudBlazor 9.9 的 `MudTabs`/`MudTabPanel` API:`ActivePanelIndex` 绑定、默认选中第一 tab、计数标签渲染方式(header 模板或 `Text` 回退),确认 MUD0002 无告警

## 2. ClusterDetail → 概览 | 集群端点 | 节点

- [x] 2.1 将 ClusterDetail.razor 的三卡片纵列改为 `<MudTabs Class="detail-tabs">` 三面板:概览(`ClusterOverviewCard`)、集群端点(`ClusterEndpointsCard`)、节点(`ClusterNodesCard`);工具栏、loading、未找到分支保持在 tab 区之外;端点/节点 tab 标签带等宽计数
- [x] 2.2 补 ClusterDetail 的 bUnit 接线契约测试(默认选中第一个 tab、面板数为 3、工具栏在 tab 外,不碰 `.mud-*` 内部 DOM)

## 3. NodeDetail → 基本信息 | 资源容量 | 条件 | 标签与注解 | 系统信息

- [x] 3.1 将 NodeDetail.razor 的五区块改为五 tab 面板:基本信息(`NodeOverviewCard`)、资源容量(`NodeResourcesCard`)、条件(`NodeConditionsCard`)、标签与注解(`NodeLabelsCard` + `NodeAnnotationsCard` 在面板内并排 md=6)、系统信息(`NodeSystemInfoCard`);工具栏/loading/未找到/集群不可达分支保持在 tab 外
- [x] 3.2 补 NodeDetail 的 bUnit 接线契约测试(面板数 5、默认第一 tab、无标签注解独立双列行)

## 4. WorkloadDetailView → 运行状态 | YAML

- [x] 4.1 将 WorkloadDetailView.razor 的状态卡 + YAML 卡改为双 tab(运行状态 `WorkloadStatusCard`、YAML `WorkloadYamlViewCard`);确认 `yaml-textarea` 全高 flex 链在 tab 面板内的表现,必要时用 `.detail-tabs` 面板规则修复
- [x] 4.2 补 WorkloadDetailView 的 bUnit 接线契约测试(默认「运行状态」tab、工具栏动作按可用性矩阵仍在 tab 外)

## 5. ConfigMapDetail → YAML | 键值

- [x] 5.1 新增 `Configmaps/Shared/ConfigMapDataViewCard.razor`:`Data` 字典键值只读 `MudTable`(键列 `.font-mono`)、value 单行 CSS 省略截断、点击行查看 → `MudDialog` 等宽全文 + 复制到剪贴板(复用 Snackbar 模式)、空 Data 显示 `.empty-state`
- [x] 5.2 将 ConfigMapDetail.razor 改为双 tab:YAML(`ConfigMapYamlViewCard` 原样)、键值(`ConfigMapDataViewCard`);工具栏/loading/未找到分支保持在 tab 外,YAML tab 默认选中
- [ ] 5.3 补 ConfigMapDataViewCard / ConfigMapDetail 的 bUnit 接线契约测试(键值行数与 Data 键数一致、长值截断类存在、空态分支、点击查看打开对话框)

## 6. 验证与收尾

- [x] 6.1 按各页 spec scenario 走查四页(静态走查 + bUnit 覆盖:tab 顺序/默认选中/计数/空态/角色差异;发现并修复 键值 tab 缺计数、`.mud-tab.mud-tab-active` 类名、`display:contents` 面板、CSS 加载顺序四点;浏览器视觉复核建议归档前人工做一次)
- [x] 6.2 `dotnet build MultiClusterMgmtSys.slnx` 0 错误 + `dotnet test MultiClusterMgmtSys.Tests` 全绿(129/129,含本变更新增 11 个用例)
