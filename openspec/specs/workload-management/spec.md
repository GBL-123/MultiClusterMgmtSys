# workload-management

## Purpose

为 apps/v1 四类工作负载（Deployment / StatefulSet / DaemonSet / ReplicaSet）提供集群内管理闭环：侧边导航「工作负载」组下每类一个列表页（集群侧栏 + 命名空间/名称过滤 + 就绪度与滚动状态列）与一个详情页（YAML 视图/编辑、扩缩容、重启、删除）。写操作按类型可用性矩阵约束（扩缩容不含 DaemonSet，重启不含 ReplicaSet）并以 Admin 角色门控，查看对所有登录用户开放；YAML 编辑采用只覆盖 spec 的乐观并发策略，全部写操作成功后写入审计。

## Requirements

### Requirement: 工作负载导航入口
系统 SHALL 在侧边导航(Drawer)提供「工作负载」导航组(`MudNavGroup`,图标区分于既有项),按 `/workloads` 前缀匹配保持展开,内含四个子入口:部署管理(`/workloads/deployments`)、有状态应用(`/workloads/statefulsets`)、守护进程(`/workloads/daemonsets`)、副本集(`/workloads/replicasets`),对所有登录用户可见。列表页 SHALL 同时支持不带集群与带集群参数两种路由(`/workloads/{kind}` 与 `/workloads/{kind}/{ClusterId:int}`),沿用集群选择状态(`ClusterSelectionState`)在无参路由下恢复上次选择。

#### Scenario: 导航组展开与子入口
- **WHEN** 任意登录用户打开侧边导航
- **THEN** 可以看到「工作负载」导航组,展开后含部署管理、有状态应用、守护进程、副本集四个子入口

#### Scenario: 会话内集群记忆
- **WHEN** 用户在 `/workloads/deployments` 直接访问(不带集群参数)
- **THEN** 页面恢复会话中上次选择的集群并加载其工作负载列表

### Requirement: 工作负载列表页
系统 SHALL 为四种 apps/v1 工作负载各提供独立列表页,页面结构一致:集群侧栏、集群状态徽章、刷新按钮、命名空间下拉过滤(选项来自集群命名空间列表)、名称搜索、统一列表表格。列表行 SHALL 展示:名称(`.link-primary` 链接,进入详情)、命名空间、就绪度(`n/m` 等宽字体)、滚动状态徽章、创建时间、操作列。集群不可达时 SHALL 显示不可达提示并禁用写操作入口;未选择集群时 SHALL 显示空态引导。行内操作 SHALL 为「扩缩容」「重启」「⋯菜单(删除)」,按操作可用性矩阵条件渲染。

#### Scenario: 列表加载与过滤
- **WHEN** 用户选择集群并按命名空间过滤、输入名称搜索
- **THEN** 表格只显示匹配命名空间且名称包含搜索词的工作负载,行内展示就绪度与滚动状态

#### Scenario: 名称链接进入详情
- **WHEN** 用户点击列表行中的名称链接
- **THEN** 页面导航到该工作负载的详情页

#### Scenario: 集群不可达
- **WHEN** 所选集群状态为不可达
- **THEN** 页面显示"集群不可达"提示,不提供列表数据与写操作入口

### Requirement: 就绪度与滚动状态判定
系统 SHALL 为列表与详情统一计算三态滚动状态与就绪度(`n/m`),逐型取数:

- **Deployment**:就绪度 = `status.readyReplicas/spec.replicas`;`status.updatedReplicas < spec.replicas` 或 `metadata.generation > status.observedGeneration` 判为滚动中;滚动中除外,`readyReplicas == spec.replicas` 且 `updatedReplicas == spec.replicas` 判为就绪,其余判为未就绪
- **StatefulSet**:就绪度 = `status.readyReplicas/spec.replicas`;`status.updatedReplicas < spec.replicas` 或 `currentRevision != updateRevision` 判为滚动中,余同上
- **DaemonSet**:就绪度 = `status.numberReady/status.desiredNumberScheduled`;`status.updatedNumberScheduled < status.desiredNumberScheduled` 判为滚动中,就绪 = `numberReady == desiredNumberScheduled` 且更新完成
- **ReplicaSet**:就绪度 = `status.readyReplicas/spec.replicas`;`metadata.generation > status.observedGeneration` 判为滚动中,余同上

