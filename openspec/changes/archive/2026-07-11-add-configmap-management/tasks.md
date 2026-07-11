## 1. ViewModels 与 Mapping

- [x] 1.1 创建 `ViewModels/ConfigMapListViewModel.cs`（Name、Namespace、DataKeyCount、DataKeyPreview、CreatedAt，完整 C# 属性语法，默认值 `= ""` / `= 0` / `= null`）
- [x] 1.2 创建 `ViewModels/ConfigMapDetailViewModel.cs`（Name、Namespace、Uid、CreatedAt、`List<ConfigMapDataEntryViewModel> Data`）
- [x] 1.3 创建 `ViewModels/ConfigMapCreateViewModel.cs`（ClusterId、Name、Namespace、`List<ConfigMapDataEntryViewModel> DataEntries`）
- [x] 1.4 创建 `ViewModels/ConfigMapUpdateViewModel.cs`（ClusterId、Name、Namespace、`List<ConfigMapDataEntryViewModel> DataEntries`）
- [x] 1.5 创建 `ViewModels/ConfigMapDataEntryViewModel.cs`（Key、Value，Value 默认 `= ""`）
- [x] 1.6 创建 `ViewModels/Mappings/ConfigMapMappingExtensions.cs`（`ToConfigMapListViewModel`、`ToConfigMapDetailViewModel` 扩展方法，从 `V1ConfigMap` 映射）

## 2. Service 层

- [x] 2.1 创建 `Services/ConfigMapService.cs`：主构造函数注入 `ClusterRepository`（无 ILogger，与 `ClusterNodeService` 一致）
- [x] 2.2 实现 `private static KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)`（复制 `ClusterNodeService.BuildConfig` 逻辑，支持 KubeConfig / Token 两种连接方式）
- [x] 2.3 实现 `Task<List<string>> GetNamespacesAsync(int clusterId)`：调 `ListNamespaceAsync()`，返回命名空间名称列表
- [x] 2.4 实现 `Task<List<ConfigMapListViewModel>> ListConfigMapsAsync(int clusterId, string? ns)`：ns 为 null 调 `ListConfigMapForAllNamespacesAsync()`，否则调 `ListNamespacedConfigMapAsync(ns)`，映射为列表 ViewModel
- [x] 2.5 实现 `Task<ConfigMapDetailViewModel?> GetConfigMapAsync(int clusterId, string name, string ns)`：调 `ReadNamespacedConfigMapAsync(name, ns)`，映射为详情 ViewModel
- [x] 2.6 实现 `Task CreateConfigMapAsync(int clusterId, ConfigMapCreateViewModel vm)`：构造 `V1ConfigMap`（metadata.name + metadata.namespace + data），调 `CreateNamespacedConfigMapAsync(body, ns)`
- [x] 2.7 实现 `Task UpdateConfigMapAsync(int clusterId, ConfigMapUpdateViewModel vm)`：先 `Read` 取回原 `V1ConfigMap`（保留 Uid/ResourceVersion），替换 `Data` 后调 `ReplaceNamespacedConfigMapAsync(body, name, ns)`
- [x] 2.8 在 `Program.cs` 中添加 `builder.Services.AddScoped<ConfigMapService>();`

## 3. ConfigMaps 列表页

