# Cluster Management

## Purpose

集群 CRUD、分组管理、多维度过滤排序、详情页、KubeConfig/Token 双连接方式、Indigo-Blue UI 主题系统。

## Requirements

### Requirement: 集群列表展示

系统 SHALL 在 `/clusters` 路由下以 `MudTable` 表格展示全部已纳管集群，列含名称、状态、版本、节点数、分组、API Server、创建时间、操作。名称列可点击进入详情页，可排序列用 `MudTableSortLabel`。

#### Scenario: 加载集群列表

- **WHEN** 用户进入 `/clusters`
- **THEN** `Clusters.razor` 调 `ClusterService.GetClustersAsync()` 加载全部集群，渲染 `MudTable` 表格

#### Scenario: 表头排序

- **WHEN** 用户点击可排序列的表头（名称/状态/版本/节点数/创建时间）
- **THEN** `MudTableSortLabel` 切换升降序，表格行按该列排序

#### Scenario: 名称点击进入详情

- **WHEN** 用户点击表格中某集群的名称
- **THEN** 跳转至 `/clusters/{Id}` 详情页

### Requirement: 多条件过滤

系统 SHALL 提供工具栏支持名称搜索、分组、状态、版本、创建时间范围六个条件 AND 组合过滤，并提供重置按钮。

#### Scenario: 名称搜索

- **WHEN** 用户在名称搜索框输入关键词
- **THEN** `filteredClusters` 计算属性实时过滤，显示名称包含关键词（`StringComparison.OrdinalIgnoreCase`）的集群

#### Scenario: 分组过滤

- **WHEN** 用户在分组下拉中选择某个分组
- **THEN** 列表仅显示该分组的集群；选择"全部分组"显示全部

#### Scenario: 状态过滤

- **WHEN** 用户在状态下拉中选择在线/离线/未知
- **THEN** 列表仅显示该状态的集群

#### Scenario: 版本过滤

- **WHEN** 用户在版本下拉中选择某版本或"未知"
- **THEN** 列表仅显示该版本的集群（"未知"匹配 `Version == null`）

#### Scenario: 创建时间范围过滤

- **WHEN** 用户设置开始时间和/或结束时间
- **THEN** 列表仅显示 `CreatedAt` 在闭区间 `[开始时间当日 00:00, 结束时间次日 00:00)` 内的集群

#### Scenario: 重置筛选

- **WHEN** 用户点击"重置"按钮
- **THEN** 清空名称、分组、状态、版本、开始时间、结束时间六个条件

#### Scenario: 筛选结果为空

- **WHEN** 过滤后无匹配集群且原列表非空
- **THEN** 显示"没有符合当前筛选条件的集群" + "重置筛选"按钮

### Requirement: 添加集群

系统 SHALL 允许 Admin 用户通过对话框添加集群，支持 KubeConfig 与 Token 两种连接方式，提交后立即探测连通性。

#### Scenario: KubeConfig 方式添加

- **WHEN** Admin 用户选择 KubeConfig 方式，填写名称、分组（可选）、粘贴 kubeconfig 文本或上传文件，提交
- **THEN** 系统建集群记录（状态 Unknown），调 `BuildConfig` + `ProbeAsync` 探测，成功则状态 Online 并回填 Version/NodeCount/ApiServer/LastCheckedAt，失败则状态 Offline（不阻断添加）

#### Scenario: Token 方式添加

- **WHEN** Admin 用户选择 Token 方式，填写名称、API Server、Bearer Token、SkipTlsVerify，提交
- **THEN** 系统建集群记录并探测，逻辑同 KubeConfig 方式

#### Scenario: 文件上传

- **WHEN** 用户选择上传文件方式提供 kubeconfig
- **THEN** `InputFile` 接受 `.yaml/.yml/.config`，限制 256KB，读取为文本填入 `KubeConfig` 字段

#### Scenario: 非 Admin 不可见添加按钮

- **WHEN** 非 Admin 用户查看集群列表
- **THEN** "添加集群"按钮不渲染（`AuthorizeView Roles="Admin"` 包裹）

### Requirement: 编辑集群

系统 SHALL 允许 Admin 用户编辑集群信息（名称、分组、连接配置），连接配置变更时保存后重新探测。

#### Scenario: 编辑集群

- **WHEN** Admin 用户点击操作列"编辑"按钮，`EditClusterDialog` 打开并预填当前值
- **THEN** 用户可修改名称、分组、切换连接方式、更新连接配置，保存后若连接配置变更则重新探测

#### Scenario: 仅改名不触发探测

