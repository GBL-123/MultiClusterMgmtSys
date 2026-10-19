# Design

## Context

关系数据全部来自 K8s 对象字段,读取路径已在既有服务中验证可行:`StorageService.ListMountedPodsAsync`(PVC→挂载 Pod 反向扫描)是「List 后内存匹配」的仓内先例;`PodService` 展示了标准的 K8s 服务形态(仓储校验 → `IClusterClientCache` 取客户端 → try/catch 转译)。渲染侧项目已有自托管 bundle + vite 8.3.2 构建链(`dashboard-trend.js` 经 `vite.config.mjs` 单入口 lib 模式,`dotnet build` 触发 `BuildChartBundle` target);G6 5.1.1 已作为依赖安装,拓扑经独立 `vite.topology.mjs` 单入口打包。约束:组件层禁止直接依赖 k8s/Infrastructure 类型(架构测试),K8s 调用统一 10s 超时,Secret 值可见性受 secrets-page 契约约束。

## Goals / Non-Goals

**Goals:**

- 四类中心(Pod / Service / 配置存储类 / 工作负载)的一跳关系聚合,边类型:调度、拥有、选择、挂载、引用、路由、供给。
- 缺失引用(引用悬空)作为一等公民可见,而非异常。
- 单一邻居查询失败降级丢边、不破全图。
- G6 组织架构图风格渲染与确定性布局,节点点击导航到系统内详情页;大图可读(分组聚合、高度自适应、放大查看)。
- 「拓扑」tab 懒加载接入 Pod 与 Service 详情页。

**Non-Goals:**

- 多跳(深度 >1)拓扑、跨命名空间聚合、独立 `/topology` 页面。
- 更多中心类型(RBAC/HPA/NetworkPolicy/ResourceQuota 等,系统内无页面导航目标时意义有限)。
- 实时刷新(SSE/WebSocket)、拓扑写操作、拓扑数据入库。
- Workload/ConfigMap/Secret/PVC 详情页的 tab 接入(服务端已支持,后续复制接入即可)。

## Decisions

**D1. G6 组织架构图风格渲染(dagre 在 JS),C# 只算渲染语义** —— 初版 ECharts GraphChart 方案(`layout:'none'` + C# 预计算两层坐标)在真实密度下可读性不足:全部邻居横排一行、卡片被适配缩放压小;经多方向 mockup 对比后定稿 G6 组织架构图风格(官网 organization-chart 示例样式)。分工:dagre 树布局(rankdir TB)与绘制在 JS;C# 纯函数 `TopologyLayout.Apply` 计算渲染语义——渲染侧上行/下行(属主链、Ingress 路由、PV 供给的起点朝上,其余邻居朝下)、下行分组键、确定性顺序(按类型与名称排序)、与中心的关系名(多关系按优先级以「 / 」合并)。确定性由 C# 排序保证,相同输入下 JS 布局结果稳定。下行同 Kind 且数量 ≥2 的邻居由 JS 收敛为合成分组卡片(`g:<Kind>` 节点,组名经 GROUP_LABEL 映射 + 数量徽标),边重排为中心→组→成员;上行节点不参与分组。放弃 ECharts 的原因:图表渲染器的能力边界(无真字体矢量排版、无 DOM 级状态样式、缩放后文字发糊),拓扑是需要「工程图纸」质感的布局确定示意图,不是数据图表。

**D2. 列表代逐个 GET:同命名空间一次 List**。Pod 中心最坏 ≤8 次 K8s 调用:Read Pod、Read Node、Read RS(+上溯 Read Deploy)、List Services、List ConfigMaps、List Secrets、List PVCs(List 调用次数恒定,与引用数量无关)。若对每个引用逐个 GET,调用数随引用数线性增长,大 Pod(挂十几个 ConfigMap)会放大延迟与 API Server 压力。反向扫描(CM/Secret/PVC 中心 List 全命名空间 Pod)沿用 `StorageService.ListMountedPodsAsync` 口径;Service 中心同样 List Pod + List Ingress(NetworkingV1)。

