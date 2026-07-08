## 1. 模型与数据层

- [x] 1.1 创建 `Models/ConnectionType.cs`（`enum ConnectionType { KubeConfig, Token }`）
- [x] 1.2 `Models/ClusterInfo.cs` 新增 `ConnectionType`、`Token`、`SkipTlsVerify`（默认 true）、`LastCheckedAt` 字段
- [x] 1.3 `Daos/AppDbContext.cs` 配置新字段（`Token` TEXT、`SkipTlsVerify` 默认 true、`LastCheckedAt` nullable）
- [x] 1.4 创建 `Daos/ClusterRepository.cs`（`GetAllAsync` Include(Group)、`GetByIdAsync`、`AddAsync`、`UpdateAsync`、`DeleteAsync`）
- [x] 1.5 创建 `Daos/GroupRepository.cs`（`GetAllAsync` Include(Clusters)、`GetByIdAsync`、`AddAsync`、`DeleteAsync`）

## 2. ViewModel 与 Mapping

- [x] 2.1 创建 `ViewModels/ClusterViewModel.cs`（列表用，不含密文）
- [x] 2.2 创建 `ViewModels/ClusterDetailViewModel.cs`（详情用，含 `Nodes` + `IsReachable`，不含密文）
- [x] 2.3 创建 `ViewModels/ClusterEditViewModel.cs`（编辑预填，含密文）
- [x] 2.4 创建 `ViewModels/ClusterCreateViewModel.cs` + `ClusterUpdateViewModel.cs`（输入 VM）
- [x] 2.5 创建 `ViewModels/ClusterNodeViewModel.cs`（节点列表项，不持久化）
- [x] 2.6 创建 `ViewModels/ClusterGroupViewModel.cs` + `GroupCreateViewModel.cs`
- [x] 2.7 创建 `ViewModels/Mappings/ClusterMappingExtensions.cs`（`ToViewModel`、`ToDetailViewModel`、`ToEditViewModel`）
- [x] 2.8 创建 `ViewModels/Mappings/GroupMappingExtensions.cs`（`ToViewModel`）

## 3. 服务层

- [x] 3.1 重构 `Services/ClusterService.cs`：注入 `ClusterRepository` + `ClusterNodeService` + `ILogger`，所有方法返回 ViewModel
- [x] 3.2 实现 `BuildConfig(ClusterInfo)` 私有方法（KubeConfig 走 `BuildConfigFromConfigFile(stream)`，Token 走手动 `KubernetesClientConfiguration`）
- [x] 3.3 实现 `ProbeAsync(ClusterInfo)` 私有方法（`Version.GetCodeAsync` + `ListNodeAsync`，成功设 Online，失败设 Offline + 记日志）
- [x] 3.4 实现 `GetClustersAsync`、`GetClusterDetailAsync`、`GetClusterForEditAsync`、`AddClusterAsync`、`UpdateClusterAsync`、`DeleteClusterAsync`、`RefreshClusterStatusAsync`
- [x] 3.5 创建 `Services/GroupService.cs`（`GetGroupsAsync`、`AddGroupAsync`、`DeleteGroupAsync`）
- [x] 3.6 `Program.cs` 注册 `ClusterRepository`、`GroupRepository`、`ClusterService`、`GroupService`（Scoped）

## 4. UI 主题系统

- [x] 4.1 创建 `Components/Theme/ThemeManager.cs`：定义 `MudTheme`（PaletteLight/PaletteDark Indigo-Blue + Typography + LayoutProperties DefaultBorderRadius 6px）
- [x] 4.2 实现 `InitializeAsync()`（读 localStorage > 默认 false）+ `ToggleDarkModeAsync()`（切换 + 写 localStorage + 锁定 ObserveSystemDarkModeChange）
- [x] 4.3 `Program.cs` 注册 `ThemeManager`（Scoped）
- [x] 4.4 `MainLayout.razor` 绑定 `MudThemeProvider`（Theme/IsDarkMode/IsDarkModeChanged/ObserveSystemDarkModeChange），`OnAfterRenderAsync(firstRender)` 调 `InitializeAsync()` + `StateHasChanged()`
- [x] 4.5 `AppBar.razor` 添加主题切换按钮（条件渲染 LightMode/DarkMode 图标）

