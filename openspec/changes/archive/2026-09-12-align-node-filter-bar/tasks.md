## 1. 过滤栏组件对齐

- [x] 1.1 改造 `Components/Nodes/Shared/NodeListFilterBar.razor`:名称框加 `Adornment="Adornment.Start"` + `AdornmentIcon="@Icons.Material.Filled.Search"`;根 `MudStack` 去掉 `Class="mt-2"`;按钮区改为 `MudSpacer` +「查询」(Filled Primary,Search 图标,`OnClick="Search"`)+「重置」(Outlined Secondary,Refresh 图标);回调 `OnFilterChanged` 改名 `OnQuery`;移除 `@bind-Value:after`;验证 = `dotnet build` 0 错误
- [x] 1.2 调整 `Components/Nodes/Pages/Nodes.razor`:新增 `appliedFilter` 字段,`filteredNodes` 与 `NodeListTable.FilterActive` 改读 `appliedFilter`;`ApplyFilter()` 拷贝草稿字段;`ResetFilter()` 重置草稿与已应用实例;过滤栏接线 `OnQuery="ApplyFilter"`;验证 = 手动检查节点页输入后表格不变、点「查询」过滤、点「重置」恢复

## 2. 测试

- [x] 2.1 更新 `MultiClusterMgmtSys.Tests/Components/Nodes/NodeListTableTests.cs` 的 `NodeListFilterBarTests`:重置回调断言改为新签名,新增「查询按钮触发 `OnQuery`」接线断言;验证 = `dotnet test MultiClusterMgmtSys.Tests --filter-class "*NodeListFilterBarTests*"` 全绿
- [x] 2.2 在 `MultiClusterMgmtSys.Tests/Components/Pages/PageFilterFlowTests.cs` 新增 `Nodes_page_query_filters_and_reset_restores`:输入名称后未点「查询」时两节点仍在、点「查询」后仅剩匹配节点、点「重置」后恢复;验证 = 该测试通过

## 3. 回归

- [x] 3.1 运行 `dotnet build MultiClusterMgmtSys.slnx` 与 `dotnet test MultiClusterMgmtSys.Tests`,确认 0 错误且测试数量基线(485 + 新增)全绿
