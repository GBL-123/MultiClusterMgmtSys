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

## 4. 渲染模块与 vite 构建(v1 初版:ECharts + 多入口;后由 ## 7 重设计)

- [x] 4.1 新建 `Web/Assets/Scripts/topology-graph.js`(镜像 dashboard-trend.js 结构:echarts/core + GraphChart + TooltipComponent + CanvasRenderer;模块级 Map 按容器寻址;export mount/update/dispose + resize;layout:'none'、roam:true、rect [110,34]、中心琥珀边框、缺失 dashed+opacity .65、边箭头 + 中文关系名、点击 `invokeMethodAsync("OnNodeClicked", nodeId)`、tooltip 纸底 mono),验证文件存在(初版 ECharts 直绘;已由 7.3 的 G6 重写替换)
- [x] 4.2 改 `vite.config.mjs` 为多入口(lib.entry 对象、fileName 函数化、entryFileNames 保留 .js 覆盖、emptyOutDir:false 与 define 原样保留),验证 `npm run build` 产出 wwwroot/js/ 下 dashboard-trend.js 与 topology-graph.js 两个 .js 且均可 `await import` 成功(多入口产物命名实测)(多入口方案已由 7.3 的独立 vite.topology.mjs 替换——多入口共享 chunk 破坏单文件自托管形态)
- [x] 4.3 验证宿主 `dotnet build` 触发 BuildChartBundle 增量重建且 0 错误(有 node_modules 环境),`-p:SkipVite=true` 路径不受影响

## 5. 组件与详情页接入

- [x] 5.1 新建 `Web/Components/Topology/TopologyGraphCard.razor`(命名空间 `.Web.Components.Topology`;参数 ClusterId/Namespace/Kind/Name;`IAsyncDisposable` + 首次激活懒加载调 TopologyService;payload 匿名对象;OnNodeClicked 经 Kind→路由映射导航,Ingress/PV 终端节点不导航;catch → ExHandler.HandleAsync(ex, "加载拓扑")),验证 `dotnet build` 0 错误、架构测试通过(载荷后收敛至共享 TopologyPayload,并新增放大查看与单飞守卫,见 8.2/8.3)
- [x] 5.2 `app.css` 新增拓扑节(第 17 节 资源拓扑:图容器 + flex-auto 链,不禁 overflow-y),验证样式生效(手工渲染检查;图例/提示/全屏对话框形态扩展见 8.2)
- [x] 5.3 `PodDetail.razor` 追加「拓扑」tab(`<MudTabPanel Text="拓扑">`,切到该 tab 才发起查询),验证手工 dev-run:进入 Pod 详情不预取、首切 tab 才见图
- [x] 5.4 `SvcDetail.razor` 同样追加「拓扑」tab,验证同上
- [x] 5.5 新建 `TopologyCardTests`(bUnit):`ctx.JSInterop.SetupModule` + module.Invocations 断言 payload(nodes/edges/样式字段);节点点击回调 → NavigationManager URL 断言;懒加载(未激活不 import 不调用);失败分支中文提示;`await using ctx` + `JSRuntimeMode.Loose`,验证测试全绿(后扩展:放大查看接线测试从真实按钮触发,见 8.2)

## 6. 集成验证

- [x] 6.1 `dotnet build MultiClusterMgmtSys.slnx` 0 错误且 `dotnet test MultiClusterMgmtSys.Tests` 全绿(基线数量 + 新增 TopologyServiceTests/TopologyCardTests)
- [x] 6.2 `./coverage.ps1` 四程序集合并行覆盖率 ≥75% 门禁通过(新代码有覆盖贡献)
- [x] 6.3 dev-run 手工端到端走查:Pod 中心全边型、Service 中心含 Ingress 路由、缺失引用虚线节点、节点点击跳转、Secret 中心无值泄露、Member 账号可见,记录走查结果(2026-10-14 本地假集群(8443)视觉走查:Pod 五类边齐全 + missing-cm 虚线节点;Service 含 Ingress「路由」边;Pod 节点点击跳转 /clusters→详情、Ingress 终端停留;Member 视图与 Admin 一致;全页 DOM 泄露扫描 clean。Secret 中心口径由服务层测试覆盖(UI v1 无 Secret 拓扑页)。走查另发现并修复 Service 小图节点压扁:ECharts graph view 默认逐轴非均匀 fit-to-fill,topology-graph.js series 加 `preserveAspect: "contain"`、边距 40→60,像素探针复验节点恒 110×34)

## 7. G6 组织架构图风格重设计(2026-10-15)

- [x] 7.1 mockup 方向对比(档案图纸 / 配电盘原理图 / 工程台账 → ECharts 与 SVG 自绘对比 → G6 组织架构图与缩进卡片树两风格),定稿组织架构图风格;素材留档 `mockups/`(含 G6 官方示例源码参考 `g6-ref/`)
- [x] 7.2 `TopologyLayout` 重写为渲染语义(IsAbove/Group/Order/Relation:属主链与 Ingress/PV 上行、下行分组键、多关系「 / 」合并),`TopologyLayoutTests` 同步重写,验证测试全绿
- [x] 7.3 `topology-graph.js` 重写为 G6 5.1.1(自定义卡片节点 `org-card`、dagre 布局、直角无箭头连线、下行同 Kind ≥2 分组卡片、tooltip、fitView 缩放夹紧 [0.6,1.15]);新增独立 `vite.topology.mjs`(package.json build 双配置),验证 `npm run build` 产出 wwwroot/js/topology-graph.js
- [x] 7.4 组件接入:`TopologyPayload`(namespace/分组/isAbove 等载荷字段)、卡片第三行「NS · 关系」、图例行、长名称中段截断;`TopologyCardTests` 同步更新,验证全绿
- [x] 7.5 视觉验证:真实 bundle + 契约 payload 无头截图(Pod/Service 中心,对照 mockup 复核);全量测试 + 覆盖率门禁通过

## 8. 大图可见性与挂载竞态修复(2026-10-17)

- [x] 8.1 容器高度按图自适应(内容高 + 120 呼吸边距,夹紧 [420, 900];`autoHeight` 渲染选项;实测 Pod 15 节点 560→686px 且缩放达上限 1.15,Service 小图维持 560 基线)
- [x] 8.2 「放大查看」:卡片头部入口 + `TopologyFullscreenDialog`(近全屏画布 min(80vh,900px),autoHeight:false;节点点击经 EventCallback 回卡片统一路由);载荷提取为共享 `TopologyPayload.cs`;bUnit 回归从真实按钮触发(断言 autoHeight:false 挂载与导航 URL)
- [x] 8.3 并发挂载残影缺陷修复:CDP 实证(重叠双 mount → canvasCount 8,滚轮缩放暴露双实例叠加);修复 = JS 挂载代次(`mountSeqs`/`destroyIfOwned`,await 恢复点校验)+ 组件 `_chartBusy`/`_mounting` 单飞;`harness/doublemount.mjs` 回归(修复后 4,单实例)
- [x] 8.4 `verify/` 留档更新:README(缺陷说明 / 复跑命令 / 口径)+ 截图(pod/service/dialog/竞态前后);常规路径与旧截图逐字节一致(无回归)
- [x] 8.5 全量测试 1342/1342 全绿 + `./coverage.ps1` 77.1% ≥ 75% 门禁 PASS(顺带修复 `HelmServiceFleetDeployTests` 进度收集的并发 flake:裸 List.Add → lock)
