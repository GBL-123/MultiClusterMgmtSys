# Design

## Context

关系数据全部来自 K8s 对象字段,读取路径已在既有服务中验证可行:`StorageService.ListMountedPodsAsync`(PVC→挂载 Pod 反向扫描)是「List 后内存匹配」的仓内先例;`PodService` 展示了标准的 K8s 服务形态(仓储校验 → `IClusterClientCache` 取客户端 → try/catch 转译)。渲染侧项目已有自托管 ECharts 6.1.0 + vite 8.3.2 构建链(`dashboard-trend.js` 单入口 lib 模式,`dotnet build` 触发 `BuildChartBundle` target,Docker 有独立 charts 阶段)。约束:组件层禁止直接依赖 k8s/Infrastructure 类型(架构测试),K8s 调用统一 10s 超时,Secret 值可见性受 secrets-page 契约约束。

## Goals / Non-Goals

**Goals:**

- 四类中心(Pod / Service / 配置存储类 / 工作负载)的一跳关系聚合,边类型:调度、拥有、选择、挂载、引用、路由、供给。
- 缺失引用(引用悬空)作为一等公民可见,而非异常。
- 单一邻居查询失败降级丢边、不破全图。
- ECharts 图确定性布局,节点点击导航到系统内详情页。
- 「拓扑」tab 懒加载接入 Pod 与 Service 详情页。

**Non-Goals:**

- 多跳(深度 >1)拓扑、跨命名空间聚合、独立 `/topology` 页面。
- 更多中心类型(RBAC/HPA/NetworkPolicy/ResourceQuota 等,系统内无页面导航目标时意义有限)。
- 实时刷新(SSE/WebSocket)、拓扑写操作、拓扑数据入库。
- Workload/ConfigMap/Secret/PVC 详情页的 tab 接入(服务端已支持,后续复制接入即可)。

## Decisions

**D1. ECharts GraphChart + `layout:'none'`,坐标由 C# 预计算** —— GraphChart 仅支持 force/circular/none 三种布局(无 dagre 分层)。v1 拓扑只有两层(中心 0、邻居 1),在 ViewModel 上算 `Layer`(图距)+ `Order`(层内序号)即可:层内先上游组(Service/工作负载/Node),后下游组(ConfigMap/Secret/PVC/Ingress/PV/ReplicaSet 等属主中间层),组内按名称排序;x=Order×间距、y=Layer×行距,JS 乘像素系数。放弃 force:布局不确定、每次渲染位置漂移,与「同数据同布局」契约冲突;放弃 circular:两层结构用圆排布可读性差。备选:前端 JS 算坐标——被否,布局是展示契约的一部分,放 C# 便于单测纯函数断言。

**D2. 列表代逐个 GET:同命名空间一次 List**。Pod 中心最坏 ≤8 次 K8s 调用:Read Pod、Read Node、Read RS(+上溯 Read Deploy)、List Services、List ConfigMaps、List Secrets、List PVCs(List 调用次数恒定,与引用数量无关)。若对每个引用逐个 GET,调用数随引用数线性增长,大 Pod(挂十几个 ConfigMap)会放大延迟与 API Server 压力。反向扫描(CM/Secret/PVC 中心 List 全命名空间 Pod)沿用 `StorageService.ListMountedPodsAsync` 口径;Service 中心同样 List Pod + List Ingress(NetworkingV1)。

**D3. 缺失引用 = `IsMissing` 节点**。挂载/引用目标不在 List 结果中时生成 `IsMissing=true` 节点(虚线渲染),不抛异常——悬空引用正是排查「Pod 起不来」的关键线索。StatusHint 为 raw 英文(Pod phase / Node Ready 条件),前端经 `K8sDisplayText` 映射中文,未登记回退原文(display-conventions 口径)。

**D4. 降级边界:中心必成、邻居可丢**。中心对象获取失败(不存在/集群不可达/超时)→ `NotFoundException`/`ClusterUnreachableException` 等业务异常(整 tab 报错,符合「集群离线看板仍可用」的精神——详情页本就以中心对象为主档);邻居类查询失败 → 丢弃该类边 + LogWarning + 继续返回剩余关系。异常翻译统一 `K8sExceptionMapper.Translate(ex, "加载拓扑")`,降级 catch 口径对齐 `ListMountedPodsAsync`(KubernetesException / HttpOperationException / TaskCanceledException / OperationCanceledException / HttpRequestException → 空结果)。

**D5. Secret 值红线:拓扑只携带名称与元数据**。节点结构天然不含 data 字段,服务端组装 ViewModel 时不读取任何 Secret 内容;与 secrets-page 的「详情页掩码 + 揭示写审计」互补——拓扑层连揭示入口都没有。零审计:只读、无变更。

