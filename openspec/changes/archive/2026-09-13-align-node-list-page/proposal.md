# align-node-list-page

## Why

节点管理列表页与其它集群作用域列表页(命名空间/服务/配置/工作负载)显示不统一:集群离线时整块页头(MudPaper + 标题 + 集群徽章 + 刷新)消失,只剩一条「集群不可达」卡片;在线时 `NodeListToolbar` 也用两个分离的小徽章(名称 Outlined + 状态 Filled)、Outlined 刷新按钮与其它页面的「h5 标题 + 单个 `名称 · 状态` Filled 徽章 + Text 刷新」不一致。用户跨页面切换时观感与位置感被打断。

## What Changes

- `Nodes.razor` 右栏在集群已加载(无论是否可达)时恒定渲染 `MudPaper pa-4` 工具栏;过滤栏仅在可达时渲染,可达时接表格,不可达时在工具栏下方渲染「集群不可达,无法获取节点列表」卡片——与命名空间/服务/配置/工作负载页完全同构。
- `NodeListToolbar` 对齐其它列表页工具栏:标题 `节点管理`(Typo.h5)+ 单个 Filled 集群徽章 `{Name} · {StatusText}`(按集群状态着色)+ `MudSpacer` + 「刷新」按钮(Text/Inherit,`Processing` 时禁用);移除分离的名称/状态徽章与 Outlined 刷新按钮的内联 spinner。
- 保留节点页既有刷新语义(刷新时工具栏保持可见、表格显示加载态)——与其它页面「整栏替换为进度条」刻意不同,本次不改。
- 同步修订 `nodes-page` 与 `node-detail-layout` 两个 spec 中互相冲突/已过时的节点列表页头契约。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `nodes-page`:节点列表页右栏结构契约调整——工具栏在集群不可达时也必须渲染,不可达卡片位于工具栏下方;删除已过时的「返回集群详情按钮 + h4 标题」表述,页面头以 `node-detail-layout` 的工具栏契约为准。
- `node-detail-layout`:节点列表页工具栏契约由「Outlined 名称徽章 + 状态徽章 + Outlined 刷新(带 spinner)」改为与其它集群作用域列表页一致的「h5 标题 + 单个 Filled `名称 · 状态` 徽章 + Text 刷新」,并明确不可达时工具栏仍渲染。

## Impact

- **修改**:`Components/Nodes/Pages/Nodes.razor`(右栏分支重组)、`Components/Nodes/Shared/NodeListToolbar.razor`(工具栏视觉与徽章结构)。
- **规格**:`openspec/specs/nodes-page/spec.md`、`openspec/specs/node-detail-layout/spec.md`。
- **测试**:节点页现有 bUnit 测试(`NodeConfigMapsPageTests.NodesPageTests`)断言仍应通过;必要时补充工具栏在不可达状态渲染的接线断言。
- 不涉及服务层、数据库 schema、NuGet 依赖或其它页面。
