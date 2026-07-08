## Why

系统最初为简单的集群列表展示，缺乏结构化分层、多维度过滤、编辑能力、详情页和统一的 UI 主题。需要重构为可维护的分层架构（Repository → Service → ViewModel → Page），并增加表格视图、多条件过滤、集群编辑、详情页、分组管理和 Indigo-Blue 主题系统。

## What Changes

- **分层重构**：新增 `Daos/ClusterRepository`、`Daos/GroupRepository`（返回实体），`Services/ClusterService`、`Services/GroupService`（返回 ViewModel），`ViewModels/` 全套 VM + `Mappings/` 扩展方法。Service 不返回实体，Razor 不引用 `Models/`。
- **数据模型扩展**：`ClusterInfo` 新增 `ConnectionType`（enum）、`Token`、`SkipTlsVerify`、`LastCheckedAt` 字段。新增 `ConnectionType` 枚举（KubeConfig/Token）。
- **集群列表页**（`Clusters.razor`）：单列布局，`MudTable` 表格视图（名称/状态/版本/节点数/分组/API Server/创建时间/操作），表头 `MudTableSortLabel` 排序，多条件 AND 过滤（名称搜索 + 分组 + 状态 + 版本 + 创建时间范围），重置按钮。
- **添加集群对话框**（`AddClusterDialog.razor`）：`MudToggleGroup` 切换 KubeConfig/Token 两种连接方式；KubeConfig 支持粘贴文本与文件上传（`InputFile`，≤256KB）；Token 模式填 API Server + Bearer Token + SkipTlsVerify。提交后立即探测连通性。
- **编辑集群对话框**（`EditClusterDialog.razor`）：预填当前值，允许切换连接方式，连接配置变更时保存后重新探测。
- **集群详情页**（`ClusterDetail.razor`，路由 `/clusters/{Id:int}`）：基本信息卡片 + 连接信息卡片（密文默认掩码，Admin 可显示）+ 节点列表卡片（实时拉取，离线降级）+ 操作区。
- **分组管理**：`CreateGroupDialog.razor`（新建分组）+ `ManageGroupsDialog.razor`（分组列表 + 删除，删除时二次确认）。
- **UI 主题系统**：`Components/Theme/ThemeManager.cs`（Scoped），定义 Indigo-Blue `MudTheme`（浅色/暗色双调色板 + Typography + LayoutProperties），`localStorage` 持久化用户偏好（键 `mcm-theme-dark-mode`），首次访问跟随 OS `prefers-color-scheme`。`MainLayout.razor` 绑定 `MudThemeProvider`，`AppBar.razor` 提供切换按钮。
- **导航菜单**：`Drawer.razor` 中 `MudNavLink` 指向 `/clusters`（图标 Hub，`Match=Prefix`）。
- **对话框 colocate**：所有对话框放在 `Pages/Clusters/` 目录下（与页面同目录），非独立 `Pages/Dialogs/` 目录。`_Imports.razor` 添加 `@using MultiClusterMgmtSys.Components.Pages.Clusters`。

## Capabilities

### New Capabilities

- `cluster-management`: 集群 CRUD、分组管理、多维度过滤排序、详情页、连接方式（KubeConfig/Token）、UI 主题系统。

### Modified Capabilities

无。

## Impact

- **新增文件**：`Models/ConnectionType.cs`、`Daos/ClusterRepository.cs`、`Daos/GroupRepository.cs`、`Services/GroupService.cs`、`ViewModels/Cluster*.cs`、`ViewModels/ClusterGroupViewModel.cs`、`ViewModels/GroupCreateViewModel.cs`、`ViewModels/Mappings/ClusterMappingExtensions.cs`、`ViewModels/Mappings/GroupMappingExtensions.cs`、`Components/Theme/ThemeManager.cs`、`Components/Pages/Clusters/ClusterDetail.razor`、`Components/Pages/Clusters/AddClusterDialog.razor`、`Components/Pages/Clusters/EditClusterDialog.razor`、`Components/Pages/Clusters/CreateGroupDialog.razor`、`Components/Pages/Clusters/ManageGroupsDialog.razor`、`Components/Layout/EmptyLayout.razor`。
- **修改文件**：`Models/ClusterInfo.cs`（新增字段）、`Daos/AppDbContext.cs`（新字段配置）、`Services/ClusterService.cs`（重构为返回 ViewModel + `BuildConfig` 私有方法）、`Components/Pages/Clusters/Clusters.razor`（重构为表格 + 过滤）、`Components/Layout/MainLayout.razor`（ThemeProvider 绑定 + 委托 AppBar/Drawer）、`Components/Layout/AppBar.razor`（主题切换按钮）、`Components/Layout/Drawer.razor`（NavLink）、`Components/_Imports.razor`（using）、`Program.cs`（注册 Repository/Service/ThemeManager）。
- **数据库**：`ClusterInfo` 新增字段触发 schema 变更，需删除 `clusters.db*` 重建库。
- **依赖**：无新增 NuGet 包。
