## Context

系统已有集群管理（`Clusters.razor` + `ClusterDetail.razor`）与 `ClusterService`（含 `GetClusterNodesAsync` + `BuildConfig`）。`ClusterDetail.razor` 已含基础节点列表卡片。需要新增独立的节点管理入口与节点详情页，并将节点查询职责从 `ClusterService` 抽取为独立 `ClusterNodeService`。

可达性判断既有链路：页面调 `ClusterService.GetClusterDetailAsync(id)` 获取 `IsReachable`（内部 catch k8s 异常），可达时再调 `ClusterNodeService.GetClusterNodesAsync(id)`（不 catch，异常上抛由页面 try/catch + Snackbar）。

页面布局既有模式：`Nodes.razor` 采用双栏 `MudGrid`（左侧 `MudTreeView` 集群选择树 + 右侧内容区），与集群管理单栏布局不同但适合"选集群 → 看资源"的交互模式。

## Goals / Non-Goals

**Goals:**

- 节点列表页（`/nodes` 与 `/nodes/{ClusterId:int}` 双路由），双栏布局，左侧集群选择树 + 右侧节点表格。
- 单节点详情页（`/nodes/{ClusterId:int}/{NodeName}`），多卡片分块展示 30+ 字段。
- 从集群详情页节点列表下钻到节点管理/节点详情。
- 节点数据实时从 k8s 拉取，不持久化。

**Non-Goals:**

- 节点写操作（封锁/排空/污点管理）。
- 节点历史数据持久化。
- Pod 维度下钻。
- 资源使用率（metrics-server）。

## Decisions

### D1: `ClusterNodeService` 独立服务，仅注入 `ClusterRepository`，无 ILogger

**选择：** 新增 `Services/ClusterNodeService.cs`，主构造函数 `ClusterNodeService(ClusterRepository repo)`，无 `ILogger`。不做 try/catch，异常直接上抛。

**理由：** 节点查询是只读操作，容错由页面层处理（页面先调 `ClusterService.GetClusterDetailAsync` 取 `IsReachable`，可达时才调 `ClusterNodeService`，页面 try/catch + Snackbar）。与 `ClusterService` 的 `ProbeAsync`（catch 并改写状态）不同——节点查询不需要在 Service 层吞异常。

### D2: `BuildConfig` 为 `private static`（非 `BuildK8sClient`）

**选择：** `ClusterNodeService` 内 `private static KubernetesClientConfiguration BuildConfig(ClusterInfo cluster)`，逻辑与 `ClusterService.BuildConfig` 完全相同（KubeConfig 走 `BuildConfigFromConfigFile(stream)`，Token 走手动配置）。

**理由：** 方法名 `BuildConfig` 与 `KubernetesClientConfiguration` 语义一致。`static` 因其不依赖实例状态。第二次复制 `BuildConfig`（`ClusterService` 一次，`ClusterNodeService` 一次），后续可统一抽取为 `KubernetesClientFactory`。

### D3: `MapNode`/`MapNodeDetail` 在 `ClusterNodeService` 内（非 `ClusterService`）

**选择：** `ClusterNodeService` 内 `private static ClusterNodeViewModel MapNode(V1Node node)` 与 `private static ClusterNodeDetailViewModel MapNodeDetail(V1Node node, ClusterInfo cluster)`，以及辅助方法 `ComputeNodeStatus`/`ComputeRoles`/`ComputeInternalIP`/`MapSystemInfo`。

**理由：** 映射逻辑与 k8s 调用同层，节点映射归节点服务。不经过 `ClusterMappingExtensions`（那是实体→VM 映射，节点是 k8s 对象→VM 映射）。

### D4: 双栏布局（左侧集群选择树 + 右侧内容区）

**选择：** `Nodes.razor` 采用 `MudGrid` 双栏：左侧 `MudItem md="3" lg="2"`（`MudTreeView` 按分组折叠）+ 右侧 `MudItem md="9" lg="10"`（标题 + 上下文卡片 + 工具栏 + 表格）。选择集群后 `NavigateTo("/nodes/{id}")` 统一 URL。

**理由：** 节点管理是"集群维度下的资源管理"，用户需要在多个集群间频繁切换，常驻集群选择树比下拉更高效。与 `Clusters.razor` 的单栏布局不同，因为集群管理本身就是在管理集群，而节点管理是选择集群后查看其资源。

### D5: 节点详情用独立详情页（非弹窗）

**选择：** `NodeDetail.razor`（`@page "/nodes/{ClusterId:int}/{NodeName}"`），独立路由页面，多卡片分块。

**理由：** 节点详情字段量大（容量/条件/污点/标签/注解/系统信息等 30+ 字段），弹窗信息密度不足且不利于链接分享与浏览器历史导航。

### D6: `GetNodeDetailAsync` 离线预检

**选择：** `GetNodeDetailAsync` 在 `entity.Status == ClusterStatus.Offline` 时直接返回 `IsReachable = false` 的空详情（不发起 k8s 调用），其他情况调 `ReadNodeAsync(nodeName)`。

**理由：** 离线集群注定无法获取节点详情，预检避免无效请求与长超时等待。与 `ClusterService.GetClusterDetailAsync` 的离线降级逻辑一致。

### D7: `ClusterService` 改为注入 `ClusterNodeService`

**选择：** `ClusterService` 主构造函数改为 `ClusterService(ClusterRepository repo, ClusterNodeService nodeService, ILogger<ClusterService> logger)`，`GetClusterDetailAsync` 内部调 `nodeService.GetClusterNodesAsync(id)` 获取节点列表。

**理由：** `ClusterService` 的 `GetClusterDetailAsync` 需要返回含节点列表的 `ClusterDetailViewModel`，节点查询职责已移至 `ClusterNodeService`，通过注入复用。

## Risks / Trade-offs

- **[`BuildConfig` 第二次复制]** → 已知技术债，后续可抽取 `KubernetesClientFactory` 统一三处调用（`ClusterService`/`ClusterNodeService`/未来的 `ConfigMapService`）。
- **[节点列表与集群详情重复拉取 `ListNodeAsync`]** → `GetClusterDetailAsync` 内部已调一次 `GetClusterNodesAsync`，`Nodes.razor` 进入时再调一次。当前规模下非必需，后续可在 scoped `ClusterService` 内做短时缓存。
- **[节点详情 `ReadNodeAsync` 404]** → k8s client 抛 `HttpOperationException`，页面 try/catch 后 `Snackbar` 提示"加载节点详情失败"，详情页显示"未找到该节点"。