- [x] 3.1 创建 `Components/Pages/ConfigMaps/ConfigMaps.razor`：`@attribute [Authorize]` + `@page "/configmaps"` + `@page "/configmaps/{ClusterId:int}"` + `<PageTitle>配置管理</PageTitle>`
- [x] 3.2 实现左侧集群选择栏：`MudTreeView` 按分组折叠（复用 `Nodes.razor` 的 `groupedClusters` 逻辑与 `ClusterService.GetClustersAsync`），点击集群 → `NavigateTo($"/configmaps/{id}")`
- [x] 3.3 实现右侧内容区骨架：标题行（`MudText h4` + 返回集群详情按钮）+ 集群上下文卡片 + 工具栏 + 表格区
- [x] 3.4 实现集群上下文卡片：复用 `ClusterService.GetClusterDetailAsync` 获取集群信息与 `IsReachable`，展示名称/状态/节点数/API Server
- [x] 3.5 实现工具栏：命名空间下拉（`MudSelect<string?>`，含"全部命名空间"选项，来自 `GetNamespacesAsync`）+ 名称搜索框（前端过滤）+ 刷新按钮
- [x] 3.6 实现 `LoadConfigMapsAsync(int id)` 方法：先 `GetClusterDetailAsync` 取 `IsReachable`，可达时调 `ListConfigMapsAsync`，try/catch + `Snackbar`
- [x] 3.7 实现 `MudTable<ConfigMapListViewModel>`：列——名称、命名空间（`MudChip Small`）、Data键数、键名预览（截断+Title）、创建时间、操作列
- [x] 3.8 实现状态分支：未选集群、加载中、集群不存在、集群不可达、列表为空、搜索无匹配
- [x] 3.9 ~~实现操作列：查看按钮（所有用户）、修改按钮（`AuthorizeView Roles="Admin"` 包裹）~~（由 13.3 取代）
- [x] 3.10 实现"新建 ConfigMap"按钮：`AuthorizeView Roles="Admin"` 包裹，集群离线时禁用，点击打开 `CreateConfigMapDialog`
- [x] 3.11 实现对话框返回后刷新：`if (result is not null && !result.Canceled) await LoadConfigMapsAsync(ClusterId.Value)`

## 4. 新建对话框组件

- [x] 4.1 创建 `Components/Pages/ConfigMaps/CreateConfigMapDialog.razor`：`[Parameter] int ClusterId`，`OnInitializedAsync` 调 `GetNamespacesAsync` 填充命名空间下拉，`MudForm` 含名称（`Required` + k8s 命名正则校验）、命名空间（`MudSelect Required`）、Data 键值对动态列表（增删行 + 键重复校验），提交调 `CreateConfigMapAsync`，成功 `Snackbar` + `Dialog.Close(Ok)`，失败 `Snackbar` 保持打开

## 5. 集成接线

