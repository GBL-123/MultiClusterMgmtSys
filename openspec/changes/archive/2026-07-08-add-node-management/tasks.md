## 1. ViewModel

- [x] 1.1 创建 `ViewModels/ClusterNodeDetailViewModel.cs`（概要字段 + Uid/CreatedAt/Unschedulable/PodCIDR/Phase + Addresses/Conditions/Taints + Capacity/Allocatable + Labels/Annotations + SystemInfo + ClusterId/ClusterName/IsReachable）
- [x] 1.2 创建 `ViewModels/NodeAddressViewModel.cs`（Type、Address）
- [x] 1.3 创建 `ViewModels/NodeConditionViewModel.cs`（Type、Status、Reason、Message、LastHeartbeatTime、LastTransitionTime）
- [x] 1.4 创建 `ViewModels/NodeTaintViewModel.cs`（Key、Value、Effect）
- [x] 1.5 创建 `ViewModels/NodeSystemInfoViewModel.cs`（Architecture/BootID/ContainerRuntimeVersion/KernelVersion/KubeProxyVersion/KubeletVersion/MachineID/OperatingSystem/OsImage/SystemUUID）

## 2. 服务层

- [x] 2.1 创建 `Services/ClusterNodeService.cs`：主构造函数 `ClusterNodeService(ClusterRepository repo)`（无 ILogger）
- [x] 2.2 实现 `private static KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)`（复制 `ClusterService.BuildConfig` 逻辑）
- [x] 2.3 实现 `GetClusterNodesAsync(int id)`：`repo.GetByIdAsync` → `BuildConfig` → `ListNodeAsync` → `MapNode` 映射（不 catch，异常上抛）
- [x] 2.4 实现 `GetNodeDetailAsync(int clusterId, string nodeName)`：离线预检返回空详情；否则 `ReadNodeAsync` → `MapNodeDetail` 映射
- [x] 2.5 实现 `MapNode`/`MapNodeDetail`/`ComputeNodeStatus`/`ComputeRoles`/`ComputeInternalIP`/`MapSystemInfo` 私有静态方法
- [x] 2.6 `ClusterService` 改为注入 `ClusterNodeService`，`GetClusterDetailAsync` 内部调 `nodeService.GetClusterNodesAsync(id)`
- [x] 2.7 `Program.cs` 注册 `ClusterNodeService`（Scoped）

## 3. 节点列表页

- [x] 3.1 创建 `Components/Pages/Nodes/Nodes.razor`：`@attribute [Authorize]` + `@page "/nodes"` + `@page "/nodes/{ClusterId:int}"` + `<PageTitle>节点管理</PageTitle>`
- [x] 3.2 左侧集群选择栏：`MudCard` + `MudTreeView`（按分组折叠，`groupedClusters` 计算属性）+ 集群搜索框 + 状态图标（`GetClusterStatusIcon`/`GetClusterStatusColor`）
- [x] 3.3 右侧内容区：标题行（`MudText h4` + 返回集群详情按钮）+ 集群上下文卡片（名称/状态/节点数/API Server）+ 工具栏（搜索 + 刷新）+ 表格区
- [x] 3.4 `LoadNodesAsync(int id)`：先 `GetClusterDetailAsync` 取 `IsReachable`，可达时调 `GetClusterNodesAsync`，try/catch + Snackbar
- [x] 3.5 `MudTable<ClusterNodeViewModel>`：名称（可点击下钻+主色下划线）、状态（Chip Success/Error/Default）、角色、Kubelet版本、操作系统、内网IP
- [x] 3.6 状态分支：未选集群（"请从左侧选择一个集群"）、loading（`MudProgressLinear`）、集群不存在（"未找到该集群"）、集群不可达（"集群不可达，无法获取节点列表"）、无节点（"暂无节点数据"）、筛选无结果（"没有符合当前筛选条件的节点" + 重置按钮）
- [x] 3.7 `OnParametersSetAsync`：检测 `ClusterId` 变化，有值时 `LoadNodesAsync`，无值时清空

## 4. 节点详情页

- [x] 4.1 创建 `Components/Pages/Nodes/NodeDetail.razor`：`@attribute [Authorize]` + `@page "/nodes/{ClusterId:int}/{NodeName}"` + `<PageTitle>`
- [x] 4.2 标题行：`MudText h4` "节点详情: {NodeName}" + 返回节点列表按钮
- [x] 4.3 概要卡片（`xs="12"`）：名称/状态Chip/角色/Kubelet版本/操作系统/内网IP
- [x] 4.4 调度信息卡片（`xs="12" md="6"`）：Unschedulable/Phase/PodCIDR
- [x] 4.5 元数据卡片（`xs="12" md="6"`）：Uid/CreatedAt
- [x] 4.6 资源容量卡片（`xs="12"`）：Capacity + Allocatable 并排 `MudTable`
- [x] 4.7 地址列表卡片（`xs="12" md="6"`）：Type/Address `MudTable`
- [x] 4.8 条件列表卡片（`xs="12" md="6"`）：Type/Status/Reason/Message/LastHeartbeatTime/LastTransitionTime `MudTable`
- [x] 4.9 污点列表卡片（`xs="12" md="6"`）：Key/Value/Effect `MudTable`
- [x] 4.10 标签卡片（`xs="12" md="6"`）：Key/Value `MudTable`
- [x] 4.11 注解卡片（`xs="12" md="6"`）：Key/Value `MudTable`（Value 截断 + Title）
- [x] 4.12 系统信息卡片（`xs="12"`）：10 字段三列栅格
- [x] 4.13 操作卡片（`xs="12"`）：刷新 + 返回节点列表按钮
- [x] 4.14 降级态：loading（`MudProgressLinear`）、node is null（"未找到该节点" + 返回按钮）、加载异常（Snackbar）

## 5. 集群详情页改造

- [x] 5.1 `ClusterDetail.razor` 节点列表卡片标题区增加"查看全部"按钮（跳转 `/nodes/{Id}`）
- [x] 5.2 节点行名称单元格改为可点击主色下划线文本（跳转 `/nodes/{Id}/{nodeName}`）

## 6. 导航与接线

- [x] 6.1 `Drawer.razor` 新增 `MudNavLink Href="/nodes" Icon="DeviceHub" Match="NavLinkMatch.Prefix"` 「节点管理」
- [x] 6.2 `_Imports.razor` 新增 `@using MultiClusterMgmtSys.Components.Pages.Nodes`

## 7. 验证

- [x] 7.1 `dotnet build` 通过（无数据库变更，无需删库）
- [x] 7.2 从侧边栏进入 `/nodes`，左侧集群选择树渲染
- [x] 7.3 选择 Online 集群，右侧节点列表渲染
- [x] 7.4 节点名称搜索 + 刷新
- [x] 7.5 点击节点名称下钻到详情页，多卡片分块展示
- [x] 7.6 从集群详情页"查看全部"跳转节点管理
- [x] 7.7 从集群详情页节点名称下钻到节点详情
- [x] 7.8 选择 Offline 集群，显示"集群不可达"
- [x] 7.9 节点不存在时详情页显示"未找到该节点"
