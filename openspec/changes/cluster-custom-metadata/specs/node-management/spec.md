## ADDED Requirements

### Requirement: 节点级 IP 手工录入

系统 SHALL 允许 Admin 用户为集群内某节点手工录入多类 IP（k8s IP/业务 IP/集群通信 IP），以 `(ClusterId, NodeName, Category)` 为业务键持久化，与 k8s 自动获取的节点地址分离存储。Guest 用户不可录入。

#### Scenario: 打开节点 IP 编辑对话框

- **WHEN** Admin 用户在节点详情页「手工 IP」卡片点击「编辑 IP」按钮
- **THEN** `NodeIpEditDialog.razor` 打开，预填该节点已持久化的 `ClusterNodeIp` 记录（每行：类别下拉 + IP 输入 + 备注 + 删除按钮），可动态增删行

#### Scenario: 保存节点 IP

- **WHEN** Admin 用户在对话框编辑 IP 行后点击保存
- **THEN** 系统以表单提交的列表为准，对 `(ClusterId, NodeName)` 下的 `ClusterNodeIp` 做 upsert + 删除（表单中不存在的旧记录删除），保存后刷新节点详情页「手工 IP」卡片

#### Scenario: 节点 IP 不触发 k8s 调用

- **WHEN** Admin 用户保存节点 IP
- **THEN** 仅持久化到 `ClusterNodeIp` 表，不调用 k8s API，不影响集群连通性状态

#### Scenario: 非 Admin 不可见编辑入口

- **WHEN** Guest 用户查看节点详情页
- **THEN** 「手工 IP」卡片可见（只读展示），但「编辑 IP」按钮不渲染（`AuthorizeView Roles="Admin"` 包裹）

#### Scenario: 节点 IP 唯一约束

- **WHEN** 同一 `(ClusterId, NodeName, Category)` 已存在记录
- **THEN** 保存时按业务键 upsert（更新而非插入），数据库唯一索引防止重复

### Requirement: 节点 IP 与 k8s 地址合并展示

系统 SHALL 在节点详情页与节点列表页合并展示 k8s 自动获取的节点地址与手工录入的 `ClusterNodeIp`，手工 IP 优先展示。

#### Scenario: 节点详情页手工 IP 卡片

- **WHEN** 节点详情页加载且该节点有持久化的 `ClusterNodeIp` 记录
- **THEN** 「手工 IP」卡片按 `NodeIpCategory` 分组展示（k8s IP/业务 IP/集群通信 IP），每组显示类别名 + IP + 备注；无记录时卡片显示「暂无手工 IP」

#### Scenario: 节点详情页 k8s 地址卡片保持独立

- **WHEN** 节点详情页加载
- **THEN** 现有「地址」卡片仍展示 k8s API 返回的 `node.status.addresses`（InternalIP/ExternalIP/Hostname 等），与「手工 IP」卡片分列，不合并

#### Scenario: 节点列表 IP 列合并展示

- **WHEN** 节点列表页加载节点列表
- **THEN** 系统一次性查询该集群全部 `ClusterNodeIp`，在内存按 `NodeName` 分组后与 k8s 节点列表合并；「节点 IP」列展示逻辑：有手工 IP 时按类别拼接 IP 值展示，无手工 IP 时回退到 k8s `InternalIP`

#### Scenario: 节点列表避免 N+1 查询

- **WHEN** 节点列表页加载
- **THEN** 手工 IP 仅发起一次批量查询（按 `ClusterId` 查全部 `ClusterNodeIp`），不在节点循环内逐个查询

## MODIFIED Requirements

### Requirement: 节点详情页

系统 SHALL 在 `/nodes/{ClusterId:int}/{NodeName}` 路由下提供节点详情页，多卡片分块展示节点全部信息，包括 k8s 实时数据与手工录入的节点 IP。

#### Scenario: 查看节点详情

- **WHEN** 用户从节点列表点击节点名称进入详情页
- **THEN** `NodeDetail.razor` 调 `ClusterNodeService.GetNodeDetailAsync(ClusterId, NodeName)` 与 `ClusterNodeIpService.GetNodeIpsAsync(ClusterId, NodeName)`，展示概要卡片 + 调度信息 + 元数据 + 资源容量 + 地址列表 + 手工 IP 卡片 + 条件列表 + 污点列表 + 标签 + 注解 + 系统信息 + 操作卡片

#### Scenario: 节点不存在

- **WHEN** 节点已被删除（`ReadNodeAsync` 抛 404）
- **THEN** 页面 try/catch 捕获，`Snackbar` 提示"加载节点详情失败"，详情页显示"未找到该节点" + 返回节点列表按钮

#### Scenario: 集群离线

- **WHEN** 集群 `Status == Offline`
- **THEN** `GetNodeDetailAsync` 直接返回 `IsReachable = false` 的空详情（不发起 k8s 调用）；手工 IP 卡片仍可加载（不依赖 k8s 可达性）

#### Scenario: 返回节点列表

- **WHEN** 用户点击"返回节点列表"按钮
- **THEN** 跳转至 `/nodes/{ClusterId}`

### Requirement: 节点列表查看

系统 SHALL 在 `/nodes` 与 `/nodes/{ClusterId:int}` 路由下提供节点列表页，采用双栏布局：左侧集群选择树 + 右侧节点表格。节点表格的 IP 列合并展示 k8s InternalIP 与手工录入的 `ClusterNodeIp`。

#### Scenario: 从侧边栏进入未选集群

- **WHEN** 用户从侧边栏「节点管理」进入 `/nodes`（无 ClusterId）
- **THEN** 左侧显示集群选择树（`MudTreeView` 按分组折叠），右侧显示"请从左侧选择一个集群"空状态

#### Scenario: 选择集群后加载节点列表

- **WHEN** 用户在左侧集群选择树中点击一个 Online 集群
- **THEN** URL 跳转至 `/nodes/{ClusterId}`，右侧先调 `ClusterService.GetClusterDetailAsync` 确认可达，再调 `ClusterNodeService.GetClusterNodesAsync` 加载节点列表与 `ClusterNodeIpService.GetClusterNodeIpsAsync(ClusterId)` 加载手工 IP，合并后渲染 `MudTable`（名称/状态/角色/Kubelet版本/操作系统/节点IP）

#### Scenario: 节点名称搜索

- **WHEN** 用户在搜索框输入关键词
- **THEN** `filteredNodes` 计算属性实时过滤，显示名称包含关键词的节点（前端过滤，不发请求）

#### Scenario: 刷新节点列表

- **WHEN** 用户点击刷新按钮
- **THEN** 重新调 `LoadNodesAsync(ClusterId)` 拉取当前集群的节点列表与手工 IP

#### Scenario: 节点名称点击下钻

- **WHEN** 用户点击表格中某节点的名称
- **THEN** 跳转至 `/nodes/{ClusterId}/{NodeName}` 节点详情页

#### Scenario: 离线集群仅展示手工 IP

- **WHEN** 用户选择离线集群
- **THEN** 右侧显示"集群不可达，无法获取节点列表"，不发起 k8s 请求（手工 IP 不单独展示，因节点名称列表来自 k8s）