- **WHEN** 用户仅修改名称或分组，未改连接配置
- **THEN** 保存后不触发 `ProbeAsync`，直接更新记录

### Requirement: 删除集群

系统 SHALL 允许 Admin 用户删除集群，删除前二次确认。

#### Scenario: 删除集群

- **WHEN** Admin 用户点击"删除"按钮
- **THEN** 弹出 `ShowMessageBoxAsync` 二次确认，确认后 `DeleteClusterAsync` 删除记录，`Snackbar` 提示"集群已删除"，列表刷新

### Requirement: 刷新集群状态

系统 SHALL 允许 Admin 用户手动刷新单个集群的连通性状态。

#### Scenario: 刷新集群状态

- **WHEN** Admin 用户点击操作列"刷新"按钮
- **THEN** `RefreshClusterStatusAsync` 重新探测，更新 Status/Version/NodeCount/LastCheckedAt，`Snackbar` 提示"集群状态已刷新"，列表刷新

### Requirement: 集群详情页

系统 SHALL 在 `/clusters/{Id:int}` 路由下提供集群详情页，展示基本信息、连接信息、节点列表与操作区。

#### Scenario: 查看详情

- **WHEN** 用户从列表点击集群名称进入详情页
- **THEN** 加载 `GetClusterDetailAsync(id)`，展示基本信息卡片（名称/状态/版本/节点数/分组/API Server/创建时间/最后检测时间）+ 连接信息卡片 + 节点列表卡片 + 操作区

#### Scenario: 节点列表实时拉取

- **WHEN** 集群 `IsReachable == true`
- **THEN** 详情页调 `GetClusterNodesAsync` 实时拉取节点列表，表格展示名称/状态/角色/Kubelet版本/OS/内网IP

#### Scenario: 离线集群节点列表降级

- **WHEN** 集群 `IsReachable == false`
- **THEN** 节点列表区显示"集群不可达，无法获取节点列表"，不发起 k8s 请求

#### Scenario: 显示连接密文

- **WHEN** Admin 用户点击"显示密文"按钮
- **THEN** 调 `GetClusterForEditAsync(id)` 加载含密文的 `ClusterEditViewModel`，以密码态 `MudTextField` 展示，可切换明文/密文

### Requirement: 分组管理

系统 SHALL 支持集群分组的创建、删除与列表查看。

#### Scenario: 新建分组

- **WHEN** Admin 用户点击"新建分组"按钮，填写分组名称，提交
- **THEN** `GroupService.AddGroupAsync` 创建分组，对话框关闭，父页面刷新分组下拉

#### Scenario: 分组管理对话框

- **WHEN** 用户点击"分组管理"按钮
- **THEN** `ManageGroupsDialog` 打开，展示分组列表（名称/集群数/操作），Admin 可删除分组（二次确认），可新建分组

#### Scenario: 删除分组后筛选重置

- **WHEN** Admin 删除当前正在筛选的分组后关闭对话框
- **THEN** 父页面刷新分组下拉与列表，当前 `filterGroupId` 不存在时置空（重置为"全部分组"）

### Requirement: UI 主题系统

系统 SHALL 提供 Indigo-Blue 主题（浅色/暗色双模式），用户可切换并持久化偏好到 `localStorage`，首次访问跟随 OS `prefers-color-scheme`。

#### Scenario: 切换主题

- **WHEN** 用户点击 AppBar 主题切换按钮
- **THEN** `ThemeManager.ToggleDarkModeAsync()` 切换 `IsDarkMode`，写入 `localStorage` 键 `mcm-theme-dark-mode`，设 `ObserveSystemDarkModeChange = false` 锁定选择

#### Scenario: 首次访问跟随 OS

- **WHEN** 首次访问（`localStorage` 无保存值）
- **THEN** `ThemeManager.InitializeAsync()` 保持 `ObserveSystemDarkModeChange = true`，`MudThemeProvider` 内置 `prefers-color-scheme` 跟踪自动决定暗色/浅色

#### Scenario: 刷新后保持偏好

- **WHEN** 用户刷新页面
- **THEN** `InitializeAsync()` 读取 `localStorage` 保存值，恢复上次选择的主题模式

### Requirement: 导航入口

系统 SHALL 在侧边栏 `Drawer.razor` 的 `MudNavMenu` 中提供「集群管理」入口。

#### Scenario: 侧边栏导航

- **WHEN** 用户查看侧边栏
- **THEN** `MudNavLink Href="/clusters" Icon="Hub" Match="NavLinkMatch.Prefix"` 显示「集群管理」，`/clusters` 和 `/clusters/{id}` 均高亮该项
