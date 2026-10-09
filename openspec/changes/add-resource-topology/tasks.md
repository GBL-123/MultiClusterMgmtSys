# Tasks

## 1. 契约与数据结构及布局纯函数

- [x] 1.1 新建 `Application/Requests/TopologyQueryRequest.cs`(record:ClusterId/Namespace/Kind/Name,镜像 PodKeyRequest 风格,中文 XML 注释),验证 `dotnet build` 0 错误、CS1591 零命中
- [x] 1.2 新建 `Application/ViewModels/TopologyViewModel.cs`(TopologyNodeViewModel/TopologyEdgeViewModel/TopologyViewModel 三类,字段按 spec:IsCenter/IsMissing/StatusHint/Layer/Order/Relation 中文),验证 `dotnet build` 0 错误
- [x] 1.3 实现布局纯函数(中心 Layer=0、邻居 Layer=1;层内先上游组 Service/工作负载/Node/Ingress/PV 再下游组 ConfigMap/Secret/PVC,组内按名称排序;x=Order×间距、y=Layer×行距),配套单测:确定性(同输入同坐标)、组顺序、中心与邻居分层,验证测试全绿
- [x] 1.4 在 `ApplicationServiceCollectionExtensions` 注册 TopologyService,验证 `dotnet build` 0 错误且架构测试(`ArchitectureTests`)不报新违规

## 2. TopologyService Pod 中心

- [x] 2.1 用 sigtool 反查并新增 `K8sMocks` Pod 中心所需 mock 段(ReadNamespacedPod/ReadNamespacedNode/ReadNamespacedReplicaSet/ReadNamespacedDeployment/ListNamespacedService/ListNamespacedConfigMap/ListNamespacedSecret/ListNamespacedPersistentVolumeClaim,注意可选参数位置),验证 mock 编译通过
- [x] 2.2 实现 Pod 中心聚合骨架:仓储校验(`NotFoundException`)→ `IClusterClientCache` 取客户端 → Read Pod;裸 Pod(无 ownerReferences)返回仅中心节点,验证服务测试骨架用例通过
- [x] 2.3 实现「调度」边(spec.nodeName → Node,节点带就绪状态 StatusHint)与单测(nodeName 为空无边、Node 存在生成边)
- [x] 2.4 实现「拥有」边 ownerReferences 链(RS→上溯 Deployment;STS/DS/Job 一跳即止)与单测(RS→Deploy 两节点两边、非四类属主一跳即止)
- [x] 2.5 实现「选择」边(List Services → selector 反查 Pod 标签)与单测(命中/未命中/selector 为空)
- [x] 2.6 实现「挂载」与「引用」边(volumes[].configMap.name / secret.secretName / persistentVolumeClaim.claimName + env.valueFrom + envFrom;同对象两类边并存、节点去重)与单测
- [x] 2.7 实现「缺失」节点语义(引用目标不在 List 结果 → IsMissing=true 且保留边,不抛异常)与单测(悬空 ConfigMap 引用可见)

## 3. 其余中心与降级

- [x] 3.1 用 sigtool 反查并补 mock 段(ListNamespacedIngress 走 NetworkingV1、ListNamespacedPod、ReadNamespacedPersistentVolume),验证 mock 编译通过
- [x] 3.2 实现 Service 中心(选择→Pod 反查 + 扫描 Ingress backend.service.name 的「路由」边)与单测(命中 Ingress/无 Ingress/selector 空)
- [x] 3.3 实现 ConfigMap/Secret 中心(ListPods 反扫 volumes+env → Pod→中心「挂载/引用」边)与单测(多 Pod 引用、无引用)
- [x] 3.4 实现 PVC 中心(反向 Pod 扫描 + spec.volumeName 已绑定时「供给」边 PV→PVC)与单测(绑定/未绑定)
- [x] 3.5 实现四类工作负载中心(spec.selector.matchLabels 反查 → 「拥有」边)与单测(多 Pod 命中)
- [x] 3.6 实现降级口径:邻居类查询失败(KubernetesException/HttpOperationException/TaskCanceledException/OperationCanceledException/HttpRequestException)丢该类边 + LogWarning 不破全图,与单测(mock 该类 List 抛 KubernetesException 断言其余边仍在);整体失败路径(集群不存在/不支持 Kind/中心 404→NotFoundException/不可达→Translate)与单测
- [x] 3.7 校验拓扑数据不含 Secret 值(节点结构断言无 data 类字段)与只读零审计(调用 mock 无 write 方法被触碰),验证断言用例通过

## 4. ECharts 模块与 vite 多入口

- [x] 4.1 新建 `Web/Assets/Scripts/topology-graph.js`(镜像 dashboard-trend.js 结构:echarts/core + GraphChart + TooltipComponent + CanvasRenderer;模块级 Map 按容器寻址;export mount/update/dispose + resize;layout:'none'、roam:true、rect [110,34]、中心琥珀边框、缺失 dashed+opacity .65、边箭头 + 中文关系名、点击 `invokeMethodAsync("OnNodeClicked", nodeId)`、tooltip 纸底 mono),验证文件存在
- [x] 4.2 改 `vite.config.mjs` 为多入口(lib.entry 对象、fileName 函数化、entryFileNames 保留 .js 覆盖、emptyOutDir:false 与 define 原样保留),验证 `npm run build` 产出 wwwroot/js/ 下 dashboard-trend.js 与 topology-graph.js 两个 .js 且均可 `await import` 成功(多入口产物命名实测)
- [x] 4.3 验证宿主 `dotnet build` 触发 BuildChartBundle 增量重建且 0 错误(有 node_modules 环境),`-p:SkipVite=true` 路径不受影响

## 5. 组件与详情页接入

- [x] 5.1 新建 `Web/Components/Topology/TopologyGraphCard.razor`(命名空间 `.Web.Components.Topology`;参数 ClusterId/Namespace/Kind/Name;`IAsyncDisposable` + 首次激活懒加载调 TopologyService;payload 匿名对象;OnNodeClicked 经 Kind→路由映射导航,Ingress/PV 终端节点不导航;catch → ExHandler.HandleAsync(ex, "加载拓扑")),验证 `dotnet build` 0 错误、架构测试通过
- [x] 5.2 `app.css` 新增第 16 节 `.topology-card`(固定高度图容器 + flex-auto 链,不禁 overflow-y),验证样式生效(手工渲染检查)
- [x] 5.3 `PodDetail.razor` 追加「拓扑」tab(`<MudTabPanel Text="拓扑">`,切到该 tab 才发起查询),验证手工 dev-run:进入 Pod 详情不预取、首切 tab 才见图
- [x] 5.4 `SvcDetail.razor` 同样追加「拓扑」tab,验证同上
- [x] 5.5 新建 `TopologyCardTests`(bUnit):`ctx.JSInterop.SetupModule` + module.Invocations 断言 payload(nodes/edges/样式字段);节点点击回调 → NavigationManager URL 断言;懒加载(未激活不 import 不调用);失败分支中文提示;`await using ctx` + `JSRuntimeMode.Loose`,验证测试全绿

## 6. 集成验证

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误且 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线数量 + 新增 TopologyServiceTests/TopologyCardTests)
- [x] 6.2 `./coverage.ps1` 四程序集合并行覆盖率 ≥75% 门禁通过(新代码有覆盖贡献)
- [ ] 6.3 dev-run 手工端到端走查:Pod 中心全边型、Service 中心含 Ingress 路由、缺失引用虚线节点、节点点击跳转、Secret 中心无值泄露、Member 账号可见,记录走查结果
