## 1. ViewModels 与 Mapping

- [ ] 1.1 创建 `ViewModels/ConfigMapListViewModel.cs`（Name、Namespace、DataKeyCount、DataKeyPreview、CreatedAt，完整 C# 属性语法，默认值 `= ""` / `= 0` / `= null`）
- [ ] 1.2 创建 `ViewModels/ConfigMapDetailViewModel.cs`（Name、Namespace、Uid、CreatedAt、`List<ConfigMapDataEntryViewModel> Data`）
- [ ] 1.3 创建 `ViewModels/ConfigMapCreateViewModel.cs`（ClusterId、Name、Namespace、`List<ConfigMapDataEntryViewModel> DataEntries`）
- [ ] 1.4 创建 `ViewModels/ConfigMapUpdateViewModel.cs`（ClusterId、Name、Namespace、`List<ConfigMapDataEntryViewModel> DataEntries`）
- [ ] 1.5 创建 `ViewModels/ConfigMapDataEntryViewModel.cs`（Key、Value，Value 默认 `= ""`）
- [ ] 1.6 创建 `ViewModels/Mappings/ConfigMapMappingExtensions.cs`（`ToConfigMapListViewModel`、`ToConfigMapDetailViewModel` 扩展方法，从 `V1ConfigMap` 映射）

## 2. Service 层

- [ ] 2.1 创建 `Services/ConfigMapService.cs`：主构造函数注入 `ClusterRepository`（无 ILogger，与 `ClusterNodeService` 一致）
- [ ] 2.2 实现 `private static KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)`（复制 `ClusterNodeService.BuildConfig` 逻辑，支持 KubeConfig / Token 两种连接方式）
- [ ] 2.3 实现 `Task<List<string>> GetNamespacesAsync(int clusterId)`：调 `ListNamespaceAsync()`，返回命名空间名称列表
- [ ] 2.4 实现 `Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(int clusterId, string? ns)`：ns 为 null 调 `ListConfigMapForAllNamespacesAsync()`，否则调 `ListNamespacedConfigMapAsync(ns)`，映射为列表 ViewModel
- [ ] 2.5 实现 `Task<ConfigMapDetailViewModel?> GetConfigMapAsync(int clusterId, string name, string ns)`：调 `ReadNamespacedConfigMapAsync(name, ns)`，映射为详情 ViewModel
- [ ] 2.6 实现 `Task CreateConfigMapAsync(int clusterId, ConfigMapCreateViewModel vm)`：构造 `V1ConfigMap`（metadata.name + metadata.namespace + data），调 `CreateNamespacedConfigMapAsync(body, ns)`
- [ ] 2.7 实现 `Task UpdateConfigMapAsync(int clusterId, ConfigMapUpdateViewModel vm)`：先 `Read` 取回原 `V1ConfigMap`（保留 Uid/ResourceVersion），替换 `Data` 后调 `ReplaceNamespacedConfigMapAsync(body, name, ns)`
- [ ] 2.8 在 `Program.cs` 中添加 `builder.Services.AddScoped<ConfigMapService>();`

## 3. ConfigMaps 页面

