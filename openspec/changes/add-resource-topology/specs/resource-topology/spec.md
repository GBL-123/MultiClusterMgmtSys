# Spec Delta

## Purpose

为多集群管理系统提供以单个 K8s 资源为中心的对象关系图:从 Pod、Service、ConfigMap、Secret、PVC 与四类工作负载出发,聚合调度、拥有、选择、挂载、引用、路由、供给七类引用边,渲染为可交互的拓扑图并嵌入详情页,让用户无需跨页跳转即可看清一个对象的上下游依赖,并一眼发现引用悬空(被引用对象已不存在)。

## ADDED Requirements

### Requirement: 拓扑查询按中心资源返回节点与边

系统 SHALL 提供 TopologyService,接收集群 Id、命名空间、中心资源类型与名称,返回拓扑数据(节点列表 + 边列表)。节点 SHALL 携带去重标识(`Kind/name`)、原始 Kind、中文类型名、名称、命名空间、是否中心、是否缺失、状态提示(可选)与布局坐标(层号 + 层内序号);边 SHALL 携带起点、终点与中文关系名。中心节点层号 SHALL 为 0,直接邻居层号为 1(v1 仅聚合一跳关系)。中心资源类型 SHALL 支持:Pod、Service、ConfigMap、Secret、PersistentVolumeClaim、Deployment、StatefulSet、DaemonSet、Job。集群不存在或中心资源类型不支持时 SHALL 抛业务异常;集群不可达时 SHALL 转译为可达性业务异常。

#### Scenario: 以 Pod 为中心查询拓扑

- **WHEN** 请求 Pod 中心拓扑(集群、命名空间、名称均有效)
- **THEN** 返回的数据中该 Pod 为唯一中心节点(层号 0),其关系对象为邻居节点(层号 1),边的关系名为中文

#### Scenario: 集群不存在

- **WHEN** 以不存在的集群 Id 查询拓扑
- **THEN** 抛出「集群不存在」业务异常,不发起任何 K8s 调用

#### Scenario: 不支持的类型

- **WHEN** 以未支持的中心类型(如 Node)查询拓扑
- **THEN** 抛出业务异常并给出中文说明

### Requirement: Pod 中心聚合五类关系边

以 Pod 为中心时,系统 SHALL 聚合以下边,方向与关系名固定:① spec.nodeName 指向的 Node,边「调度」(Pod→Node);② ownerReferences 链上的属主工作负载,ReplicaSet SHALL 再上溯其 Deployment,StatefulSet/DaemonSet/Job 一跳即止,边「拥有」(工作负载→Pod);③ 同命名空间内 spec.selector 能匹配本 Pod 标签的 Service,边「选择」(Service→Pod);④ pod.spec.volumes 引用的 ConfigMap、Secret、PVC,边「挂载」(Pod→被挂载对象);⑤ containers 中 env.valueFrom(configMapKeyRef/secretKeyRef)与 envFrom(configMapRef/secretRef)引用的 ConfigMap、Secret,边「引用」(Pod→被引用对象)。裸 Pod(无 ownerReferences)SHALL 无「拥有」边且不视为异常。

#### Scenario: 调度边指向节点

- **WHEN** Pod 的 spec.nodeName 指向集群中存在的节点
- **THEN** 拓扑包含 Pod→Node 的「调度」边,节点状态提示反映该节点就绪状态

#### Scenario: 属主链上溯到 Deployment

- **WHEN** Pod 由 ReplicaSet 管理,ReplicaSet 由 Deployment 管理
- **THEN** 拓扑含「Deployment→ReplicaSet→Pod」两条「拥有」边,Deployment 与 ReplicaSet 均为图中节点

#### Scenario: 裸 Pod 无属主边

- **WHEN** Pod 无任何 ownerReferences
- **THEN** 拓扑无「拥有」边,正常返回其余关系

#### Scenario: Service selector 反查

- **WHEN** 同命名空间某 Service 的 selector 标签均被 Pod 标签满足
- **THEN** 拓扑含该 Service→Pod 的「选择」边

#### Scenario: 挂载与引用去重

- **WHEN** 同一 ConfigMap 同时被 volume 挂载与 envFrom 引用
- **THEN** 两类边各自存在,ConfigMap 节点不重复

### Requirement: Service 中心聚合选择与路由边

以 Service 为中心时,系统 SHALL:① 反查同命名空间中标签满足 spec.selector 的 Pod,边「选择」(Service→Pod);② 扫描同命名空间 Ingress 的 rules[].http.paths[].backend.service.name 指向本 Service 的条目,边「路由」(Ingress→Service)。

#### Scenario: Ingress 路由到 Service

- **WHEN** 某 Ingress 的 backend.service.name 指向中心 Service
- **THEN** 拓扑含 Ingress→Service 的「路由」边,Ingress 为节点

#### Scenario: 无 Ingress 引用

- **WHEN** 无任何 Ingress 指向中心 Service
- **THEN** 拓扑无「路由」边,正常返回「选择」边

### Requirement: 配置与存储中心反向聚合 Pod 引用

以 ConfigMap、Secret 或 PVC 为中心时,系统 SHALL 扫描同命名空间全部 Pod,凡 spec.volumes 或 env/envFrom 引用中心对象的,建立边「挂载」或「引用」(Pod→中心对象)。PVC 额外 SHALL:当 spec.volumeName 已绑定 PV 时,取该 PV 建边「供给」(PV→PVC)。

