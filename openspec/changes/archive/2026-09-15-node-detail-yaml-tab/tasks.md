# node-detail-yaml-tab — Tasks

## 1. 数据链路

- [x] 1.1 `ViewModels/ClusterNodeDetailViewModel.cs` 新增 `string Yaml` 属性(默认空串,中文 XML 注释);`dotnet build MultiClusterMgmtSys.slnx` 0 错误
- [x] 1.2 `Services/ClusterNodeService.cs` 的 `MapNodeDetail` 填充 `Yaml = KubernetesYaml.Serialize(node)`;扩展 `ClusterNodeServiceTests.GetNodeDetailAsync_maps_full_node_view` 断言 YAML 含节点名与 `kind: Node`,该测试通过

## 2. 节点详情页 UI

- [x] 2.1 新增 `Components/Nodes/Shared/NodeYamlViewCard.razor`:照 `NamespaceYamlViewCard` 模式(`yaml-card` 卡 + 只读 `yaml-textarea`、空态 `[ 暂无 YAML ]`),`dotnet build` 通过且组件可被引用
- [x] 2.2 `Components/Nodes/Pages/NodeDetail.razor` 在 tab 区最前插入 `<MudTabPanel Text="YAML">`(首 tab 默认选中契约由既有 `activeTabIndex` 默认值成立),`dotnet build` 通过
- [x] 2.3 更新 `NodeConfigMapsPageTests` 节点详情流程测试:tab 下标整体平移(资源容量起 +1),补断言 YAML 为默认首屏且 `yaml-textarea` 渲染出节点 YAML,测试通过
- [x] 2.4 (回归修正) `NodeDetail.razor` 外层 `MudStack` 对齐其它详情页全高 flex 链(`flex-auto d-flex` + `align-self: stretch; min-height: 0;`),YAML 卡自动撑满页面高度;build 0 错误、全量测试通过

## 3. 整体验证

- [x] 3.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误(AGENTS.md 约定:CS1591 零命中)
- [x] 3.2 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线 607+),无回归