K8s API 读取失败时状态 SHALL 呈现未知态,不向用户弹错。

#### Scenario: 滚动进行中的 Deployment
- **WHEN** Deployment 的 `spec.replicas` 为 4 且 `status.updatedReplicas` 为 2
- **THEN** 列表行就绪度显示 `n/4`、滚动状态显示"滚动中"

#### Scenario: 稳定的 StatefulSet
- **WHEN** StatefulSet 的 `spec.replicas` 为 3 且 `readyReplicas` 为 3、`updatedReplicas` 为 3
- **THEN** 列表行滚动状态显示"就绪"

### Requirement: 工作负载详情页
系统 SHALL 为每种类型提供详情页(路由 `/workloads/{kind}/{ClusterId:int}/{Namespace}/{Name}`),展示 YAML 视图与信息卡片(元数据、逐型状态),并提供工具栏操作:刷新、编辑 YAML、扩缩容(按矩阵)、重启(按矩阵)、删除。资源不存在时 SHALL 显示"不存在或已被删除"空态并可返回列表。

#### Scenario: 详情页加载
- **WHEN** 用户从列表进入工作负载详情页
- **THEN** 页面显示该对象的 YAML 视图与工具栏,工具栏动作按可用性矩阵渲染

#### Scenario: 对象已被删除
- **WHEN** 详情页请求的工作负载在集群中不存在
- **THEN** 页面显示不存在提示与"返回列表"入口

### Requirement: 工作负载 YAML 新建
系统 SHALL 允许 Admin 从列表页通过「新建」按钮打开 YAML 编辑对话框,按类型预填 YAML 模板;提交时系统 SHALL 反序列化用户 YAML 并校验 `metadata.namespace` 必填后创建。YAML 解析失败 SHALL 抛出中文校验异常(不直出原始异常);创建成功 SHALL 写入审计(类别"工作负载"、操作"创建")并刷新列表。

#### Scenario: 从模板新建 Deployment
- **WHEN** Admin 在部署管理页点击「新建」并提交合法 YAML(含 `metadata.namespace`)
- **THEN** 系统在对应命名空间创建 Deployment,提示成功并刷新列表,写入审计记录

#### Scenario: YAML 缺少命名空间
- **WHEN** 用户提交的 YAML 未包含 `metadata.namespace`
- **THEN** 系统提示中文校验错误,不发起创建请求

#### Scenario: YAML 格式错误
- **WHEN** 用户提交的 YAML 无法反序列化为对应类型
- **THEN** 系统提示"YAML 格式错误:…"且不发起创建请求

### Requirement: 工作负载 YAML 编辑
系统 SHALL 允许 Admin 在详情页进入 YAML 编辑并保存。保存 SHALL 采用乐观并发安全策略:先读取集群中该对象的最新状态,仅将用户 YAML 中的 `spec` 覆盖到最新对象上(元数据与状态以服务器为准),再以最新 resourceVersion 执行替换。保存触发 409 冲突时 SHALL 提示"集群状态已变化,请重试";YAML 解析失败 SHALL 提示"YAML 格式错误:…"。保存成功 SHALL 写入审计(操作"修改")并返回视图态。

#### Scenario: 常规保存
- **WHEN** Admin 编辑 Deployment 的 `spec` 并保存
- **THEN** 系统读取最新对象、覆盖 spec 并替换成功,对象的 status 不被触碰

#### Scenario: 保存时发生冲突
- **WHEN** 替换请求返回 409 冲突
- **THEN** 系统提示"集群状态已变化,请重试",不覆盖控制器写入的内容

