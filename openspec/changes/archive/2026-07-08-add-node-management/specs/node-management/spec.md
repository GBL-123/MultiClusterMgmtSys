## ADDED Requirements

### Requirement: 节点列表查看

系统 SHALL 在 `/nodes` 与 `/nodes/{ClusterId:int}` 路由下提供节点列表页，采用双栏布局：左侧集群选择树 + 右侧节点表格。

#### Scenario: 从侧边栏进入未选集群

- **WHEN** 用户从侧边栏「节点管理」进入 `/nodes`（无 ClusterId）
- **THEN** 左侧显示集群选择树（`MudTreeView` 按分组折叠），右侧显示"请从左侧选择一个集群"空状态

#### Scenario: 选择集群后加载节点列表

- **WHEN** 用户在左侧集群选择树中点击一个 Online 集群
- **THEN** URL 跳转至 `/nodes/{ClusterId}`，右侧先调 `ClusterService.GetClusterDetailAsync` 确认可达，再调 `ClusterNodeService.GetClusterNodesAsync` 加载节点列表并渲染 `MudTable`（名称/状态/角色/Kubelet版本/操作系统/内网IP）

#### Scenario: 节点名称搜索

- **WHEN** 用户在搜索框输入关键词
- **THEN** `filteredNodes` 计算属性实时过滤，显示名称包含关键词的节点（前端过滤，不发请求）

#### Scenario: 刷新节点列表

- **WHEN** 用户点击刷新按钮
- **THEN** 重新调 `LoadNodesAsync(ClusterId)` 拉取当前集群的节点列表

#### Scenario: 节点名称点击下钻

- **WHEN** 用户点击表格中某节点的名称
- **THEN** 跳转至 `/nodes/{ClusterId}/{NodeName}` 节点详情页

### Requirement: 集群选择树

系统 SHALL 在节点列表页左侧提供集群选择树，按分组折叠展示全部集群，点击集群叶子节点跳转该集群的节点列表。

#### Scenario: 分组树渲染

- **WHEN** 节点列表页加载
- **THEN** 左侧 `MudTreeView` 按 `GroupName` 分组（"未分组"排最后），一级目录为分组名（`Folder`/`FolderOpen` 图标，默认展开），二级为集群名（`EndIcon` 为状态图标：Online=CheckCircle/Offline=Cancel/Unknown=RemoveCircle）

#### Scenario: 集群搜索

- **WHEN** 用户在左侧搜索框输入集群名称关键词
- **THEN** 集群树实时过滤，仅显示名称包含关键词的集群

#### Scenario: 离线集群可点击

- **WHEN** 用户点击离线集群
- **THEN** 跳转至 `/nodes/{id}`，右侧显示"集群不可达，无法获取节点列表"

### Requirement: 节点详情页

系统 SHALL 在 `/nodes/{ClusterId:int}/{NodeName}` 路由下提供节点详情页，多卡片分块展示节点全部信息。

#### Scenario: 查看节点详情

- **WHEN** 用户从节点列表点击节点名称进入详情页
- **THEN** `NodeDetail.razor` 调 `ClusterNodeService.GetNodeDetailAsync(ClusterId, NodeName)`，展示概要卡片 + 调度信息 + 元数据 + 资源容量（Capacity/Allocatable 并排表格）+ 地址列表 + 条件列表 + 污点列表 + 标签 + 注解 + 系统信息 + 操作卡片

#### Scenario: 节点不存在

- **WHEN** 节点已被删除（`ReadNodeAsync` 抛 404）
- **THEN** 页面 try/catch 捕获，`Snackbar` 提示"加载节点详情失败"，详情页显示"未找到该节点" + 返回节点列表按钮

#### Scenario: 集群离线

- **WHEN** 集群 `Status == Offline`
- **THEN** `GetNodeDetailAsync` 直接返回 `IsReachable = false` 的空详情（不发起 k8s 调用），详情页显示"未找到该节点"

#### Scenario: 返回节点列表

- **WHEN** 用户点击"返回节点列表"按钮
- **THEN** 跳转至 `/nodes/{ClusterId}`

### Requirement: 从集群详情下钻

系统 SHALL 在 `ClusterDetail.razor` 的节点列表卡片中提供下钻入口。

#### Scenario: 查看全部节点

- **WHEN** 用户在集群详情页点击节点列表卡片的"查看全部"按钮
- **THEN** 跳转至 `/nodes/{Id}` 节点管理页

#### Scenario: 集群详情节点名称下钻

- **WHEN** 用户在集群详情页节点列表中点击某节点名称
- **THEN** 跳转至 `/nodes/{Id}/{nodeName}` 节点详情页

### Requirement: 导航入口

系统 SHALL 在侧边栏 `Drawer.razor` 的 `MudNavMenu` 中提供「节点管理」入口。

#### Scenario: 侧边栏导航

- **WHEN** 用户查看侧边栏
- **THEN** `MudNavLink Href="/nodes" Icon="DeviceHub" Match="NavLinkMatch.Prefix"` 显示「节点管理」，`/nodes` 和 `/nodes/{id}` 和 `/nodes/{id}/{name}` 均高亮该项

### Requirement: 页面访问控制

系统 SHALL 要求用户登录后才能访问节点管理页面。节点管理为只读观测，不限制角色（Admin 与 Guest 均可查看）。

#### Scenario: 需登录

- **WHEN** 未登录用户访问 `/nodes` 或 `/nodes/{ClusterId}` 或 `/nodes/{ClusterId}/{NodeName}`
- **THEN** 页面 `@attribute [Authorize]` 强制认证，未登录触发跳转登录页

#### Scenario: Admin 与 Guest 均可查看

- **WHEN** Guest 用户访问节点管理页
- **THEN** 页面正常渲染，无 `AuthorizeView Roles="Admin"` 限制（只读页面，无写操作按钮）
