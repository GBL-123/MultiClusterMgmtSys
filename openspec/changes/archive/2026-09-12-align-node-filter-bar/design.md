# align-node-filter-bar — Design

## Context

节点列表数据由 `Nodes.razor` 一次性加载到内存(`ClusterNodeService.GetClusterNodesAsync`),当前 `filteredNodes` 直接读取与控件双向绑定的 `NodeListFilter`,因此输入即过滤。其他列表页(服务/工作负载/ConfigMap/命名空间/集群/账号/审计)的过滤栏均为「编辑草稿 → 点查询应用 → 点重置恢复」模式。参见 proposal.md 了解动机,参见 specs/nodes-page/spec.md 了解行为契约。

## Goals / Non-Goals

**Goals:**

- `NodeListFilterBar` 的结构、按钮样式与其它列表页过滤栏一致。
- 过滤只在点击「查询」后应用;「重置」清空并恢复全量。
- 保持纯客户端过滤、节点数据一次性加载不变。

**Non-Goals:**

- 不引入服务端分页/查询,不改变 `ClusterNodeService` 或数据获取方式。
- 不改动侧栏、表格、详情页或其它页面。

## Decisions

### 草稿与已应用过滤分离

页面持有两个 `NodeListFilter` 实例:与控件绑定的 `filter`(草稿)和 `filteredNodes` / `FilterActive` 读取的 `appliedFilter`。`OnQuery` 时把草稿字段拷贝到已应用实例,`OnReset` 同时重置两者。

备选:继续即时过滤,仅加一个装饰性的「查询」按钮(否决——按钮无实际语义,与其他页面行为仍不一致);为 `NodeListFilter` 增加 `Clone()`(否决——拷贝字段只有四行,不必为此改 `Models/` 公共契约)。

### 回调命名对齐

过滤栏回调由 `OnFilterChanged` 改为 `OnQuery` + `OnReset`,与 `WorkloadListFilterBar`/`SvcListFilterBar` 等一致,页面接线与新测试按此书写。

## Risks / Trade-offs

- [用户输入后忘记点「查询」,以为过滤未生效] → 与其它列表页完全同构,跨页面操作预期一致;空态文案仍由 `FilterActive` 驱动,不会误导。
- [bUnit 测试从「即时过滤」改为显式点击「查询」] → 新增 `PageFilterFlowTests` 中的节点页流程测试,覆盖「查询前不变、查询后过滤、重置恢复」三段。