**D3. 缺失引用 = `IsMissing` 节点**。挂载/引用目标不在 List 结果中时生成 `IsMissing=true` 节点(虚线渲染),不抛异常——悬空引用正是排查「Pod 起不来」的关键线索。StatusHint 为 raw 英文(Pod phase / Node Ready 条件),Web 侧 `TopologyPayload` 经 `K8sDisplayText` 映射中文文案(未登记回退原文,display-conventions 口径)。

**D4. 降级边界:中心必成、邻居可丢**。中心对象获取失败(不存在/集群不可达/超时)→ `NotFoundException`/`ClusterUnreachableException` 等业务异常(整 tab 报错,符合「集群离线看板仍可用」的精神——详情页本就以中心对象为主档);邻居类查询失败 → 丢弃该类边 + LogWarning + 继续返回剩余关系。异常翻译统一 `K8sExceptionMapper.Translate(ex, "加载拓扑")`,降级 catch 口径对齐 `ListMountedPodsAsync`(KubernetesException / HttpOperationException / TaskCanceledException / OperationCanceledException / HttpRequestException → 空结果)。

**D5. Secret 值红线:拓扑只携带名称与元数据**。节点结构天然不含 data 字段,服务端组装 ViewModel 时不读取任何 Secret 内容;与 secrets-page 的「详情页掩码 + 揭示写审计」互补——拓扑层连揭示入口都没有。零审计:只读、无变更。

**D6. 两个并行的单入口 vite 配置**。`vite.config.mjs`(dashboard-trend)保持原样;新增 `vite.topology.mjs`(单入口 lib,G6)独立构建 `topology-graph.js`;`package.json` build 依次执行两者。备选(改 `vite.config.mjs` 为多入口、一次构建两份产物)被否:多入口下 rollup 会把共享依赖抽成 hash 命名 chunk,破坏「单文件自托管」形态(浏览器 import 需要固定文件名);独立配置的代价仅是一次额外 vite 启动。宿主 BuildChartBundle 增量目标与 Docker 构建阶段均执行 `npm run build`,自动覆盖两个配置;Docker 构建阶段 `npm ci` + `npm run build` 后以 `-p:SkipVite=true` 跳过宿主重复构建。

**D7. JS interop 契约**。`topology-graph.js`:模块级 Map 按容器元素寻址,export mount/update/dispose;`mount(container, payload, options)` 第三参 options(`autoHeight` 默认 true;放大查看对话框传 false)。payload 经 `TopologyPayload.Build`(卡片与对话框共用)构建:节点(含渲染语义字段)+ 边 + dotnetRef。G6 配置:自定义节点 `org-card`(卡片:资源名中段截断、类型缩写与中文名、「NS · 关系」三行 + 右上状态徽标;中心琥珀 2px 边框、缺失 dashed + opacity .65)、orth 路由直角连线(无箭头)、zoom-canvas + drag-canvas、tooltip 插件、autoResize、关闭动画;初始视图 fitView → 缩放夹紧 [0.6, 1.15] → fitCenter;容器高度按图自适应 [420, 900](内容高 + 120 呼吸边距)。并发防护:每容器挂载代次(`mountSeqs`),mount/update/dispose 抢占失效更早的在途挂载,await 恢复点校验代次、过期实例自清理(仅销毁仍登记在案的实例),真实渲染失败仍上抛给组件;组件侧 `_chartBusy`/`_mounting` 单飞守卫避免发出重叠挂载。节点点击经 `invokeMethodAsync("OnNodeClicked", nodeId)`(跳过 `g:` 分组节点),组件内维护 `Kind→路由模板` 映射(Pod/工作负载含 ReplicaSet/Service/ConfigMap/Secret/PVC/Node 可跳,Ingress/PV 终端不导航);「放大查看」对话框内点击经 EventCallback 回卡片走同一映射并关闭对话框。import 用根绝对路径 `/js/topology-graph.js`(多段路由防误解析)。配色沿用设计系统:琥珀 `#D97706` 中心边框、缺失 dashed,卡片阴影 + 直角灰线,tooltip 纸底 mono。

