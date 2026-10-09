# Proposal

## Why

详情页只能看到一个对象自身的字段,用户排查「这个 Pod 为什么连不上数据库」「这个 Service 背后跑了什么」时,需要在 Pod、Service、ConfigMap、Secret、PVC、Node、工作负载等多个页面之间来回跳转,人工在脑中拼出引用关系。K8s 对象之间的引用关系(pod.spec.volumes、env.valueFrom、ingress backend、svc selector、ownerReferences、nodeName 等)都是机器可读的,完全可以自动汇聚成一张关系图,直接嵌在详情页里展示。

## What Changes

- 新增 `TopologyService`(Application 层):以任一资源为中心对象,聚合其 K8s 引用关系,返回「节点 + 边」的拓扑数据。覆盖四类中心:Pod(调度→Node、拥有→工作负载链、选择←Service、挂载/引用→ConfigMap/Secret/PVC)、Service(选择→Pod、路由←Ingress)、ConfigMap/Secret/PVC(被 Pod 反向引用;PVC 额外供给←PV)、工作负载(拥有→Pod)。
- 新增 `TopologyGraphCard.razor`(Web/Components/Topology):ECharts graph 渲染,C# 侧预计算分层坐标,JS 侧只负责绘制与点击回调。
- Pod 详情页与 Service 详情页各新增「拓扑」tab,v1 落地两个页面;其余中心类型的拓扑数据服务端已支持,后续页面接入零新逻辑。
- 新增自托管 ECharts 模块 `topology-graph.js`(vite 多入口打包,复用既有 dashboard-trend 构建链)。
- 被引用但实际不存在的对象渲染为虚线「缺失」节点(核心诊断价值:引用悬空一眼可见),不抛 404 异常。

## Capabilities

### New Capabilities

- `resource-topology`: 资源拓扑关系图契约——以 Pod/Service/ConfigMap/Secret/PVC/工作负载为中心的关系聚合口径(边类型、方向、缺失节点语义、下游查询失败降级)、Secret 只显名称不显值、只读零审计、ECharts 渲染与节点点击导航、详情页「拓扑」tab 嵌入。

### Modified Capabilities

<!-- 无:detail-page-tabs 契约范围不含 PodDetail/SvcDetail;pod-management / service-management 的既有需求不变,新增 tab 为纯增量页面结构。 -->

## Impact

- **新增代码**:`Application/Requests/TopologyQueryRequest.cs`、`Application/ViewModels/TopologyViewModel.cs`、`Application/Services/TopologyService.cs`、`Web/Components/Topology/TopologyGraphCard.razor`、`Web/Assets/Scripts/topology-graph.js`;测试 `TopologyServiceTests`、`TopologyCardTests`、K8sMocks 新增 mock 段。
- **修改代码**:`vite.config.mjs`(单入口 → 多入口)、`PodDetail.razor` / `SvcDetail.razor`(加「拓扑」tab)、`app.css`(第 16 节 `.topology-card`)、`ApplicationServiceCollectionExtensions`(注册 TopologyService)。
- **构建链**:vite 多入口是既有 BuildChartBundle 增量链的行为变更点,需实测多入口 lib 产物命名;Docker charts 阶段无需改动。
- **K8s 调用**:全部经既有 `IClusterClientCache`(自动继承 10s 超时契约),不新增集群连接方式;Pod 中心最坏 ≤8 次调用,均在既有配额风格内。
- **无数据库/Schema 变更**、无审计写入、无路由新增。
