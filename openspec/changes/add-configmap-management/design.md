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

对话框既有模式：与页面 **colocate** 在同一子目录（`Pages/Clusters/AddClusterDialog.razor` 等），不存在 `Pages/Dialogs/` 目录。`_Imports.razor` 按子目录逐个添加 `@using`。

导航菜单位于 `Drawer.razor`（非 `MainLayout.razor`），当前含 `/clusters`、`/nodes`、`/testpage` 三个 `MudNavLink`。

## Goals / Non-Goals

**Goals:**

- 为已纳管集群提供 ConfigMap 的查看（列表 + 详情）、新建、修改能力。
- 遵循现有分层与编码规范：`[Inject]` 写在 `@code` 内、对话框 colocate、`_Imports.razor` 按子目录添加 using。
- 可达性判断复用 `ClusterService.GetClusterDetailAsync` + `IsReachable` 模式，`ConfigMapService` 不做容错 catch。
- 页面布局复用 `Nodes.razor` 双栏模式（左侧集群选择树 + 右侧内容区）。
- 权限：查看对所有登录用户开放；新建/修改仅 `Admin`。

**Non-Goals:**

- 不持久化 ConfigMap 到本地 SQLite——ConfigMap 始终是集群上的实时资源。
- 不支持 `binaryData` 字段——v1 仅处理文本类 `data`。
- 不支持删除 ConfigMap——留作后续扩展。
- 不支持 YAML 原文导入/导出——v1 以结构化表单编辑键值对。
- 不支持跨集群批量操作。
- 不支持 `labels` / `annotations` / `immutable` 字段编辑。
- 不抽取共享 `KubernetesClientFactory`——v1 在 `ConfigMapService` 内第三次复制 `BuildConfig`，后续统一重构。
- 不新增 EF 迁移——不新增实体/DbSet，不改 `AppDbContext`。

## Decisions

### D1: `ConfigMapService` 作为独立 Scoped 服务，与 `ClusterNodeService` 平级

**选择：** 新增 `Services/ConfigMapService.cs`，主构造函数注入 `ClusterRepository`，不注入 `ILogger`。

**理由：** ConfigMap 是独立的 k8s 资源维度，与节点管理平级。`ClusterNodeService` 也仅注入 `ClusterRepository` 且无 logger，保持一致。

**替代方案：** 将 ConfigMap 操作方法加到 `ClusterNodeService` 中——拒绝，因为 `ClusterNodeService` 语义是"节点维度"，混入 ConfigMap 破坏单一职责。

### D2: 错误处理跟随现有模式——`ConfigMapService` 不 catch，可达性由 `ClusterService` 提供

**选择：** `ConfigMapService` 的所有方法不做 try/catch，异常直接上抛。页面先调 `ClusterService.GetClusterDetailAsync(id)` 获取 `IsReachable`，仅在可达时调 `ConfigMapService`，页面层 try/catch + `Snackbar` 提示。

**理由：** 与 `Nodes.razor` + `ClusterNodeService` 的实际实现完全一致。不引入 `ConfigMapListResult { Items, IsReachable, ErrorMessage }` 新类型，避免增加模式复杂度。

**替代方案：** `ConfigMapService` 自包含 catch 并返回带 `IsReachable` 的结果对象——拒绝，因为这与现有模式不一致，且 `ClusterService.GetClusterDetailAsync` 已提供可达性判断，重复 catch 是冗余的。

### D3: 页面采用双栏布局，复用 `Nodes.razor` 模式

**选择：** `ConfigMaps.razor` 采用 `MudGrid` 双栏：左侧 `MudTreeView` 集群选择树（按分组折叠，`md="3" lg="2"`）+ 右侧内容区（`md="9" lg="10"`）。双路由 `/configmaps` 与 `/configmaps/{ClusterId:int}`，选择集群后 `NavigateTo($"/configmaps/{id}")` 统一 URL。

**理由：** ConfigMap 与节点管理同属"集群维度下的资源管理"类别，用户交互模式一致（选集群 → 看资源），视觉一致性降低学习成本。

**替代方案：** 单栏布局 + 顶部 `MudSelect` 下拉选择集群——拒绝，因为偏离已建立的双栏模式且未有不需常驻集群切换栏的合理理由。

### D4: 对话框 colocate 在 `Pages/ConfigMaps/` 目录

**选择：** `CreateConfigMapDialog.razor`、`EditConfigMapDialog.razor`、`ConfigMapDetailDialog.razor` 均放在 `Components/Pages/ConfigMaps/` 下，与 `ConfigMaps.razor` 同目录。

**理由：** 现有对话框全部 colocate（`Pages/Clusters/` 下 4 个对话框），不存在 `Pages/Dialogs/` 目录。保持一致。

### D5: 导航入口加在 `Drawer.razor`，非 `MainLayout.razor`

**选择：** 在 `Drawer.razor` 的 `MudNavMenu` 中、`/nodes` 之后新增 `<MudNavLink Href="/configmaps" Icon="@Icons.Material.Filled.Settings" Match="NavLinkMatch.Prefix">配置管理</MudNavLink>`。

**理由：** `MainLayout.razor` 不含 `MudNavMenu`，导航菜单实际在 `Drawer.razor` 中。

### D6: `_Imports.razor` 新增 `@using MultiClusterMgmtSys.Components.Pages.ConfigMaps`

**选择：** 在 `_Imports.razor` 中 `@using MultiClusterMgmtSys.Components.Pages.Nodes` 之后添加 ConfigMaps using。

**理由：** 现有 `_Imports.razor` 按子目录逐个声明 using（`Pages.Clusters`、`Pages.Nodes`），无全局 `Pages.Dialogs` using。新子目录必须添加对应 using，否则 `DialogService.ShowAsync<CreateConfigMapDialog>()` 无法解析组件。

### D7: ConfigMap 详情用对话框（非独立详情页）

**选择：** ConfigMap 详情通过 `ConfigMapDetailDialog.razor` 弹窗展示，不建独立详情页。

**理由：** ConfigMap 详情数据量小（仅元信息 + Data 键值对），对话框信息密度足够。节点管理用独立详情页是因为节点详情字段量大（容量/条件/污点/标签/注解/系统信息等 30+ 字段），弹窗承载不下。两者数据量差异决定了不同的展示方式。

**替代方案：** 独立详情页 `/configmaps/{ClusterId}/{Namespace}/{Name}`——拒绝，因为 ConfigMap 详情不需要链接分享场景，且 URL 中含 Namespace 和 Name 两个 string 参数使路由复杂化。

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

## Risks / Trade-offs

- **[BuildConfig 三次复制]** → v1 接受此技术债，后续创建独立变更抽取 `KubernetesClientFactory`。三次复制的内容完全相同，维护风险可控。
- **[ConfigMap 详情用对话框不可链接]** → ConfigMap 详情不需要链接分享场景（运维操作通常在当前会话内完成）。若后续有需求可升级为独立详情页。
- **[无 ConfigMap 删除功能]** → v1 明确不做。用户需求仅含查看/新建/修改。删除留作 v2 扩展（带二次确认）。
- **[大集群 ConfigMap 数量多]** → `MudTable` 默认 `RowsPerPage=10`，支持 10/20/50 切换，前端分页。列表请求本身是全量拉取后前端过滤，超大集群（数千 ConfigMap）可能加载慢——v1 接受，v2 可考虑 k8s label selector 或服务端分页。
- **[修改时并发冲突]** → `ReplaceNamespacedConfigMapAsync` 携带 `ResourceVersion`，k8s 会检测并发修改并返回 409。页面 catch 后提示"资源已被他人修改，请刷新后重试"。
