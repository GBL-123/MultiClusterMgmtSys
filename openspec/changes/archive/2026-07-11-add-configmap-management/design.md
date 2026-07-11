## Context

系统当前分层为 `Repository → Service → ViewModel + Mapping → Razor Page/Dialog`，已有两个 k8s 资源管理先例：

- **`ClusterService`**：集群 CRUD + 连通性探测（`ProbeAsync`），内部 catch 异常并改写 `ClusterInfo.Status`，注入 `ILogger`。
- **`ClusterNodeService`**：节点列表/详情的实时拉取，主构造函数仅注入 `ClusterRepository`，**无 logger**，**不做 try/catch**——异常直接上抛由调用方（页面）处理。

可达性判断的既有链路（`Nodes.razor` 实现）：
```
页面 LoadNodesAsync(id)
  → cluster = ClusterService.GetClusterDetailAsync(id)   // 内部 catch，设 IsReachable
  → if (cluster.IsReachable)
      nodes = ClusterNodeService.GetClusterNodesAsync(id)  // 不 catch，异常上抛
    else
      nodes = empty
```

页面布局既有模式（`Nodes.razor`）：双栏 `MudGrid`——左侧 `MudTreeView` 集群选择树（按分组折叠）+ 右侧内容区（标题 + 上下文卡片 + 工具栏 + 表格）。

独立详情页先例：`NodeDetail.razor`（路由 `/nodes/{ClusterId:int}/{NodeName}`），采用 `@attribute [Authorize]` + 标题行 + 返回按钮 + `MudCard` 分区布局，不依赖对话框。

对话框既有模式：与页面 **colocate** 在同一子目录（`Pages/Clusters/AddClusterDialog.razor` 等），不存在 `Pages/Dialogs/` 目录。`_Imports.razor` 按子目录逐个添加 `@using`。

导航菜单位于 `Drawer.razor`（非 `MainLayout.razor`），当前含 `/clusters`、`/nodes`、`/testpage` 三个 `MudNavLink`。

`KubernetesClient 19.0.2` 提供 `KubernetesYaml.Serialize(V1ConfigMap)` 与 `KubernetesYaml.Deserialize<V1ConfigMap>(string)` 静态方法，可将 k8s 资源对象序列化为 YAML 字符串及从 YAML 反序列化，无需引入额外 NuGet 包。

## Goals / Non-Goals

**Goals:**

- 为已纳管集群提供 ConfigMap 的查看（列表 + 详情页）、新建（对话框）、结构化修改（编辑页）、YAML 修改（YAML 编辑页）、删除（确认对话框）能力。
- 遵循现有分层与编码规范：`[Inject]` 写在 `@code` 内、组件 colocate、`_Imports.razor` 按子目录添加 using。
- 可达性判断复用 `ClusterService.GetClusterDetailAsync` + `IsReachable` 模式，`ConfigMapService` 不做容错 catch。
- 列表页布局复用 `Nodes.razor` 双栏模式（左侧集群选择树 + 右侧内容区）。
- 详情页与编辑页采用独立页面（非对话框），Data 键值对以 `MudTabs Position="Position.Left"` 左侧垂直页签布局展示。
- YAML 编辑页提供 YAML 定义编辑能力（`KubernetesYaml.Deserialize` 反序列化后提交）。
- 删除 ConfigMap 需确认对话框，防止误删。
- 命名空间下拉切换后即时刷新列表（`ValueChanged` 回调）。
- 权限：查看对所有登录用户开放；新建/编辑/编辑YAML/删除仅 `Admin`；编辑页与 YAML 编辑页仅 `Admin` 可访问。

**Non-Goals:**

- 不持久化 ConfigMap 到本地 SQLite——ConfigMap 始终是集群上的实时资源。
- 不支持 `binaryData` 字段——仅处理文本类 `data`。
- 不支持跨集群批量操作。
- 不支持 `labels` / `annotations` / `immutable` 字段的结构化编辑（YAML 编辑可间接修改这些字段）。
- 不抽取共享 `KubernetesClientFactory`——在 `ConfigMapService` 内第三次复制 `BuildConfig`，后续统一重构。
- 不新增 EF 迁移——不新增实体/DbSet，不改 `AppDbContext`。
- 新建 ConfigMap 保持对话框形式，不改为独立页面。
- 详情页与结构化编辑页不展示 YAML 定义——YAML 仅在独立的 YAML 编辑页中展示与编辑。

## Decisions

### D1: `ConfigMapService` 作为独立 Scoped 服务，与 `ClusterNodeService` 平级

**选择：** 新增 `Services/ConfigMapService.cs`，主构造函数注入 `ClusterRepository`，不注入 `ILogger`。

