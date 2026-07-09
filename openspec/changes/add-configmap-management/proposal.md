## Why

系统已具备集群管理（`/clusters`）与节点管理（`/nodes`）能力，但运维场景中 ConfigMap 是 Kubernetes 上最常被频繁查看与修改的资源之一。用户希望在不切换到 `kubectl` 的前提下，直接在多集群管理系统中查看、新建、修改、删除目标集群上的 ConfigMap，避免在多集群间切换工具的成本。查看与修改采用独立页面（非对话框），Data 键值对以左侧垂直页签布局展示，并支持通过编辑 YAML 定义来修改 ConfigMap。

## What Changes

- 新增 `ConfigMapService`（Scoped）：承载 ConfigMap 的 k8s 读写操作（列出命名空间、列出/读取/创建/替换/删除 ConfigMap、YAML 反序列化替换），与 `ClusterNodeService` 平级，复用 `ClusterRepository` 获取集群连接信息。
- 新增 `ConfigMaps.razor` 列表页（双路由 `/configmaps` 与 `/configmaps/{ClusterId:int}`）：采用与 `Nodes.razor` 一致的双栏布局（左侧集群选择树 + 右侧内容区），支持命名空间即时过滤、名称搜索、手动刷新。操作列提供"编辑"、"编辑YAML"、"删除"三个按钮（均仅 Admin 可见），点击名称跳转详情页。
- 新增 `ConfigMapDetail.razor` 详情页（路由 `/configmaps/{ClusterId:int}/{Namespace}/{Name}`）：展示元信息 + Data 键值对（`MudTabs Position="Position.Left"` 左侧垂直页签布局），不展示 YAML，不使用对话框。
- 新增 `EditConfigMap.razor` 编辑页（路由 `/configmaps/{ClusterId:int}/{Namespace}/{Name}/edit`）：Data 键值对以左侧垂直页签编辑（可关闭删除键、可添加新键），不展示 YAML，不使用对话框。
- 新增 `EditConfigMapYaml.razor` YAML 编辑页（路由 `/configmaps/{ClusterId:int}/{Namespace}/{Name}/yaml`）：展示 ConfigMap 的 YAML 定义并允许编辑，保存时通过 `KubernetesYaml.Deserialize<V1ConfigMap>` 反序列化后调 `ReplaceNamespacedConfigMapAsync` 提交，不使用对话框。
- 新增 `CreateConfigMapDialog.razor` 对话框（新建场景保持对话框形式），colocated 在 `Pages/ConfigMaps/` 目录下。
- 新增 ViewModel 集（`ConfigMapListViewModel`、`ConfigMapDetailViewModel`（含 `Yaml` 字段）、`ConfigMapCreateViewModel`、`ConfigMapUpdateViewModel`、`ConfigMapDataEntryViewModel`）与 `ConfigMapMappingExtensions`（映射时通过 `KubernetesYaml.Serialize` 生成 YAML）。
- 在 `Drawer.razor` 的 `MudNavMenu` 中新增「配置管理」导航入口。
- 在 `_Imports.razor` 中新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`。
- 在 `Program.cs` 中注册 `ConfigMapService`。
- 可达性判断复用现有模式：页面先调 `ClusterService.GetClusterDetailAsync` 取 `IsReachable`，可达时再调 `ConfigMapService`；`ConfigMapService` 不做容错 catch，异常上抛由页面处理（与 `Nodes.razor` + `ClusterNodeService` 模式一致）。
- 命名空间下拉切换后通过 `ValueChanged` 回调即时刷新列表，无需手动点击刷新按钮。
- 不新增 `Models/` 实体、不新增 `Daos/` 仓储、不改 `AppDbContext`、不引入 EF 迁移——ConfigMap 是集群实时资源，不持久化到本地 SQLite。

## Capabilities

### New Capabilities

- `configmap-management`: ConfigMap 资源的查看（列表 + 详情页）、新建（对话框）、结构化修改（编辑页）、YAML 修改（YAML 编辑页）、删除（确认对话框）能力，覆盖服务层 k8s 调用、ViewModel 映射、页面布局与左侧垂直页签交互、权限控制与离线降级。

### Modified Capabilities

无。本变更不修改任何已有 spec 的需求级别行为。

## Impact

- **新增文件**：`Services/ConfigMapService.cs`、`ViewModels/ConfigMap*.cs`、`ViewModels/Mappings/ConfigMapMappingExtensions.cs`、`Components/Pages/ConfigMaps/ConfigMaps.razor`、`Components/Pages/ConfigMaps/ConfigMapDetail.razor`、`Components/Pages/ConfigMaps/EditConfigMap.razor`、`Components/Pages/ConfigMaps/EditConfigMapYaml.razor`、`Components/Pages/ConfigMaps/CreateConfigMapDialog.razor`。
- **修改文件**：`Components/Layout/Drawer.razor`（新增 NavLink）、`Components/_Imports.razor`（新增 using）、`Program.cs`（注册服务）。
- **不改文件**：`AppDbContext.cs`、`ClusterRepository.cs`、`ClusterService.cs`、`ClusterNodeService.cs`、现有 `Models/` 实体。
- **依赖**：复用现有 `KubernetesClient 19.0.2`（`CoreV1` API 的 ConfigMap 操作 + `KubernetesYaml.Serialize`/`Deserialize` 用于 YAML 序列化与反序列化），无新增 NuGet 包。
- **数据库**：无 schema 变更，无需删除/重建 `clusters.db`。
- **权限**：查看页与列表页对所有登录用户开放；新建/编辑/编辑YAML/删除仅 `Admin` 角色可操作（`AuthorizeView Roles="Admin"` 包裹按钮 + `@attribute [Authorize(Roles="Admin")]` 保护编辑页与 YAML 编辑页）。
