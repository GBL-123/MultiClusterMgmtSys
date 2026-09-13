# align-node-list-page 任务清单

## 1. 工具栏视觉对齐

- [x] 1.1 改造 `Components/Nodes/Shared/NodeListToolbar.razor`:标题保持 `Typo.h5`「节点管理」;两个分离徽章合并为单个 Filled `MudChip`(`Size.Small`、`Color="GetClusterStatusColor(Cluster.Status)"`、`Class="ml-2"`,文本 `@Cluster.Name · @Cluster.StatusText`);刷新按钮改为 `Variant.Text` + `Color.Inherit` + `StartIcon="Refresh"` + `Disabled="Processing"`,移除内联 `MudProgressCircular`;验证 = `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 1.2 复核 `NodeListToolbar` 不再渲染返回集群详情按钮、分离名称徽章或 spinner,与 `Namespaces.razor` 标题行逐项一致;验证 = 人工对照 `Components/Namespaces/Pages/Namespaces.razor` 第 58-74 行

## 2. 页面分支重组

- [x] 2.1 调整 `Components/Nodes/Pages/Nodes.razor`:把 `!cluster.IsReachable` 分支并入 `else`(集群已加载)分支——`MudPaper pa-4` 内恒定渲染 `NodeListToolbar`,过滤栏包在 `@if (cluster.IsReachable)` 内;`@if (!cluster.IsReachable)` 时在 `MudPaper` 之后渲染「集群不可达,无法获取节点列表」卡片,否则渲染 `NodeListTable`;验证 = `dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 2.2 保持 `cluster is null && loading` 首载进度条与刷新时工具栏常驻(表格 `Loading` 态)行为不变;验证 = 人工检查离线集群页面标题栏可见、在线集群刷新时工具栏不消失

## 3. 测试与回归

- [x] 3.1 在 `MultiClusterMgmtSys.Tests/Components/Pages/NodeConfigMapsPageTests.cs` 的 `NodesPageTests` 补一个离线分支接线断言:离线集群渲染后 `Markup` 同时包含「节点管理」(工具栏)与「集群不可达」;验证 = `dotnet test MultiClusterMgmtSys.Tests --filter-class "*NodesPageTests*"` 全绿
- [x] 3.2 全量验证:`dotnet build MultiClusterMgmtSys.slnx` 0 错误;`dotnet test MultiClusterMgmtSys.Tests` 全绿且测试数不低于当前基线