**理由：** ConfigMap 是独立的 k8s 资源维度，与节点管理平级。`ClusterNodeService` 也仅注入 `ClusterRepository` 且无 logger，保持一致。

**替代方案：** 将 ConfigMap 操作方法加到 `ClusterNodeService` 中——拒绝，因为 `ClusterNodeService` 语义是"节点维度"，混入 ConfigMap 破坏单一职责。

### D2: 错误处理跟随现有模式——`ConfigMapService` 不 catch，可达性由 `ClusterService` 提供

**选择：** `ConfigMapService` 的所有方法不做 try/catch，异常直接上抛。页面先调 `ClusterService.GetClusterDetailAsync(id)` 获取 `IsReachable`，仅在可达时调 `ConfigMapService`，页面层 try/catch + `Snackbar` 提示。

**理由：** 与 `Nodes.razor` + `ClusterNodeService` 的实际实现完全一致。不引入 `ConfigMapListResult { Items, IsReachable, ErrorMessage }` 新类型，避免增加模式复杂度。

**替代方案：** `ConfigMapService` 自包含 catch 并返回带 `IsReachable` 的结果对象——拒绝，因为这与现有模式不一致，且 `ClusterService.GetClusterDetailAsync` 已提供可达性判断，重复 catch 是冗余的。

### D3: 列表页采用双栏布局，复用 `Nodes.razor` 模式

**选择：** `ConfigMaps.razor` 采用 `MudGrid` 双栏：左侧 `MudTreeView` 集群选择树（按分组折叠，`md="3" lg="2"`）+ 右侧内容区（`md="9" lg="10"`）。双路由 `/configmaps` 与 `/configmaps/{ClusterId:int}`，选择集群后 `NavigateTo($"/configmaps/{id}")` 统一 URL。

**理由：** ConfigMap 与节点管理同属"集群维度下的资源管理"类别，用户交互模式一致（选集群 → 看资源），视觉一致性降低学习成本。

**替代方案：** 单栏布局 + 顶部 `MudSelect` 下拉选择集群——拒绝，因为偏离已建立的双栏模式且未有不需常驻集群切换栏的合理理由。

### D4: 组件 colocate 在 `Pages/ConfigMaps/` 目录

**选择：** `CreateConfigMapDialog.razor`、`ConfigMapDetail.razor`、`EditConfigMap.razor`、`EditConfigMapYaml.razor` 均放在 `Components/Pages/ConfigMaps/` 下，与 `ConfigMaps.razor` 同目录。

**理由：** 现有组件全部 colocate（`Pages/Clusters/` 下 4 个对话框），不存在 `Pages/Dialogs/` 目录。保持一致。

### D5: 导航入口加在 `Drawer.razor`，非 `MainLayout.razor`

**选择：** 在 `Drawer.razor` 的 `MudNavMenu` 中、`/nodes` 之后新增 `<MudNavLink Href="/configmaps" Icon="@Icons.Material.Filled.Settings" Match="NavLinkMatch.Prefix">配置管理</MudNavLink>`。

**理由：** `MainLayout.razor` 不含 `MudNavMenu`，导航菜单实际在 `Drawer.razor` 中。

### D6: `_Imports.razor` 新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`

**选择：** 在 `_Imports.razor` 中 `@using MultiClusterMgmtSys.Components.Pages.Nodes` 之后添加 ConfigMaps using。

**理由：** 现有 `_Imports.razor` 按子目录逐个声明 using（`Pages.Clusters`、`Pages.Nodes`），无全局 `Pages.Dialogs` using。新子目录必须添加对应 using，否则 `DialogService.ShowAsync<CreateConfigMapDialog>()` 无法解析组件。

### D7: ConfigMap 详情、结构化修改、YAML 修改使用独立页面，非对话框

**选择：** 新建 `ConfigMapDetail.razor`（路由 `/configmaps/{ClusterId:int}/{Namespace}/{Name}`）、`EditConfigMap.razor`（路由 `.../edit`）、`EditConfigMapYaml.razor`（路由 `.../yaml`），不使用对话框。新建场景保持 `CreateConfigMapDialog.razor` 对话框形式。删除使用确认对话框（`DialogService.ShowMessageBoxAsync`）。

**理由：** 页签式键值布局与 YAML 编辑需要较大的水平空间，对话框在键名较长或 YAML 行较长时仍显拥挤。独立页面可利用全宽，且支持浏览器前进/后退导航与 URL 分享。`NodeDetail.razor` 已建立独立详情页的先例。新建场景下键值对从零开始添加，对话框交互已足够。删除操作仅需确认，对话框合适。