#### Scenario: ConfigMap 被两个 Pod 引用

- **WHEN** 同命名空间两个 Pod 的 envFrom 引用中心 ConfigMap
- **THEN** 拓扑含两条 Pod→ConfigMap 的「引用」边,两个 Pod 均为节点

#### Scenario: 已绑定 PVC 的供给边

- **WHEN** 中心 PVC 的 spec.volumeName 指向存在的 PV
- **THEN** 拓扑含 PV→PVC 的「供给」边

### Requirement: 工作负载中心聚合属主与选择边

以 Deployment、StatefulSet、DaemonSet 或 Job 为中心时,系统 SHALL 反查同命名空间中标签满足 workload.spec.selector.matchLabels 的 Pod,边「拥有」(工作负载→Pod)。

#### Scenario: 工作负载选择器匹配多个 Pod

- **WHEN** 中心 Deployment 的 selector 匹配同命名空间三个 Pod
- **THEN** 拓扑含三条 Deployment→Pod 的「拥有」边

### Requirement: 被引用但不存在的对象渲染为缺失节点

当 Pod 引用的 ConfigMap、Secret 或 PVC 在集群中不存在(未出现在同命名空间资源列表中)时,系统 SHALL 仍生成该节点并标记为「缺失」(供前端以虚线样式渲染),SHALL NOT 因引用悬空抛出 404 类异常或中断整图。缺失节点 SHALL 保留引用边。

#### Scenario: 引用悬空可见

- **WHEN** Pod 的某 volume 引用的 ConfigMap 已被删除
- **THEN** 拓扑仍含该 ConfigMap 节点(标记缺失)与「挂载」边,查询不失败

### Requirement: 下游查询失败降级不破全图

中心对象获取成功后,任一类邻居关系查询失败(超时、权限、瞬时错误)时,系统 SHALL 丢弃该类关系边并记警告日志,SHALL NOT 让整图查询失败;恢复后再次查询可得到完整结果。

#### Scenario: Ingress 列表失败时 Service 拓扑降级

- **WHEN** 中心 Service 的 Pod 反查成功,但 Ingress 列表查询失败
- **THEN** 返回含「选择」边、不含「路由」边的拓扑,不抛异常

### Requirement: 拓扑数据不包含 Secret 值

拓扑数据(节点与边)SHALL 只包含资源名称、类型、命名空间与状态提示,SHALL NOT 包含任何 ConfigMap/Secret 的键值、数据内容或 Secret 的 data/stringData 值。

#### Scenario: Secret 中心拓扑不含值

- **WHEN** 以 Secret 为中心查询拓扑
- **THEN** 节点仅含该 Secret 的名称与元信息,不含任何键值内容

### Requirement: 拓扑为只读且全员可见

拓扑查询 SHALL 为只读操作,SHALL NOT 写审计日志、SHALL NOT 修改任何 K8s 对象;Admin 与 Member 均可查看(角色无关)。K8s 调用 SHALL 走统一客户端缓存并继承统一 10 秒超时契约。

#### Scenario: 非管理员查看拓扑

- **WHEN** Member 打开含拓扑 tab 的详情页
- **THEN** 拓扑正常渲染,审计日志无新增记录

### Requirement: 拓扑图渲染与节点点击导航

拓扑 tab SHALL 以 ECharts 图渲染:中心节点 SHALL 以品牌色边框高亮,缺失节点 SHALL 以虚线样式区分,边 SHALL 带方向箭头与中文关系名;支持缩放与平移。节点点击 SHALL 跳转到该资源在系统内的详情页(Pod/工作负载/Service/ConfigMap/Secret/PVC/Node);系统内无对应页面的资源(如 Ingress、PV)SHALL 不可点击导航。相同输入的布局 SHALL 确定(同数据同坐标)。

#### Scenario: 点击 Service 节点跳转

- **WHEN** 在 Pod 中心拓扑中点击某个 Service 节点
- **THEN** 页面导航到该 Service 的详情页

#### Scenario: Ingress 节点不可点击

- **WHEN** 在 Service 中心拓扑中点击 Ingress 节点
- **THEN** 不发生导航,节点仅悬浮展示信息

#### Scenario: 缺失节点虚线样式

- **WHEN** 拓扑含缺失节点
- **THEN** 该节点以虚线边框与降低不透明度渲染,与普通节点视觉可区分

### Requirement: 详情页拓扑 tab 懒加载

Pod 详情页与 Service 详情页 SHALL 新增「拓扑」tab。拓扑数据 SHALL 在首次切入该 tab 时才发起查询并渲染,SHALL NOT 在页面进入时(其他 tab 数据加载阶段)预取;tab 内提供加载中反馈;查询失败 SHALL 以统一异常提示呈现且不影响其他 tab。

#### Scenario: 首次切入才加载

- **WHEN** 用户进入 Pod 详情页但未切换到「拓扑」tab
- **THEN** 不发起拓扑查询;首次切入「拓扑」tab 后才发起查询并渲染图

#### Scenario: 拓扑加载失败不影响其他 tab

- **WHEN** 拓扑查询失败(如集群不可达)
- **THEN** tab 内显示中文错误提示,用户切回其他 tab 数据与交互不受影响