**D6. vite 多入口**。`lib.entry` 改为对象 `{"dashboard-trend": ..., "topology-graph": ...}`,`fileName` 用 `(format, name) => name`,`rollupOptions.output.entryFileNames` 的 `.js` 覆盖保留(防 `.mjs` 后缀),`emptyOutDir:false`、`define NODE_ENV=production`(zrender 字面量比较必需)原样保留。`BuildChartBundle` 的 Inputs 是目录通配,新源文件自动纳入;Docker charts 阶段(node:22-alpine npm ci)无需改动。备选(独立第二个 vite 配置/两份产物)被否:重复 echarts 依赖打包、构建链翻倍。

**D7. JS interop 契约镜像 dashboard-trend.js**。`topology-graph.js` 同构:模块级 Map 按容器元素寻址,mount/update/dispose + resize 监听;imports echarts/core + GraphChart + TooltipComponent + CanvasRenderer。Blazor 组件 `IAsyncDisposable`;import 用根绝对路径 `/js/topology-graph.js`(多段路由防误解析)。节点点击经 `IDotNetObjectReference.invokeMethodAsync("OnNodeClicked", nodeId)` 回调,组件内维护 `Kind→路由模板` 映射(Pod/工作负载/Service/ConfigMap/Secret/PVC/Node 有页面可跳,Ingress/PV 为终端节点)。配色沿用设计系统:中心节点品牌琥珀 `#D97706` 2px 边框,普通节点 INK 1px,缺失节点 dashed + opacity .65,边 HAIRLINE + 箭头,tooltip 纸底 mono。

**D8. 组件与页面接入**。`Web/Components/Topology/TopologyGraphCard.razor`(命名空间 `.Web.Components.Topology`,仅 import Application 的 Requests/ViewModels,满足架构隔离测试);参数 ClusterId/Namespace/Kind/Name,payload 走匿名对象(与 DashboardNodeTrendCard 一致)。PodDetail/SvcDetail 追加 `<MudTabPanel Text="拓扑">`,首次激活才调 `TopologyService` 并 mount(页面既有数据加载不变)。样式 `app.css` 第 16 节 `.topology-card`(固定高度图容器,flex-auto 链)。

**D9. 测试策略**。服务层:TopologyServiceTests 覆盖 Pod 中心 5 类边、ownerRef 链 RS→Deploy、IsMissing、Service 反查、CM/Secret/PVC 反查、供给边、降级丢边;K8sMocks 新增 mock 段,**签名必须经 sigtool 反查**(k8s 19 的可选参数位置与直觉不符,如 ConfigMap 列表接口的 pretty 参数位置)。bUnit:TopologyCardTests 用 `ctx.JSInterop.SetupModule` + `module.Invocations` 按标识符断言 payload,点击回调断言 NavigationManager URL;`await using ctx` + `JSRuntimeMode.Loose`。布局纯函数:Layer/Order 确定性与分组顺序直接断言。

## Risks / Trade-offs

- [vite 多入口 lib 模式的产物命名/代码切分行为与单入口不一致] → 第一步就实测产物(文件名、双入口各自可 import),必要时手写 `output` 数组收紧;回退方案是保持两份配置二选一,不阻塞其余任务。
- [k8s 19 mock 签名位置怪癖导致 setup 打错方法] → 一律 sigtool 反查实际签名再写 mock,沿用仓内「照抄必查签名」纪律。
- [大命名空间 ListPods 反向扫描的内存开销] → 单命名空间规模可控,且与 storage-management 既有口径一致;不做分页/流式(过度设计)。
- [Pod 中心 ≤8 次串行调用的首开延迟] → 懒加载避免无关路径承担成本;调用均 10s 超时兜底;串行实现简单,若实测慢再并行化(局部优化不涉契约)。
- [ECharts graph 的 label 在节点密集时重叠] → rect 节点 [110,34] + 层内间距按组拉开;roam 缩放兜底人工调阅。
- [ownerRef 上溯遇到非四类工作负载的属主(如自定义 CRD 控制器)] → 一跳即止不深追,只建边到该属主对象,节点标记为不可导航类型。

## Migration Plan

无数据库 Schema 变更、无既有行为变更,正常部署即生效。前端 bundle 需构建环境有 node_modules(仓内 dotnet build 已处理:缺依赖仅 MCMS001 警告不中断;Docker 由 charts 阶段独立构建)。回滚 = 移除两个页面的拓扑 tab + 还原 vite.config.mjs 单入口,服务与 JS 模块可保留为死代码,无持久化残留。