**替代方案：** 扩大对话框尺寸并内部使用页签——拒绝，因为对话框无法支持 URL 导航/分享，且 YAML 内容在对话框内滚动体验差。

### D8: 修改采用 `ReplaceNamespacedConfigMapAsync`（全量替换 data）

**选择：** `UpdateConfigMapAsync` 先 `ReadNamespacedConfigMapAsync` 取回原 `V1ConfigMap`（保留 `Uid`/`ResourceVersion`），替换其 `Data` 字段后调 `ReplaceNamespacedConfigMapAsync`。

**理由：** k8s 对 ConfigMap 的更新建议全量替换 `data`，避免 strategic merge patch 的歧义。保留 `ResourceVersion` 可让 k8s 检测并发冲突（409）。

### D9: `BuildConfig` 第三次复制（v1 务实，后续统一重构）

**选择：** 在 `ConfigMapService` 内复制 `BuildConfig` 私有方法（与 `ClusterNodeService.BuildConfig` 一致，`private static`）。

**理由：** v1 聚焦功能交付，抽取 `KubernetesClientFactory` 是跨服务重构，应单独评估。三次复制是已知技术债，记录在 Non-Goals 中。

**替代方案：** 现在就抽取 `KubernetesClientFactory`——推迟，因为重构 `ClusterService` + `ClusterNodeService` + `ConfigMapService` 三处调用方属于独立变更，不应混入功能开发。

### D10: 命名空间列表返回 `List<string>`，不建 `NamespaceViewModel`

**选择：** `GetNamespacesAsync` 返回 `Task<List<string>>`，不引入 `NamespaceViewModel`。

**理由：** 命名空间仅有 name 一个字段，包装为 ViewModel 是过度设计。`MudSelect<string>` 直接绑定 `List<string>` 即可。

### D11: Data 键值对使用 MudTabs 左侧垂直页签布局

**选择：** 详情页与编辑页的 Data 区域使用 `MudTabs Position="Position.Left"`，左侧垂直页签栏，每个键一个 `MudTabPanel`，页签文本为键名，面板内容为该键的值。

- **详情页**：`MudTabPanel` 内为只读 `MudTextField`（`ReadOnly` + 等宽字体 + 多行）。不展示 YAML 页签。
- **编辑页**：`MudTabPanel` 内为可编辑 `MudTextField`（等宽字体 + 多行）。每个页签可关闭（`Closeable="true"`）用于删除键；页签栏有"添加键"按钮用于新增键。新增键时弹出简单输入框收集键名，创建后自动选中新页签。不展示 YAML 页签。

**理由：** 左侧垂直页签布局将键名列表纵向排列在左侧，点击即切换右侧值面板，键名与值的空间分配更合理。键数量多时左侧页签栏可垂直滚动，比顶部水平页签更高效（顶部页签在键多时需水平滚动，键名截断不易辨识）。

**替代方案：** 顶部水平页签（`Position="Position.Top"`）——拒绝，因为键名长度不一，顶部页签水平排列时截断严重，且键数量多时水平滚动体验差。

### D12: YAML 编辑通过独立页面，KubernetesYaml.Serialize/Deserialize 实现

**选择：** 新增 `EditConfigMapYaml.razor`（路由 `.../yaml`），展示 ConfigMap 的 YAML 定义（由 `ConfigMapDetailViewModel.Yaml` 字段提供，`KubernetesYaml.Serialize` 生成）并允许编辑。保存时通过 `KubernetesYaml.Deserialize<V1ConfigMap>(yaml)` 反序列化为 `V1ConfigMap`，先 `Read` 取回原对象保留 `ResourceVersion`，用反序列化结果替换后调 `ReplaceNamespacedConfigMapAsync`。详情页与结构化编辑页不展示 YAML。

**理由：** YAML 编辑是独立的编辑模式，与结构化键值编辑互补。独立页面提供全宽 YAML 编辑区，等宽字体多行文本框体验好。`KubernetesYaml.Deserialize` 是 KubernetesClient 内置方法，与 `kubectl apply -f` 一致。详情页与结构化编辑页聚焦键值对展示与编辑，不混入 YAML 视图，保持页面职责单一。

**替代方案：** 在详情页增加 YAML 只读页签——拒绝，因为用户需求明确为详情页不展示 YAML，YAML 仅用于编辑场景。

### D13: 命名空间即时刷新改用 ValueChanged 回调

**选择：** `ConfigMaps.razor` 的命名空间 `MudSelect` 使用 `Value="selectedNamespace" ValueChanged="OnNamespaceValueChanged"`，在 `OnNamespaceValueChanged` 中先更新 `selectedNamespace` 再调 `ListConfigMapsAsync` 重新拉取。

