# node-detail-yaml-tab

## Why

节点详情页是唯一没有 YAML 视图的 K8s 对象详情页:命名空间、配置、服务、工作负载详情页都以只读「YAML」tab 作为首 tab 默认展示,节点页却以「基本信息」开头,排障时无法直接查看 Node 对象原文(taints、conditions、labels 等的原始形态)。这是既有详情页 YAML 展示契约的缺口,不是新功能方向。

## What Changes

- 节点详情页新增只读「YAML」tab,置于 tab 列表首位并默认选中(与配置、服务、命名空间、工作负载详情页的 YAML 首 tab 约定一致)
- 新增 `NodeYamlViewCard` 只读 YAML 卡片(沿用既有 `*YamlViewCard` 模式:`yaml-card` + `yaml-textarea` 只读、空态 `[ 暂无 YAML ]`)
- `ClusterNodeDetailViewModel` 新增 `Yaml` 字段,`ClusterNodeService.GetNodeDetailAsync` 映射时以 `KubernetesYaml.Serialize(node)` 填充
- 同步更新受影响的既有测试(节点详情页 tab 下标整体后移)与 `node-detail-layout` spec(五 tab → 六 tab)

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `node-detail-layout`:「Node detail page five-tab composition」改为节点详情页 tab 组成契约(YAML 置首、默认选中、只读 YAML 卡内容约定)

## Impact

- 主项目:`Components/Nodes/Pages/NodeDetail.razor`、新增 `Components/Nodes/Shared/NodeYamlViewCard.razor`、`ViewModels/ClusterNodeDetailViewModel.cs`、`Services/ClusterNodeService.cs`
- 测试:`MultiClusterMgmtSys.Tests/Services/ClusterNodeServiceTests.cs`(详情映射断言)、`MultiClusterMgmtSys.Tests/Components/Pages/NodeConfigMapsPageTests.cs`(tab 下标平移、YAML 首屏断言)
- 无数据库、无 K8s API 调用变化、无新依赖