- [x] 5.1 在 `Components/Layout/Drawer.razor` 的 `MudNavMenu` 中，`/nodes` NavLink 之后新增 `<MudNavLink Href="/configmaps" Icon="@Icons.Material.Filled.Settings" Match="NavLinkMatch.Prefix">配置管理</MudNavLink>`
- [x] 5.2 在 `Components/_Imports.razor` 中，`@using MultiClusterMgmtSys.Components.Pages.Nodes` 之后新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`

## 6. 初始验证（对话框版本，已被任务 10/15 取代）

- [x] 6.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过（不构建 slnx 以避免 Docker Compose）
- [x] 6.2 ~~运行应用，从侧边栏进入「配置管理」，验证左侧集群选择树渲染~~（由 15.2 取代）
- [x] 6.3 ~~选择一个 Online 集群，验证命名空间下拉加载 + ConfigMap 列表渲染~~（由 15.3 取代）
- [x] 6.4 ~~测试命名空间过滤、名称搜索、刷新功能~~（由 15.4 取代）
- [x] 6.5 ~~点击"查看"验证详情对话框展示 Data 键值对~~（由 15.5 取代，改为详情页）
- [x] 6.6 ~~以 Admin 登录，测试新建 ConfigMap~~（由 15.6 取代）
- [x] 6.7 ~~以 Admin 登录，测试修改 ConfigMap~~（由 15.7 取代，改为编辑页）
- [x] 6.8 ~~选择一个 Offline 集群，验证"集群不可达"提示~~（由 15.8 取代）
- [x] 6.9 ~~以非 Admin 用户登录，验证新建/修改按钮不可见~~（由 15.9 取代）

## 7. ViewModel 与 Mapping 变更（YAML 字段）

- [x] 7.1 在 `ViewModels/ConfigMapDetailViewModel.cs` 中新增 `public string Yaml { get; set; } = "";` 属性
- [x] 7.2 在 `ViewModels/Mappings/ConfigMapMappingExtensions.cs` 的 `ToConfigMapDetailViewModel` 方法中，使用 `KubernetesYaml.Serialize(cm)` 生成 YAML 字符串并赋值给 `Yaml` 字段（添加 `using k8s;` 引用）

## 8. ConfigMapDetail.razor 详情页

- [x] 8.1 创建 `Components/Pages/ConfigMaps/ConfigMapDetail.razor`：`@attribute [Authorize]` + `@page "/configmaps/{ClusterId:int}/{Namespace}/{Name}"` + `<PageTitle>ConfigMap 详情: @Name</PageTitle>`
- [x] 8.2 实现标题行：`MudText Typo="Typo.h4"` "ConfigMap 详情: @Name" + `MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.ArrowBack"` "返回列表" → `NavigateTo($"/configmaps/{ClusterId}")`
- [x] 8.3 实现 `OnInitializedAsync`：调 `ConfigMapService.GetConfigMapAsync(ClusterId, Name, Namespace)`，加载失败或 null 时显示"ConfigMap 不存在或已被删除"提示 + 返回按钮，try/catch + `Snackbar`
- [x] 8.4 实现元信息卡片：`MudCard` 展示名称、命名空间、创建时间、UID（`MudGrid` 四列布局）
- [x] 8.5 ~~实现 Data 键值对 `MudTabs` 页签区：每个 Data 键一个 `MudTabPanel`~~（由 14.1 取代，改为左侧垂直页签）
- [x] 8.6 ~~实现"YAML"页签~~（由 14.2 取代，移除 YAML 页签）
- [x] 8.7 实现无 Data 时的空状态：Data 页签区域显示"暂无 Data"提示
- [x] 8.8 实现加载中状态：`MudProgressLinear Indeterminate`；实现加载失败/null 状态：`MudCard` 提示 + 返回列表按钮

## 9. EditConfigMap.razor 编辑页 + 列表页修改 + 删除旧对话框

- [x] 9.1 创建 `Components/Pages/ConfigMaps/EditConfigMap.razor`：`@attribute [Authorize(Roles="Admin")]` + `@page "/configmaps/{ClusterId:int}/{Namespace}/{Name}/edit"` + `<PageTitle>修改 ConfigMap: @Name</PageTitle>`
- [x] 9.2 实现标题行：`MudText Typo="Typo.h4"` "修改 ConfigMap: @Name" + `MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.ArrowBack"` "返回列表" → `NavigateTo($"/configmaps/{ClusterId}")`
- [x] 9.3 实现 `OnInitializedAsync`：调 `ConfigMapService.GetConfigMapAsync` 加载当前 Data 作为初始值（`List<ConfigMapDataEntryViewModel>`），加载失败或 null 时显示"ConfigMap 不存在或已被删除"提示 + 返回按钮
- [x] 9.4 实现名称与命名空间只读字段：两个 `MudTextField ReadOnly="true"` 展示 Name 和 Namespace
- [x] 9.5 ~~实现 Data 键值对 `MudTabs` 页签编辑区~~（由 14.3 取代，改为左侧垂直页签）
- [x] 9.6 实现页签关闭删除键：`MudTabPanel` 的 `Closeable="true"` + `OnClose` 事件处理，关闭页签时从 `dataEntries` 列表移除对应项
- [x] 9.7 实现"添加键"功能：页签栏末尾或上方提供"添加键" `MudButton`，点击后弹出简单输入框收集键名，校验键名不重复后创建新 `ConfigMapDataEntryViewModel` 并添加到列表，自动选中新页签
- [x] 9.8 实现"保存"按钮：调 `UpdateConfigMapAsync`，成功后 `Snackbar` "修改成功" + `NavigateTo($"/configmaps/{ClusterId}")`，409 冲突时 `Snackbar` "资源已被他人修改，请刷新后重试"（保持页面），其他异常 `Snackbar` 错误提示
- [x] 9.9 实现键重复校验：添加新键时若键名已存在则 `Snackbar` 提示"键名已存在"，不创建重复页签
- [x] 9.10 ~~修改 `ConfigMaps.razor` 列表页：名称列点击与"查看"按钮改为 `NavigateTo`~~（由 13.1/13.2 取代）
- [x] 9.11 修复 `ConfigMaps.razor` 命名空间即时刷新：将 `MudSelect` 的 `@bind-Value="selectedNamespace" @onchange="OnNamespaceChanged"` 改为 `Value="selectedNamespace" ValueChanged="OnNamespaceValueChanged"`
- [x] 9.12 移除 `ConfigMaps.razor` 中 `OpenDetailDialog` 和 `OpenEditDialog` 方法
- [x] 9.13 删除 `Components/Pages/ConfigMaps/ConfigMapDetailDialog.razor`
- [x] 9.14 删除 `Components/Pages/ConfigMaps/EditConfigMapDialog.razor`

