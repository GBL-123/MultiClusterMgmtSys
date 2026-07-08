## Context

系统最初为简单集群列表，`ClusterService` 既查 DB 又调 k8s 又返回实体，无分层、无 ViewModel、无过滤、无编辑、无详情页、无主题系统。需要重构为可维护的分层架构并增加完整的管理能力。

现有目录结构：页面按功能分子目录（`Pages/Clusters/`、`Pages/Nodes/`），对话框与页面 colocate（无 `Pages/Dialogs/` 目录）。`MainLayout.razor` 委托给 `<AppBar>` + `<Drawer>` 组件，导航菜单在 `Drawer.razor` 中。

## Goals / Non-Goals

**Goals:**

- 分层架构：`Repository（返回实体）→ Service（返回 ViewModel）→ Razor（消费 ViewModel）`，单向依赖。
- 集群表格视图 + 多条件 AND 过滤 + 表头排序。
- 添加/编辑集群，支持 KubeConfig 与 Token 两种连接方式。
- 集群详情页（基本信息 + 连接信息 + 节点列表 + 操作）。
- 分组管理（新建/删除/列表）。
- Indigo-Blue UI 主题系统（浅色/暗色双模式 + localStorage 持久化 + OS 偏好跟随）。

**Non-Goals:**

- 批量操作（批量刷新/删除/移动分组）。
- 一键刷新全部/自动定时刷新。
- 集群导出。
- 节点管理（独立变更，见 `add-node-management`）。
- 节点写操作（封锁/排空/污点）。

## Decisions

### D1: 分层架构——Repository 返回实体，Service 返回 ViewModel

**选择：** `Daos/ClusterRepository`、`Daos/GroupRepository` 返回 `ClusterInfo`/`ClusterGroup` 实体；`Services/ClusterService`、`Services/GroupService` 编排 Repository + k8s 调用，返回 ViewModel；`ViewModels/Mappings/` 扩展方法映射实体→VM。

**理由：** 单向依赖，Razor 不引用 `Models/`，Service 不返回实体，ViewModel 是纯 POCO 契约。

### D2: 对话框 colocate 在 `Pages/Clusters/` 目录

**选择：** `AddClusterDialog.razor`、`EditClusterDialog.razor`、`CreateGroupDialog.razor`、`ManageGroupsDialog.razor` 均放在 `Pages/Clusters/` 下。

**理由：** 对话框与页面同目录，`_Imports.razor` 按子目录添加 `@using`，`DialogService.ShowAsync<T>()` 可解析组件。不建独立 `Pages/Dialogs/` 目录。

### D3: `BuildConfig` 私有方法（非 `BuildK8sClient`）

**选择：** `ClusterService` 内私有方法 `BuildConfig(ClusterInfo cluster)`，按 `ConnectionType` 分支：KubeConfig 走 `BuildConfigFromConfigFile(stream)`，Token 走手动 `KubernetesClientConfiguration { Host, AccessToken, SkipTlsVerify }`。

**理由：** 方法名 `BuildConfig` 与 `KubernetesClientConfiguration` 语义一致。k8s 连接逻辑暂留 Service 内，不单独成层。

### D4: `ThemeManager` 在 `Components/Theme/`（非 `Services/ThemeService.cs`）

**选择：** `Components/Theme/ThemeManager.cs`，Scoped 服务，注入 `IJSRuntime`。

**理由：** 主题是 UI 层关注点，放在 `Components/Theme/` 与 `MainLayout`/`AppBar` 同层更内聚。`Services/` 放业务编排服务。

### D5: `MainLayout.razor` 委托给 AppBar/Drawer 组件

**选择：** `MainLayout.razor` 包含 `MudThemeProvider` 绑定 + `<AppBar>` + `<Drawer>` + `<MudMainContent>`。主题切换按钮在 `AppBar.razor`，导航菜单在 `Drawer.razor`。

**理由：** 组件化拆分，`MainLayout` 只做外壳编排，AppBar/Drawer 各自独立。

### D6: 节点列表 ViewModel 命名 `ClusterNodeViewModel`（非 `ClusterNodeInfo`）

**选择：** 节点列表项为 `ViewModels/ClusterNodeViewModel.cs`，放在 `ViewModels/` 目录。

**理由：** 遵循分层约定——所有 Service 与前端之间的数据契约统一命名 `XxxViewModel`，放 `ViewModels/`。`ClusterNodeInfo` 暗示是 `Models/` 实体，但节点数据不持久化、不落库，它是纯运行时 ViewModel。

### D7: `GetClusterNodesAsync` 后续移至 `ClusterNodeService`

**选择：** 节点列表拉取方法最初在 `ClusterService` 中，后续节点管理变更时抽取为独立 `ClusterNodeService`。

**理由：** `ClusterService` 职责是集群 CRUD + 探测，节点维度查询独立成服务更清晰。此变更在 `add-node-management` 中完成。

### D8: 过滤为前端实时计算（非防抖）

**选择：** `filteredClusters` 为 `@code` 中的计算属性，每次 `searchName`/`filterGroupId`/`filterStatus`/`filterVersion`/`filterStartDate`/`filterEndDate` 变化时重新计算，无防抖。

**理由：** 集群数量通常在数十级别，前端 LINQ 过滤即时完成，无需防抖或服务端过滤。

### D9: 敏感数据策略

**选择：** 列表 ViewModel（`ClusterViewModel`）不含 `KubeConfig`/`Token`。详情 ViewModel（`ClusterDetailViewModel`）也不含密文。编辑预填 ViewModel（`ClusterEditViewModel`）含密文用于表单回填。详情页"显示密文"按钮调 `GetClusterForEditAsync(id)` 获取含密文的 VM，前端默认掩码 + 点击显示。

**理由：** 避免普通详情请求把凭据带进内存/日志。密文仅在明确需要时（Admin 点击显示）才加载。

### D10: UI 主题——localStorage 持久化 + OS 偏好跟随

**选择：** `ThemeManager.InitializeAsync()` 在 `MainLayout.OnAfterRenderAsync(firstRender)` 中调用。顺序：`localStorage` 保存值 > OS `prefers-color-scheme` > 默认 false。用户显式切换后设 `ObserveSystemDarkModeChange = false` 锁定选择。`MudThemeProvider.ObserveSystemDarkModeChange` 绑定到该属性。

**理由：** 首次访问跟随 OS 体验好；用户显式选择后锁定避免被 OS 变更覆盖。

## Risks / Trade-offs

- **[敏感信息明文存储]** → `KubeConfig`/`Token` 以明文 TEXT 存于 SQLite。系统定位为开发/内部工具，不做加密。详情页展示默认掩码，需 Admin 主动点击显示。
- **[模型变更须重建库]** → `ClusterInfo` 新增字段触发 schema 变更，必须删除 `clusters.db*` 重跑（无迁移，`EnsureCreated()` 建表）。
- **[InputFile 大小限制]** → kubeconfig 文件上传限制 256KB，防止滥用。
- **[Token 模式 TLS]** → `SkipTlsVerify` 默认 true（自签证书常见），UI 提示安全风险。
- **[版本筛选选项来源]** → 来自当前已加载集群的 distinct `Version`，集群刷新后需重新加载列表以更新选项。