**D8. 组件与页面接入**。`Web/Components/Topology/TopologyGraphCard.razor`(命名空间 `.Web.Components.Topology`,仅 import Application 的 Requests/ViewModels,满足架构隔离测试);参数 ClusterId/Namespace/Kind/Name,载荷经共享 `TopologyPayload.Build` 构建(卡片与放大查看对话框共用,防两处漂移)。卡片含图例行与操作提示行、右上「放大查看」按钮(拓扑非空才渲染);PodDetail/SvcDetail 追加 `<MudTabPanel Text="拓扑">`,首次激活才调 `TopologyService` 并 mount(页面既有数据加载不变)。样式 `app.css` 第 17 节(资源拓扑:图容器/图例/提示/全屏对话框形态,flex-auto 链)。

**D9. 测试策略**。服务层:TopologyServiceTests 覆盖 Pod 中心 5 类边、ownerRef 链 RS→Deploy、IsMissing、Service 反查、CM/Secret/PVC 反查、供给边、降级丢边;K8sMocks 新增 mock 段,**签名必须经 sigtool 反查**(k8s 19 的可选参数位置与直觉不符,如各 List 接口的 pretty 参数位置)。布局纯函数:TopologyLayoutTests 断言渲染语义(上行/下行判定、下行分组键、确定性顺序、多关系合并)。bUnit:TopologyCardTests 用 `ctx.JSInterop.SetupModule` + module.Invocations 按标识符断言 payload 与 mount/update 选项;「放大查看」从真实按钮触发(不绕过组件接线,防「对话框参数漏传」类缺陷),断言对话框画布以 autoHeight:false 挂载、节点点击导航 URL;`await using ctx` + `JSRuntimeMode.Loose`。渲染视觉:`verify/`(`harness/` 页面 + README)用真实 bundle 与契约 payload 在无头浏览器截图复核(Pod/Service/对话框/竞态前后),并发挂载竞态以 `harness/doublemount.mjs` 为回归驱动(重叠双 mount 断言单实例)。

## Risks / Trade-offs

- [G6 bundle 体积(~1.9MB / gzip 461KB)拖慢详情页] → 拓扑 tab 懒加载 + 按需动态 import;bundle 为自托管静态资产,不阻塞其他 tab 与其他页面。
- [k8s 19 mock 签名位置怪癖导致 setup 打错方法] → 一律 sigtool 反查实际签名再写 mock,沿用仓内「照抄必查签名」纪律。
- [大命名空间 ListPods 反向扫描的内存开销] → 单命名空间规模可控,且与 storage-management 既有口径一致;不做分页/流式(过度设计)。
- [Pod 中心 ≤8 次串行调用的首开延迟] → 懒加载避免无关路径承担成本;调用均 10s 超时兜底;串行实现简单,若实测慢再并行化(局部优化不涉契约)。
- [密集图卡片文字重叠/超出画布] → 下行分组收敛 + fitView 缩放夹紧 [0.6, 1.15] + 名称中段截断(保留尾部区分度)+ 悬浮 tooltip 展示完整信息;「放大查看」提供近全屏复核。
- [ownerRef 上溯遇到非四类工作负载的属主(如自定义 CRD 控制器)] → 一跳即止不深追,只建边到该属主对象,节点标记为不可导航类型。
- [容器尺寸变化与滚轮缩放/挂载时序交错产生画布残影] → 已实证修复:JS 每容器挂载代次(抢占失效在途挂载,await 恢复点校验自清理)+ 组件侧单飞守卫;CDP 竞态回归脚本留档(`verify/harness/doublemount.mjs`,修复前双实例 8 canvas / 修复后单实例 4)。

## Migration Plan

无数据库 Schema 变更、无既有行为变更,正常部署即生效。前端 bundle 需构建环境有 node_modules(宿主 `dotnet build` 缺依赖仅 MCMS001 警告不中断;Docker 构建阶段 `npm ci` + `npm run build` 产出双配置 bundle)。回滚 = 移除两个页面的拓扑 tab + 删除 `vite.topology.mjs` 与其 build 脚本项,服务与 JS 模块可保留为死代码,无持久化残留。
