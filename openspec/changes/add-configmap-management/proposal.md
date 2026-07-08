## Why

系统已具备集群管理（`/clusters`）与节点管理（`/nodes`）能力，但运维场景中 ConfigMap 是 Kubernetes 上最常被频繁查看与修改的资源之一。用户希望在不切换到 `kubectl` 的前提下，直接在多集群管理系统中查看、新建、修改目标集群上的 ConfigMap，避免在多集群间切换工具的成本。

## What Changes

- 新增 `ConfigMapService`（Scoped）：承载 ConfigMap 的 k8s 读写操作（列出命名空间、列出/读取/创建/替换 ConfigMap），与 `ClusterNodeService` 平级，复用 `ClusterRepository` 获取集群连接信息。
- 新增 `ConfigMaps.razor` 页面（双路由 `/configmaps` 与 `/configmaps/{ClusterId:int}`）：采用与 `Nodes.razor` 一致的双栏布局（左侧集群选择树 + 右侧内容区），支持命名空间过滤、名称搜索、手动刷新。
- 新增 3 个对话框组件（`CreateConfigMapDialog.razor`、`EditConfigMapDialog.razor`、`ConfigMapDetailDialog.razor`），colocated 在 `Pages/ConfigMaps/` 目录下。
- 新增 ViewModel 集（`ConfigMapListViewModel`、`ConfigMapDetailViewModel`、`ConfigMapCreateViewModel`、`ConfigMapUpdateViewModel`、`ConfigMapDataEntryViewModel`）与 `ConfigMapMappingExtensions`。
- 在 `Drawer.razor` 的 `MudNavMenu` 中新增「配置管理」导航入口。
- 在 `_Imports.razor` 中新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`。
- 在 `Program.cs` 中注册 `ConfigMapService`。
- 可达性判断复用现有模式：页面先调 `ClusterService.GetClusterDetailAsync` 取 `IsReachable`，可达时再调 `ConfigMapService`；`ConfigMapService` 不做容错 catch，异常上抛由页面处理（与 `Nodes.razor` + `ClusterNodeService` 模式一致）。
- 不新增 `Models/` 实体、不新增 `Daos/` 仓储、不改 `AppDbContext`、不引入 EF 迁移——ConfigMap 是集群实时资源，不持久化到本地 SQLite。

## Capabilities

### New Capabilities

- `configmap-management`: ConfigMap 资源的查看（列表 + 详情）、新建、修改能力，覆盖服务层 k8s 调用、ViewModel 映射、页面布局与对话框交互、权限控制与离线降级。

### Modified Capabilities

无。本变更不修改任何已有 spec 的需求级别行为。

## Impact

- **新增文件**：`Services/ConfigMapService.cs`、`ViewModels/ConfigMap*.cs`、`ViewModels/Mappings/ConfigMapMappingExtensions.cs`、`Components/Pages/ConfigMaps/ConfigMaps.razor`、`Components/Pages/ConfigMaps/CreateConfigMapDialog.razor`、`Components/Pages/ConfigMaps/EditConfigMapDialog.razor`、`Components/Pages/ConfigMaps/ConfigMapDetailDialog.razor`。
- **修改文件**：`Components/Layout/Drawer.razor`（新增 NavLink）、`Components/_Imports.razor`（新增 using）、`Program.cs`（注册服务）。
- **不改文件**：`AppDbContext.cs`、`ClusterRepository.cs`、`ClusterService.cs`、`ClusterNodeService.cs`、现有 `Models/` 实体。
- **依赖**：复用现有 `KubernetesClient 19.0.2`（`CoreV1` API 的 ConfigMap 操作），无新增 NuGet 包。
- **数据库**：无 schema 变更，无需删除/重建 `clusters.db`。
- **权限**：查看对所有登录用户开放；新建/修改仅 `Admin` 角色可操作（`AuthorizeView Roles="Admin"`）。