### Requirement: 工作负载扩缩容
系统 SHALL 允许 Admin 对 Deployment、StatefulSet、ReplicaSet 执行扩缩容:从列表行或详情页打开扩缩容对话框(当前副本数预填),提交后通过 scale 子资源更新 `spec.replicas`,不读取/替换整个对象。DaemonSet SHALL 不提供扩缩容入口。扩缩容成功 SHALL 写入审计(类别"工作负载"、操作"扩缩容",目标含 `ns/name → n`)并刷新就绪度展示。

#### Scenario: 扩容 Deployment
- **WHEN** Admin 将 Deployment 副本数从 2 调整为 4 并提交
- **THEN** 系统通过 scale 子资源更新副本数,提示成功并刷新列表,审计记录目标包含"→ 4"

#### Scenario: DaemonSet 无扩缩容
- **WHEN** 用户查看守护进程列表或详情
- **THEN** 页面不渲染任何扩缩容入口

### Requirement: 工作负载滚动重启
系统 SHALL 允许 Admin 对 Deployment、StatefulSet、DaemonSet 执行滚动重启:经轻量确认后,向 `spec.template.metadata.annotations` 打补丁写入 `kubectl.kubernetes.io/restartedAt`(RFC3339 当前时间),触发新滚动。ReplicaSet SHALL 不提供重启入口。重启成功 SHALL 写入审计(类别"工作负载"、操作"重启")并刷新列表,重启后滚动状态 SHALL 呈现"滚动中"直至完成。

#### Scenario: 重启 Deployment
- **WHEN** Admin 确认重启某 Deployment
- **THEN** 系统写入 restartedAt 注解触发滚动,提示成功,列表行随后显示"滚动中"

#### Scenario: ReplicaSet 无重启
- **WHEN** 用户查看副本集列表或详情
- **THEN** 页面不渲染任何重启入口

### Requirement: 工作负载删除
系统 SHALL 允许 Admin 删除工作负载,删除前 SHALL 经强确认,确认文案 SHALL 明示级联影响(删除工作负载将连带删除其管理的副本与 Pod)。删除成功 SHALL 写入审计(操作"删除",目标含 `ns/name`)并刷新列表。

#### Scenario: 删除前确认
- **WHEN** Admin 在列表 ⋯菜单或详情页点击删除
- **THEN** 系统弹出确认对话框,文案包含级联影响说明

#### Scenario: 删除成功
- **WHEN** Admin 确认删除
- **THEN** 系统调用删除 API,提示成功并刷新列表,写入审计记录

### Requirement: 操作可用性矩阵
系统 SHALL 按类型控制写操作的可用性:扩缩容适用 Deployment / StatefulSet / ReplicaSet,滚动重启适用 Deployment / StatefulSet / DaemonSet;可用性 MUST 同时约束 UI 条件渲染与服务层方法暴露(不可用的操作不提供服务方法),避免仅依赖 UI 隐藏。

#### Scenario: 服务层无对应方法
- **WHEN** 代码尝试对 DaemonSet 调用扩缩容、对 ReplicaSet 调用重启
- **THEN** 服务层不存在相应公开方法,调用在编译期不可表达

### Requirement: 工作负载权限控制
系统 SHALL 对所有登录用户开放工作负载查看(列表、详情、过滤),SHALL 将全部写操作(新建、编辑 YAML、扩缩容、重启、删除)以 Admin 角色门控,非 Admin 用户不渲染写操作入口。

#### Scenario: Member 查看列表
- **WHEN** Member 用户打开工作负载列表
- **THEN** 页面正常展示列表与过滤,不渲染任何写操作按钮

#### Scenario: Member 尝试写操作
- **WHEN** Member 用户访问仅 Admin 可用的操作路径
- **THEN** 页面不提供对应入口;服务层仍受 Admin 门控约束