**理由：** Blazor 中 `@bind-Value` 内部使用 `ValueChanged`，额外添加 `@onchange` 不会可靠触发（MudSelect 的变更走 `ValueChanged` 而非 DOM `onchange`）。直接使用 `ValueChanged` 回调是 MudBlazor 组件的正确用法。

**替代方案：** 使用 `@bind-Value:after="OnNamespaceChanged"`——可行但语义不如直接 `ValueChanged` 清晰，且 `@bind-Value:after` 在某些 MudBlazor 版本中行为不一致。

### D14: 编辑页与 YAML 编辑页权限控制使用页面级 [Authorize(Roles="Admin")]

**选择：** `EditConfigMap.razor` 与 `EditConfigMapYaml.razor` 使用 `@attribute [Authorize(Roles="Admin")]`，非 Admin 用户访问编辑路由时被认证中间件重定向至 `/access-denied`。列表页的"编辑"、"编辑YAML"、"删除"按钮用 `AuthorizeView Roles="Admin"` 包裹（控制按钮可见性）。

**理由：** 双重保护——按钮不可见防止非 Admin 用户发现入口，页面级 `[Authorize Roles]` 防止直接输入 URL 绕过。与系统现有认证配置一致（`LoginPath`、`AccessDeniedPath` 已在 `Program.cs` 中配置）。

### D15: ConfigMap 删除使用确认对话框

**选择：** 列表页"删除"按钮点击后调 `DialogService.ShowMessageBoxAsync` 弹出确认对话框，用户确认后调 `ConfigMapService.DeleteConfigMapAsync(clusterId, name, ns)`（内部调 `DeleteNamespacedConfigMapAsync`），成功后 `Snackbar` 提示 + 列表刷新。

**理由：** 删除是不可逆操作，需二次确认防止误删。`ShowMessageBoxAsync` 是 MudBlazor 内置的确认对话框，交互简单。删除后列表刷新确保数据一致。

**替代方案：** 独立删除确认页——拒绝，因为删除操作仅需确认，不需要额外信息输入，对话框足够。

### D16: 列表操作列不设"查看"按钮，点击名称查看

**选择：** 列表操作列仅提供"编辑"、"编辑YAML"、"删除"三个按钮（均 Admin），不设"查看"按钮。查看通过点击名称列跳转详情页实现。

**理由：** 名称列已可点击跳转详情页，额外设"查看"按钮冗余。操作列聚焦写操作（编辑/YAML编辑/删除），读操作通过名称点击完成，职责清晰。

## Risks / Trade-offs

- **[BuildConfig 三次复制]** → 接受此技术债，后续创建独立变更抽取 `KubernetesClientFactory`。三次复制的内容完全相同，维护风险可控。
- **[大集群 ConfigMap 数量多]** → `MudTable` 默认 `RowsPerPage=10`，支持 10/20/50 切换，前端分页。列表请求本身是全量拉取后前端过滤，超大集群（数千 ConfigMap）可能加载慢——接受，后续可考虑 k8s label selector 或服务端分页。
- **[修改时并发冲突]** → `ReplaceNamespacedConfigMapAsync` 携带 `ResourceVersion`，k8s 会检测并发修改并返回 409。页面 catch 后提示"资源已被他人修改，请刷新后重试"。
- **[路由参数含特殊字符]** → Namespace 和 Name 均为 k8s 资源名，遵循 DNS 子域命名规则（小写字母/数字/`-`/`.`），不含 `/`、`?`、`#` 等 URL 特殊字符，无需 URL 编码。Blazor 路由参数默认匹配到下一个 `/`，可安全承载。
- **[YAML 编辑反序列化失败]** → 用户提交格式不合法的 YAML 时，`KubernetesYaml.Deserialize` 抛异常，页面 catch 后 `Snackbar` 提示"YAML 格式错误: {ex.Message}"，编辑页保持打开，用户可修正后重试。
- **[YAML 编辑覆盖结构化编辑]** → YAML 编辑与结构化编辑是两种独立编辑模式，用户可能在一种模式编辑后切换到另一种模式，此时页面重新加载会获取最新集群数据，不会丢失未保存的更改（因为未保存的更改在页面切换时丢弃）。
- **[编辑页页签关闭删除键]** → 用户误关页签会丢失键。编辑页在页签关闭时不立即标记删除，而是从 `dataEntries` 列表移除；用户需点"保存"才提交到集群。关闭页签后可通过浏览器后退/刷新恢复原始数据。
- **[删除操作不可逆]** → 删除前弹出确认对话框，显示 ConfigMap 名称，用户需明确点击"删除"才执行。k8s 删除 ConfigMap 后无法恢复，但 ConfigMap 通常由 GitOps/部署配置管理，可重新应用。