## 5. 集群列表页

- [x] 5.1 `Clusters.razor` 重构为 `MudStack` + `MudText h4` 标题 + 两行工具栏 + `MudTable`
- [x] 5.2 第一行：集群数量副标题 + "分组管理""新建分组""添加集群"按钮（后两者 `AuthorizeView Roles="Admin"`）
- [x] 5.3 第二行工具栏：名称搜索 + 分组下拉 + 状态下拉 + 版本下拉 + 开始时间 + 结束时间 + 重置按钮
- [x] 5.4 `filteredClusters` 计算属性：六条件 AND 组合
- [x] 5.5 `MudTable` 列：名称（可点击+排序）、状态（Chip+排序）、版本（排序）、节点数（排序）、分组、API Server（截断+Title）、创建时间（排序）、操作（刷新/编辑/删除，`AuthorizeView` 包裹）
- [x] 5.6 状态分支：loading（`MudProgressLinear`）、无集群（空态+添加按钮）、筛选无结果（空态+重置按钮）

## 6. 对话框组件

- [x] 6.1 `AddClusterDialog.razor`：`MudToggleGroup` 切换 KubeConfig/Token；KubeConfig 支持粘贴/上传（`InputFile` ≤256KB）；Token 填 API Server + Token + SkipTlsVerify；`MudForm` 校验；提交调 `AddClusterAsync`
- [x] 6.2 `EditClusterDialog.razor`：预填当前值（`GetClusterForEditAsync`），允许切换连接方式，保存调 `UpdateClusterAsync`
- [x] 6.3 `CreateGroupDialog.razor`：名称必填，提交调 `AddGroupAsync`
- [x] 6.4 `ManageGroupsDialog.razor`：`MudTable` 展示分组列表（名称/集群数/删除），删除二次确认，新建分组入口

## 7. 集群详情页

- [x] 7.1 `ClusterDetail.razor`（`@page "/clusters/{Id:int}"`）：标题 + 返回按钮
- [x] 7.2 基本信息卡片：名称/状态 Chip/版本/节点数/分组/API Server/创建时间/最后检测时间
- [x] 7.3 连接信息卡片：连接方式/API Server + "显示密文"按钮（`AuthorizeView`，调 `GetClusterForEditAsync` 加载密文，密码态展示可切换明文）
- [x] 7.4 节点列表卡片：`GetClusterDetailAsync` 取 `IsReachable`，可达时展示节点 `MudTable`，离线时降级提示，"查看全部"跳转 `/nodes/{Id}`
- [x] 7.5 操作区：刷新状态/编辑/删除按钮（`AuthorizeView Roles="Admin"`）

## 8. 导航与接线

- [x] 8.1 `Drawer.razor` 新增 `MudNavLink Href="/clusters" Icon="Hub" Match="Prefix"` 「集群管理」
- [x] 8.2 `_Imports.razor` 新增 `@using MultiClusterMgmtSys.Components.Pages.Clusters`
- [x] 8.3 `Components/Layout/EmptyLayout.razor`（登录页用，仅 ThemeProvider + providers）

## 9. 验证

- [x] 9.1 删除 `clusters.db*` 重建库，`dotnet build` 通过
- [x] 9.2 集群列表表格渲染 + 表头排序
- [x] 9.3 六条件过滤组合 + 重置
- [x] 9.4 KubeConfig 粘贴/上传两种方式添加集群
- [x] 9.5 Token 方式添加集群
- [x] 9.6 编辑集群（改名不探测、改连接配置探测）
- [x] 9.7 删除集群（二次确认）
- [x] 9.8 刷新集群状态
- [x] 9.9 详情页基本信息 + 连接信息 + 节点列表（在线/离线两种）
- [x] 9.10 显示密文（Admin 可见，Guest 不可见）
- [x] 9.11 分组新建/删除/管理
- [x] 9.12 主题切换 + 刷新保持 + OS 跟随
