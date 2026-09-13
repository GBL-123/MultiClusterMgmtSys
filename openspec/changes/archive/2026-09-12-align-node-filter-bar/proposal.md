# align-node-filter-bar

## Why

节点管理页的过滤条件栏与其他列表页(集群、服务、工作负载、ConfigMap、命名空间)的观感与交互不一致:名称框缺少搜索图标、没有「查询」按钮、根节点多一个 `mt-2` 间距,且过滤随输入即时生效而不是点「查询」生效。用户在不同列表页之间切换时操作预期被打断。

## What Changes

- `NodeListFilterBar` 对齐其他列表页过滤栏:名称框加搜索图标;布局改为 `MudSpacer` +「查询」(Filled Primary,Search 图标)+「重置」(Outlined Secondary,Refresh 图标);移除根 `MudStack` 的 `mt-2`。
- 过滤改为显式查询:编辑控件只更新草稿条件,点击「查询」后才应用到节点列表;「重置」清空草稿并恢复全量列表。
- 节点数据仍为一次性加载,过滤仍在客户端完成,不引入服务端往返。

## Capabilities

### New Capabilities

(无)

### Modified Capabilities

- `nodes-page`:节点列表过滤条件栏需求的行为契约调整——四个控件 +「查询」「重置」按钮,过滤在点击「查询」时生效、重置恢复全量;新增/改写对应场景。

## Impact

- **修改**:`Components/Nodes/Shared/NodeListFilterBar.razor`(视觉与回调,`OnFilterChanged` → `OnQuery`)、`Components/Nodes/Pages/Nodes.razor`(草稿/已应用过滤分离,接线 `OnQuery`)。
- **测试**:`MultiClusterMgmtSys.Tests/Components/Nodes/NodeListTableTests.cs`(过滤栏回调接线调整)、`MultiClusterMgmtSys.Tests/Components/Pages/PageFilterFlowTests.cs`(新增节点页查询/重置端到端流程)。
- 不涉及服务层、数据库 schema、NuGet 依赖或其它页面。
