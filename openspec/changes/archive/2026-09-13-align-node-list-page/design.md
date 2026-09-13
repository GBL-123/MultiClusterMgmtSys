# align-node-list-page — Design

## Context

`Nodes.razor` 的右栏分支顺序与其它集群作用域列表页不同:集群不可达时直接渲染「集群不可达」卡片,跳过了工具栏 `MudPaper`;而其工具栏 `NodeListToolbar` 又使用两个分离徽章(名称 Outlined + 状态 Filled)与带内联 spinner 的 Outlined 刷新按钮,而 `Namespaces.razor`/`Svcs.razor`/`ConfigMaps.razor`/`WorkloadListView.razor` 统一为「h5 标题 + 单个 Filled `名称 · 状态` 徽章 + Text/Inherit 刷新」。参见 proposal.md 了解动机,参见 specs/ 下两个 delta 了解行为契约。

## Goals / Non-Goals

**Goals:**

- 节点列表页右栏在集群已加载的任何状态下都与其它列表页同构(工具栏恒在,过滤栏仅可达时,不可达卡片在工具栏下方)。
- `NodeListToolbar` 的标题、徽章、刷新按钮样式与其它列表页完全一致。
- 保留节点页既有的「刷新时工具栏保持可见、表格显示加载态」行为。

**Non-Goals:**

- 不改变节点数据获取方式、侧栏、表格列、过滤逻辑或详情页。
- 不引入 Admin 操作按钮(节点不可新建)。
- 不改动其它页面以反向「统一」。

## Decisions

### 复用现有 `NodeListToolbar` 组件,只改其内部视觉

页面结构已拆分为 `Nodes.razor` + `NodeListToolbar.razor` + `NodeListFilterBar.razor`,与其它页内联工具栏的做法不同但视觉可按同一契约渲染。备选:把工具栏内联进 `Nodes.razor` 使其逐字复制其它页面(否决——组件已存在且参数稳定,内联只会增加页面体积)。

### 不可达卡片移到工具栏 `MudPaper` 之后

`Nodes.razor` 的 `!cluster.IsReachable` 分支并入 `else`(cluster 已加载)分支内部,与其它页面的 `@if (!cluster.IsReachable) { card } else { table }` 完全同构。这样离线时标题/徽章/刷新仍然可见,用户知道当前上下文。

### 保持刷新语义不变

其它列表页刷新时 `loading == true` 命中 `else if (loading)` 整栏替换为进度条,而节点页用 `cluster is null && loading` 使刷新时保留工具栏、由表格显示「// 正在加载...」。`node-detail-layout` 已把这个差异写成明确契约(「Refresh keeps the toolbar visible」),且节点页刷新按钮带反馈的必要性更高,故本次不改——这是本次唯一一个「与其他页面刻意不同」的保留项。

## Risks / Trade-offs

- [视觉回归:改动工具栏后可能与 bUnit 现有断言不匹配] → 现有节点页测试只断言「节点管理」文本与数据渲染;实施时跑全量测试,必要时补「不可达时工具栏仍渲染」的接线断言。
- [两个 spec 仍互相冲突导致后续漂移] → 本次 delta 同时修订 `nodes-page` 与 `node-detail-layout`,归档后两者关于节点列表页头的表述一致。
