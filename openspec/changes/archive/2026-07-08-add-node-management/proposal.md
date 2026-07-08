## Why

系统已具备集群管理能力，但缺乏节点维度的观测。运维场景中需要查看集群下全部节点的概要信息（名称、状态、角色、版本、地址等）以及单节点下钻详情（容量、可分配资源、条件、污点、标签、注解、系统信息等）。需要新增独立的节点管理入口与节点详情页，支持从侧边栏独立进入和从集群详情下钻进入两种方式。

## What Changes

- 新增 `ClusterNodeService`（Scoped，`Services/`）：从 `ClusterService` 中抽取节点查询职责，主构造函数仅注入 `ClusterRepository`（无 ILogger），承载 `GetClusterNodesAsync`（列表）与 `GetNodeDetailAsync`（详情）。不做 try/catch，异常上抛由页面处理。
- 新增 `ClusterNodeDetailViewModel` 及子 ViewModel（`NodeAddressViewModel`、`NodeConditionViewModel`、`NodeTaintViewModel`、`NodeSystemInfoViewModel`）。
- 新增 `Nodes.razor`（双路由 `/nodes` 与 `/nodes/{ClusterId:int}`）：双栏布局——左侧 `MudTreeView` 集群选择树（按分组折叠）+ 右侧内容区（标题 + 集群上下文卡片 + 工具栏 + 节点表格）。选择集群后 `NavigateTo("/nodes/{id}")` 统一 URL。
- 新增 `NodeDetail.razor`（路由 `/nodes/{ClusterId:int}/{NodeName}`）：多卡片分块展示节点详情（概要/调度/元数据/资源容量/地址/条件/污点/标签/注解/系统信息/操作）。
- `ClusterDetail.razor` 改造：节点列表卡片增加"查看全部"入口（跳转 `/nodes/{Id}`），节点名称可点击下钻（跳转 `/nodes/{Id}/{nodeName}`）。
- `Drawer.razor` 新增 `MudNavLink Href="/nodes"` 「节点管理」导航入口。
- `_Imports.razor` 新增 `@using MultiClusterMgmtSys.Components.Pages.Nodes`。
- `Program.cs` 注册 `ClusterNodeService`（Scoped）。
- 节点数据不持久化到 SQLite，每次从 k8s 实时拉取，不涉及 `AppDbContext` 模型变更。

## Capabilities

### New Capabilities

- `node-management`: 节点列表查看（双栏布局 + 集群选择树 + 前端搜索）、单节点详情下钻（多卡片分块）、从集群详情下钻入口。

### Modified Capabilities

无。

## Impact

- **新增文件**：`Services/ClusterNodeService.cs`、`ViewModels/ClusterNodeDetailViewModel.cs`、`ViewModels/NodeAddressViewModel.cs`、`ViewModels/NodeConditionViewModel.cs`、`ViewModels/NodeTaintViewModel.cs`、`ViewModels/NodeSystemInfoViewModel.cs`、`Components/Pages/Nodes/Nodes.razor`、`Components/Pages/Nodes/NodeDetail.razor`。
- **修改文件**：`Services/ClusterService.cs`（节点查询职责移至 `ClusterNodeService`，`GetClusterDetailAsync` 改为调 `nodeService.GetClusterNodesAsync`）、`Components/Pages/Clusters/ClusterDetail.razor`（节点列表增加下钻入口）、`Components/Layout/Drawer.razor`（新增 NavLink）、`Components/_Imports.razor`（新增 using）、`Program.cs`（注册 `ClusterNodeService`）。
- **数据库**：无 schema 变更，无需删除/重建 `clusters.db`。
- **依赖**：无新增 NuGet 包。