## 10. 验证（页签化版本，已被任务 15 取代）

- [x] 10.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过
- [x] 10.2 ~~运行应用，从侧边栏进入「配置管理」~~（由 15.2 取代）
- [x] 10.3 ~~选择一个 Online 集群~~（由 15.3 取代）
- [x] 10.4 ~~命名空间即时刷新~~（由 15.4 取代）
- [x] 10.5 ~~详情页验证~~（由 15.5 取代）
- [x] 10.6 ~~新建 ConfigMap~~（由 15.6 取代）
- [x] 10.7 ~~编辑页验证~~（由 15.7 取代）
- [x] 10.8 ~~离线集群~~（由 15.8 取代）
- [x] 10.9 ~~非 Admin~~（由 15.9 取代）
- [x] 10.10 ~~返回列表~~（由 15.10 取代）

## 11. Service 层新增（删除 + YAML 更新）

- [x] 11.1 在 `ConfigMapService.cs` 中实现 `Task DeleteConfigMapAsync(int clusterId, string name, string ns)`：调 `DeleteNamespacedConfigMapAsync(name, ns)`
- [x] 11.2 在 `ConfigMapService.cs` 中实现 `Task UpdateConfigMapFromYamlAsync(int clusterId, string name, string ns, string yaml)`：`KubernetesYaml.Deserialize<V1ConfigMap>(yaml)` 反序列化，先 `ReadNamespacedConfigMapAsync` 取回原对象保留 `ResourceVersion`，用反序列化结果的 `Data` 替换后调 `ReplaceNamespacedConfigMapAsync`

## 12. EditConfigMapYaml.razor YAML 编辑页

- [x] 12.1 创建 `Components/Pages/ConfigMaps/EditConfigMapYaml.razor`：`@attribute [Authorize(Roles="Admin")]` + `@page "/configmaps/{ClusterId:int}/{Namespace}/{Name}/yaml"` + `<PageTitle>编辑 YAML: @Name</PageTitle>`
- [x] 12.2 实现标题行：`MudText Typo="Typo.h4"` "编辑 YAML: @Name" + `MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.ArrowBack"` "返回列表" → `NavigateTo($"/configmaps/{ClusterId}")`
- [x] 12.3 实现 `OnInitializedAsync`：调 `ConfigMapService.GetConfigMapAsync` 加载，取 `detail.Yaml` 作为编辑区初始值，加载失败或 null 时显示"ConfigMap 不存在或已被删除"提示 + 返回按钮
- [x] 12.4 实现名称与命名空间只读字段：两个 `MudTextField ReadOnly="true"` 展示 Name 和 Namespace
- [x] 12.5 实现 YAML 编辑区：`MudTextField` 多行（`Lines="25"` + 等宽字体 `Style="font-family: monospace;"` + `@bind-Value="yamlContent"`）
- [x] 12.6 实现"保存"按钮：调 `UpdateConfigMapFromYamlAsync(ClusterId, Name, Namespace, yamlContent)`，成功后 `Snackbar` "修改成功" + `NavigateTo($"/configmaps/{ClusterId}")`，YAML 格式错误时 `Snackbar` "YAML 格式错误: {ex.Message}"，409 冲突时 `Snackbar` "资源已被他人修改，请刷新后重试"，其他异常 `Snackbar` 错误提示
- [x] 12.7 实现加载中状态与加载失败/null 状态