- [ ] 3.1 创建 `Components/Pages/ConfigMaps/ConfigMaps.razor`：`@attribute [Authorize]` + `@page "/configmaps"` + `@page "/configmaps/{ClusterId:int}"` + `<PageTitle>配置管理</PageTitle>`
- [ ] 3.2 实现左侧集群选择栏：`MudTreeView` 按分组折叠（复用 `Nodes.razor` 的 `groupedClusters` 逻辑与 `ClusterService.GetClustersAsync`），点击集群 → `NavigateTo($"/configmaps/{id}")`
- [ ] 3.3 实现右侧内容区骨架：标题行（`MudText h4` + 返回集群详情按钮）+ 集群上下文卡片 + 工具栏 + 表格区
- [ ] 3.4 实现集群上下文卡片：复用 `ClusterService.GetClusterDetailAsync` 获取集群信息与 `IsReachable`，展示名称/状态/节点数/API Server
- [ ] 3.5 实现工具栏：命名空间下拉（`MudSelect<string?>`，含"全部命名空间"选项，来自 `GetNamespacesAsync`）+ 名称搜索框（前端过滤）+ 刷新按钮
- [ ] 3.6 实现 `LoadConfigMapsAsync(int id)` 方法：先 `GetClusterDetailAsync` 取 `IsReachable`，可达时调 `ListConfigMapsAsync`，try/catch + `Snackbar`
- [ ] 3.7 实现 `MudTable<ConfigMapListViewModel>`：列——名称（可点击打开详情对话框）、命名空间（`MudChip Small`）、Data键数、键名预览（截断+Title）、创建时间、操作列（查看+修改图标按钮）
- [ ] 3.8 实现状态分支：未选集群（"请从左侧选择一个集群"）、加载中（`MudProgressLinear`）、集群不存在（"未找到该集群"）、集群不可达（"集群不可达，无法获取 ConfigMap"）、列表为空（"暂无 ConfigMap"）、搜索无匹配（"未找到匹配的 ConfigMap" + 重置按钮）
- [ ] 3.9 实现操作列：查看按钮（所有用户，打开 `ConfigMapDetailDialog`）、修改按钮（`AuthorizeView Roles="Admin"` 包裹，打开 `EditConfigMapDialog`）
- [ ] 3.10 实现"新建 ConfigMap"按钮：`AuthorizeView Roles="Admin"` 包裹，集群离线时禁用，点击打开 `CreateConfigMapDialog`
- [ ] 3.11 实现对话框返回后刷新：`if (result is not null && !result.Canceled) await LoadConfigMapsAsync(ClusterId.Value)`

## 4. 对话框组件

- [ ] 4.1 创建 `Components/Pages/ConfigMaps/ConfigMapDetailDialog.razor`：`[CascadingParameter] IMudDialogInstance` + `[Inject] ConfigMapService` + `[Inject] ISnackbar`，`[Parameter]` 接收 ClusterId/Name/Namespace，`OnInitializedAsync` 调 `GetConfigMapAsync` 加载，展示元信息 + Data 键值对卡片（只读多行 `MudTextField` 等宽字体），"关闭"按钮调 `Dialog.Cancel()`
- [ ] 4.2 创建 `Components/Pages/ConfigMaps/CreateConfigMapDialog.razor`：`[Parameter] int ClusterId`，`OnInitializedAsync` 调 `GetNamespacesAsync` 填充命名空间下拉，`MudForm` 含名称（`Required` + k8s 命名正则校验）、命名空间（`MudSelect Required`）、Data 键值对动态列表（增删行 + 键重复校验），提交调 `CreateConfigMapAsync`，成功 `Snackbar` + `Dialog.Close(Ok)`，失败 `Snackbar` 保持打开
- [ ] 4.3 创建 `Components/Pages/ConfigMaps/EditConfigMapDialog.razor`：`[Parameter]` 接收 ClusterId/Name/Namespace，`OnInitializedAsync` 调 `GetConfigMapAsync` 加载当前 Data 作为初始值（加载失败 `Dialog.Cancel` + `Snackbar`），名称/命名空间只读，Data 可编辑，提交调 `UpdateConfigMapAsync`，成功/失败处理同新建

## 5. 集成接线

- [ ] 5.1 在 `Components/Layout/Drawer.razor` 的 `MudNavMenu` 中，`/nodes` NavLink 之后新增 `<MudNavLink Href="/configmaps" Icon="@Icons.Material.Filled.Settings" Match="NavLinkMatch.Prefix">配置管理</MudNavLink>`
- [ ] 5.2 在 `Components/_Imports.razor` 中，`@using MultiClusterMgmtSys.Components.Pages.Nodes` 之后新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`

## 6. 验证

- [ ] 6.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过（不构建 slnx 以避免 Docker Compose）
- [ ] 6.2 运行应用，从侧边栏进入「配置管理」，验证左侧集群选择树渲染
- [ ] 6.3 选择一个 Online 集群，验证命名空间下拉加载 + ConfigMap 列表渲染
- [ ] 6.4 测试命名空间过滤、名称搜索、刷新功能
- [ ] 6.5 点击"查看"验证详情对话框展示 Data 键值对
- [ ] 6.6 以 Admin 登录，测试新建 ConfigMap（含校验：名称规则、键重复、名称冲突 409）
- [ ] 6.7 以 Admin 登录，测试修改 ConfigMap（含校验：资源版本冲突 409、ConfigMap 已删除）
- [ ] 6.8 选择一个 Offline 集群，验证"集群不可达"提示 + 新建/修改按钮禁用
- [ ] 6.9 以非 Admin 用户登录，验证新建/修改按钮不可见