## 13. 列表页操作列修改

- [x] 13.1 修改 `ConfigMaps.razor` 名称列：保留点击名称跳转详情页（`NavigateToDetail` 方法），移除"查看"图标按钮
- [x] 13.2 修改 `ConfigMaps.razor` 操作列：将"修改"按钮改为"编辑"按钮（`NavigateToEdit` 方法，已有），新增"编辑YAML"按钮（`NavigateToYamlEdit` 方法 → `NavigateTo($"/configmaps/{ClusterId}/{ns}/{name}/yaml")`），新增"删除"按钮（`DeleteConfigMap` 方法 → `DialogService.ShowMessageBoxAsync` 确认 → `DeleteConfigMapAsync` → 刷新列表）
- [x] 13.3 操作列三个按钮均用 `AuthorizeView Roles="Admin"` 包裹，非 Admin 用户仅可见名称点击查看

## 14. 详情页与编辑页页签布局修改

- [x] 14.1 修改 `ConfigMapDetail.razor`：将 `MudTabs` 改为 `Position="Position.Left"` 左侧垂直页签布局，移除"YAML"页签
- [x] 14.2 修改 `ConfigMapDetail.razor`：移除 YAML 相关的 `MudTabPanel`，仅保留 Data 键页签
- [x] 14.3 修改 `EditConfigMap.razor`：将 `MudTabs` 改为 `Position="Position.Left"` 左侧垂直页签布局

## 15. 验证（最终版本）

- [x] 15.1 `dotnet build MultiClusterMgmtSys/MultiClusterMgmtSys.csproj` 通过（不构建 slnx 以避免 Docker Compose）
- [ ] 15.2 运行应用，从侧边栏进入「配置管理」，验证左侧集群选择树渲染
- [ ] 15.3 选择一个 Online 集群，验证命名空间下拉加载 + ConfigMap 列表渲染
- [ ] 15.4 在列表页切换命名空间下拉，验证列表立即刷新；测试名称搜索、手动刷新功能
- [ ] 15.5 从列表页点击 ConfigMap 名称，验证跳转至详情页；在详情页验证 Data 键值对以左侧垂直页签展示，点击不同页签切换显示对应值；验证详情页不展示 YAML
- [ ] 15.6 以 Admin 登录，测试新建 ConfigMap（含校验：名称规则、键重复、名称冲突 409）
- [ ] 15.7 以 Admin 登录，从列表页点击"编辑"按钮，验证跳转至编辑页；在编辑页验证 Data 左侧垂直页签可编辑、可关闭删除键、可添加新键；保存后验证 `Snackbar` 提示 + 自动返回列表页
- [ ] 15.8 以 Admin 登录，从列表页点击"编辑YAML"按钮，验证跳转至 YAML 编辑页；编辑 YAML 后保存，验证 `Snackbar` 提示 + 自动返回列表页；测试 YAML 格式错误时的错误提示
- [ ] 15.9 以 Admin 登录，从列表页点击"删除"按钮，验证确认对话框弹出；确认后验证 `Snackbar` 提示 + 列表刷新
- [ ] 15.10 选择一个 Offline 集群，验证"集群不可达"提示 + 新建/编辑/删除按钮禁用
- [ ] 15.11 以非 Admin 用户登录，验证操作列按钮不可见；直接访问 `/configmaps/{id}/{ns}/{name}/edit` 和 `/configmaps/{id}/{ns}/{name}/yaml`，验证重定向至 `/access-denied`
- [ ] 15.12 验证详情页/编辑页/YAML编辑页的"返回列表"按钮正确导航回 `/configmaps/{ClusterId}`
